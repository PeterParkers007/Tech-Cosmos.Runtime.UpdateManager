# ZJM UpdateManager 统一更新管理系统

一个高性能的 Unity 更新管理系统，通过观察者模式集中处理 Update、FixedUpdate 和 LateUpdate，显著提升游戏性能。

## ✨ 功能特性

- **🚀 性能优化** - 减少 MonoBehaviour 组件数量，降低 Unity 引擎开销
- **🔧 统一管理** - 集中处理所有游戏对象的更新逻辑
- **🎯 类型安全** - 强类型接口约束，编译时错误检查
- **🔄 动态注册** - 运行时安全添加/移除观察者
- **📊 多更新类型** - 支持 Update、FixedUpdate、LateUpdate 三种更新周期

## 🚀 快速开始

### 1. 设置 UpdateManager

在场景中创建 GameObject 并添加 `UpdateManager` 组件：

```csharp
// 自动创建管理器（推荐）
public class GameBootstrap : MonoBehaviour
{
    void Start()
    {
        // 确保 UpdateManager 存在
        if (FindObjectOfType<UpdateManager>() == null)
        {
            GameObject managerObj = new GameObject("UpdateManager");
            managerObj.AddComponent<UpdateManager>();
            DontDestroyOnLoad(managerObj);
        }
    }
}
```

### 2. 实现观察者接口

```csharp
public class PlayerController : MonoBehaviour, IObserverUpdate, IObserverFixedUpdate
{
    private void Start()
    {
        // 注册到更新管理器
        UpdateManager.RegisterObserverUpdate(this);
        UpdateManager.RegisterObserverFixedUpdate(this);
    }
    
    private void OnDestroy()
    {
        // 重要：销毁时取消注册
        UpdateManager.UnRegisterObserverUpdate(this);
        UpdateManager.UnRegisterObserverFixedUpdate(this);
    }
    
    public void ObserverUpdate()
    {
        // 处理每帧更新逻辑（输入检测、动画等）
        HandleInput();
        UpdateAnimation();
    }
    
    public void ObserverFixedUpdate()
    {
        // 处理物理更新逻辑
        HandlePhysics();
    }
    
    private void HandleInput()
    {
        // 输入处理代码
    }
    
    private void HandlePhysics()
    {
        // 物理处理代码
    }
    
    private void UpdateAnimation()
    {
        // 动画更新代码
    }
}
```

### 3. 完整使用示例

```csharp
public class EnemyAI : MonoBehaviour, IObserverUpdate, IObserverLateUpdate
{
    [SerializeField] private float moveSpeed = 5f;
    
    private void Start()
    {
        UpdateManager.RegisterObserverUpdate(this);
        UpdateManager.RegisterObserverLateUpdate(this);
    }
    
    private void OnDestroy()
    {
        UpdateManager.UnRegisterObserverUpdate(this);
        UpdateManager.UnRegisterObserverLateUpdate(this);
    }
    
    public void ObserverUpdate()
    {
        // 每帧更新：AI 决策、移动计算
        CalculateMovement();
        MakeDecisions();
    }
    
    public void ObserverLateUpdate()
    {
        // LateUpdate：相机跟随、最终位置调整
        UpdateCameraFollow();
    }
    
    private void CalculateMovement()
    {
        // 移动计算逻辑
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }
    
    private void MakeDecisions()
    {
        // AI 决策逻辑
    }
    
    private void UpdateCameraFollow()
    {
        // 相机相关逻辑
    }
}
```

## 📖 核心接口

### IObserverUpdate - 每帧更新
```csharp
public interface IObserverUpdate
{
    void ObserverUpdate();
}

// 使用场景：输入处理、游戏逻辑、动画更新等
```

### IObserverFixedUpdate - 物理更新
```csharp
public interface IObserverFixedUpdate
{
    void ObserverFixedUpdate();
}

// 使用场景：物理计算、刚体运动等
```

### IObserverLateUpdate - 延迟更新
```csharp
public interface IObserverLateUpdate
{
    void ObserverLateUpdate();
}

// 使用场景：相机跟随、最终位置调整等
```

## 🛠️ 最佳实践

### 1. 正确的生命周期管理

```csharp
public class Projectile : MonoBehaviour, IObserverUpdate
{
    private bool _isActive = true;
    
    private void OnEnable()
    {
        UpdateManager.RegisterObserverUpdate(this);
    }
    
    private void OnDisable()
    {
        UpdateManager.UnRegisterObserverUpdate(this);
    }
    
    public void ObserverUpdate()
    {
        if (!_isActive) return;
        
        // 子弹移动逻辑
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
        
        // 生命周期检查
        if (Time.time - spawnTime > lifeTime)
        {
            _isActive = false;
            gameObject.SetActive(false);
        }
    }
}
```

### 2. 性能敏感对象的优化

```csharp
public class ParticleSystemController : MonoBehaviour, IObserverUpdate
{
    private ParticleSystem _particleSystem;
    private bool _needsUpdate = false;
    
    private void Start()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        UpdateManager.RegisterObserverUpdate(this);
    }
    
    private void OnDestroy()
    {
        UpdateManager.UnRegisterObserverUpdate(this);
    }
    
    public void Play()
    {
        _needsUpdate = true;
        _particleSystem.Play();
    }
    
    public void ObserverUpdate()
    {
        if (!_needsUpdate) return;
        
        // 只有需要更新时才执行逻辑
        if (!_particleSystem.IsAlive())
        {
            _needsUpdate = false;
            gameObject.SetActive(false);
        }
    }
}
```

### 3. 复杂对象的多更新管理

```csharp
public class AdvancedCharacter : MonoBehaviour, 
    IObserverUpdate, 
    IObserverFixedUpdate, 
    IObserverLateUpdate
{
    private void Start()
    {
        // 按需注册不同的更新类型
        UpdateManager.RegisterObserverUpdate(this);
        UpdateManager.RegisterObserverFixedUpdate(this);
        UpdateManager.RegisterObserverLateUpdate(this);
    }
    
    private void OnDestroy()
    {
        // 确保全部取消注册
        UpdateManager.UnRegisterObserverUpdate(this);
        UpdateManager.UnRegisterObserverFixedUpdate(this);
        UpdateManager.UnRegisterObserverLateUpdate(this);
    }
    
    public void ObserverUpdate()
    {
        // 游戏逻辑更新
        UpdateInput();
        UpdateAI();
        UpdateAnimations();
    }
    
    public void ObserverFixedUpdate()
    {
        // 物理相关更新
        UpdatePhysics();
        UpdateCollisions();
    }
    
    public void ObserverLateUpdate()
    {
        // 渲染前最后更新
        UpdateCamera();
        UpdateUI();
    }
}
```

## ⚡ 性能对比

### 传统方式 vs UpdateManager

**传统方式（100个 MonoBehaviour）：**
```csharp
public class TraditionalObject : MonoBehaviour
{
    void Update() { /* 逻辑 */ }
    void FixedUpdate() { /* 逻辑 */ }
    void LateUpdate() { /* 逻辑 */ }
}
```
- ❌ 100个 Update() 调用开销
- ❌ 100个 FixedUpdate() 调用开销  
- ❌ 100个 LateUpdate() 调用开销

**UpdateManager 方式：**
```csharp
public class OptimizedObject : IObserverUpdate, IObserverFixedUpdate
{
    public void ObserverUpdate() { /* 逻辑 */ }
    public void ObserverFixedUpdate() { /* 逻辑 */ }
}
```
- ✅ 1个 Update() 调用 + 100个接口调用
- ✅ 1个 FixedUpdate() 调用 + 100个接口调用
- ✅ 显著降低引擎开销

## 🐛 故障排除

### 常见问题

**Q: 对象更新没有被调用**
- 检查是否正确实现了对应的接口
- 确认在 Start() 或 OnEnable() 中调用了注册方法
- 验证 UpdateManager 是否存在于场景中

**Q: 对象销毁时出现空引用**
- 确保在 OnDestroy() 或 OnDisable() 中取消注册
- 检查取消注册的顺序是否正确

**Q: 性能没有提升**
- 确保移除了原有的 Update() 方法
- 检查是否有大量空实现的观察者
- 考虑按需注册，不需要更新的对象不要注册

### 调试技巧

```csharp
// 添加调试信息
public class DebugUpdateManager : MonoBehaviour
{
    void Update()
    {
        Debug.Log($"Update Observers: {GetObserverCount()}");
    }
    
    // 通过反射获取实际数量（仅用于调试）
    private int GetObserverCount()
    {
        var field = typeof(UpdateManager).GetField("_observers", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var list = (List<IObserverUpdate>)field.GetValue(null);
        return list.Count;
    }
}
```

## 📋 版本要求

- **Unity**: 2019.4 或更高版本
- **.NET**: 4.x 运行时
- **平台**: 所有平台支持

## 🎯 使用建议

### 适合使用 UpdateManager 的场景：
- ✅ 大量同类型对象（子弹、敌人、粒子等）
- ✅ 性能敏感的游戏对象
- ✅ 需要精细控制更新顺序的对象
- ✅ 大型项目需要统一更新管理

### 不适合使用的场景：
- ❌ 少量简单对象
- ❌ 原型开发阶段
- ❌ 第三方插件集成

## 📄 许可证

MIT License - 可自由用于商业项目

---

**性能提示**: 在拥有 100+ 个需要更新的对象时，使用 UpdateManager 通常可以获得 20-40% 的性能提升。