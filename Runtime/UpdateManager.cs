using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using ZJM_Optimize.Runtime.Interfaces;

namespace ZJM_Optimize.Runtime
{
    /// <summary>
    /// 统一更新管理器。
    /// 公开 API 与旧版完全一致；内部由“List + 线性判重 + RemoveAt”改为
    /// “List 保序 + Dictionary 定位 + 墓碑注销 + 帧末稳定压缩”。
    /// 遍历顺序（注册逆序）、帧边界语义、销毁兜底行为均与旧版保持一致。
    /// </summary>
    public class UpdateManager : MonoBehaviour
    {
        private static readonly ObserverGroup<IObserverUpdate> _updates = new ObserverGroup<IObserverUpdate>();
        private static readonly ObserverGroup<IObserverFixedUpdate> _fixeds = new ObserverGroup<IObserverFixedUpdate>();
        private static readonly ObserverGroup<IObserverLateUpdate> _lates = new ObserverGroup<IObserverLateUpdate>();

        private void Update()
        {
            var group = _updates;
            var count = group.Count;
            for (var i = count - 1; i >= 0; i--)
            {
                var observer = group[i];
                if (observer == null)
                    continue;

                if (ObserverUtil.IsDestroyed(observer))
                {
                    group.MarkDead(i);
                    continue;
                }

                observer.ObserverUpdate();
            }

            group.CompactIfNeeded();
        }

        private void FixedUpdate()
        {
            var group = _fixeds;
            var count = group.Count;
            for (var i = count - 1; i >= 0; i--)
            {
                var observer = group[i];
                if (observer == null)
                    continue;

                if (ObserverUtil.IsDestroyed(observer))
                {
                    group.MarkDead(i);
                    continue;
                }

                observer.ObserverFixedUpdate();
            }

            group.CompactIfNeeded();
        }

        private void LateUpdate()
        {
            var group = _lates;
            var count = group.Count;
            for (var i = count - 1; i >= 0; i--)
            {
                var observer = group[i];
                if (observer == null)
                    continue;

                if (ObserverUtil.IsDestroyed(observer))
                {
                    group.MarkDead(i);
                    continue;
                }

                observer.ObserverLateUpdate();
            }

            group.CompactIfNeeded();
        }

        public static void RegisterObserverUpdate(IObserverUpdate observer)
        {
            _updates.Register(observer);
        }

        public static void RegisterObserverFixedUpdate(IObserverFixedUpdate observer)
        {
            _fixeds.Register(observer);
        }

        public static void RegisterObserverLateUpdate(IObserverLateUpdate observer)
        {
            _lates.Register(observer);
        }

        public static void UnRegisterObserverUpdate(IObserverUpdate observer)
        {
            _updates.UnRegister(observer);
        }

        public static void UnRegisterObserverFixedUpdate(IObserverFixedUpdate observer)
        {
            _fixeds.UnRegister(observer);
        }

        public static void UnRegisterObserverLateUpdate(IObserverLateUpdate observer)
        {
            _lates.UnRegister(observer);
        }
    }

    internal static class ObserverUtil
    {
        /// <summary>
        /// Unity 销毁的 Object 通过接口引用时不会走 fake-null，必须显式检测。
        /// 与旧实现 `observer is Object u &amp;&amp; u == null` 语义等价。
        /// </summary>
        public static bool IsDestroyed(object observer)
        {
            var unityObject = observer as UnityEngine.Object;
            if (ReferenceEquals(unityObject, null))
                return false;

            return unityObject == null;
        }
    }

    /// <summary>
    /// 按引用身份比较。Dictionary 默认比较器会走 object.Equals，
    /// 而 UnityEngine.Object 与业务类都可能重写它，这里必须锁死引用语义。
    /// </summary>
    internal sealed class RefComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly RefComparer<T> Instance = new RefComparer<T>();

        public bool Equals(T x, T y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }

    /// <summary>
    /// 一个更新阶段的观察者表。
    /// 不变量：
    ///   1. _items 保序，是唯一的遍历顺序来源；
    ///   2. _slotOf 是 O(1) 的成员判定与槽位索引，_items[i] 与 _slotOf 一一对应；
    ///   3. 注销只把槽位置空（墓碑），不在遍历中移动元素；
    ///   4. 压缩只发生在遍历结束后，且是稳定压缩（顺序不变）。
    /// </summary>
    internal sealed class ObserverGroup<T> where T : class
    {
        private const int CompactMinDead = 32;

        private readonly List<T> _items = new List<T>();
        private readonly Dictionary<T, int> _slotOf = new Dictionary<T, int>(RefComparer<T>.Instance);

        private int _dead;

        public int Count
        {
            get { return _items.Count; }
        }

        public T this[int index]
        {
            get { return _items[index]; }
        }

        public void Register(T observer)
        {
            if (observer == null || ObserverUtil.IsDestroyed(observer))
                return;

            if (_slotOf.ContainsKey(observer))
                return;

            _slotOf.Add(observer, _items.Count);
            _items.Add(observer);
        }

        public void UnRegister(T observer)
        {
            if (observer == null)
                return;

            int slot;
            if (!_slotOf.TryGetValue(observer, out slot))
                return;

            _slotOf.Remove(observer);
            _items[slot] = null;
            _dead++;
        }

        /// <summary>遍历时发现漏注销的已销毁对象，就地立墓碑（不移动元素）。</summary>
        public void MarkDead(int index)
        {
            var observer = _items[index];
            if (observer == null)
                return;

            _items[index] = null;
            _slotOf.Remove(observer);
            _dead++;
        }

        /// <summary>
        /// 稳定压缩：墓碑占比超过 1/4 且数量超过下限时，一次性回收。
        /// 必须在遍历结束后调用，不能在遍历中调用。
        /// </summary>
        public void CompactIfNeeded()
        {
            if (_dead < CompactMinDead || _dead * 4 < _items.Count)
                return;

            var write = 0;
            for (var read = 0; read < _items.Count; read++)
            {
                var observer = _items[read];
                if (observer == null)
                    continue;

                _items[write] = observer;
                _slotOf[observer] = write;
                write++;
            }

            if (write < _items.Count)
                _items.RemoveRange(write, _items.Count - write);

            _dead = 0;
        }
    }
}
