using System.Collections.Generic;
using UnityEngine;
using ZJM_Optimize.Runtime.Interfaces;

namespace ZJM_Optimize.Runtime
{
    public class UpdateManager : MonoBehaviour
    {
        private static readonly List<IObserverUpdate> _observers = new();
        private static readonly List<IObserverUpdate> _pendingObservers = new();

        private static readonly List<IObserverFixedUpdate> _obseverFixeds = new();
        private static readonly List<IObserverFixedUpdate> _pendingObserverFixeds = new();

        private static readonly List<IObserverLateUpdate> _obseverLates = new();
        private static readonly List<IObserverLateUpdate> _pendingObserverLates = new();

        private static int _currentObserverIndex;
        private static int _currentObserverFixedIndex;
        private static int _currentObserverLateIndex;

        private void Update()
        {
            FlushPending(_observers, _pendingObservers);
            for (_currentObserverIndex = _observers.Count - 1; _currentObserverIndex >= 0; _currentObserverIndex--)
            {
                var observer = _observers[_currentObserverIndex];
                if (IsDestroyed(observer))
                {
                    _observers.RemoveAt(_currentObserverIndex);
                    continue;
                }

                observer.ObserverUpdate();
            }
        }

        private void FixedUpdate()
        {
            FlushPending(_obseverFixeds, _pendingObserverFixeds);
            for (_currentObserverFixedIndex = _obseverFixeds.Count - 1; _currentObserverFixedIndex >= 0; _currentObserverFixedIndex--)
            {
                var observer = _obseverFixeds[_currentObserverFixedIndex];
                if (IsDestroyed(observer))
                {
                    _obseverFixeds.RemoveAt(_currentObserverFixedIndex);
                    continue;
                }

                observer.ObserverFixedUpdate();
            }
        }

        private void LateUpdate()
        {
            FlushPending(_obseverLates, _pendingObserverLates);
            for (_currentObserverLateIndex = _obseverLates.Count - 1; _currentObserverLateIndex >= 0; _currentObserverLateIndex--)
            {
                var observer = _obseverLates[_currentObserverLateIndex];
                if (IsDestroyed(observer))
                {
                    _obseverLates.RemoveAt(_currentObserverLateIndex);
                    continue;
                }

                observer.ObserverLateUpdate();
            }
        }

        public static void RegisterObserverUpdate(IObserverUpdate observer)
            => Register(observer, _observers, _pendingObservers);

        public static void RegisterObserverFixedUpdate(IObserverFixedUpdate observer)
            => Register(observer, _obseverFixeds, _pendingObserverFixeds);

        public static void RegisterObserverLateUpdate(IObserverLateUpdate observer)
            => Register(observer, _obseverLates, _pendingObserverLates);

        public static void UnRegisterObserverUpdate(IObserverUpdate observer)
            => UnRegister(observer, _observers, _pendingObservers, ref _currentObserverIndex);

        public static void UnRegisterObserverFixedUpdate(IObserverFixedUpdate observer)
            => UnRegister(observer, _obseverFixeds, _pendingObserverFixeds, ref _currentObserverFixedIndex);

        public static void UnRegisterObserverLateUpdate(IObserverLateUpdate observer)
            => UnRegister(observer, _obseverLates, _pendingObserverLates, ref _currentObserverLateIndex);

        static void Register<T>(T observer, List<T> active, List<T> pending) where T : class
        {
            if (observer == null || IsDestroyed(observer))
                return;
            if (ContainsRef(active, observer) || ContainsRef(pending, observer))
                return;

            pending.Add(observer);
        }

        static void UnRegister<T>(T observer, List<T> active, List<T> pending, ref int currentIndex) where T : class
        {
            if (observer == null)
                return;

            RemoveAllRefs(active, observer, ref currentIndex);
            RemoveAllRefs(pending, observer);
        }

        static void FlushPending<T>(List<T> active, List<T> pending) where T : class
        {
            if (pending.Count == 0)
                return;

            for (var i = 0; i < pending.Count; i++)
            {
                var observer = pending[i];
                if (observer == null || IsDestroyed(observer))
                    continue;
                if (ContainsRef(active, observer))
                    continue;

                active.Add(observer);
            }

            pending.Clear();
        }

        static bool ContainsRef<T>(List<T> list, T item) where T : class
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], item))
                    return true;
            }

            return false;
        }

        static void RemoveAllRefs<T>(List<T> list, T item) where T : class
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(list[i], item))
                    list.RemoveAt(i);
            }
        }

        static void RemoveAllRefs<T>(List<T> list, T item, ref int currentIndex) where T : class
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(list[i], item))
                    continue;

                list.RemoveAt(i);
                if (i <= currentIndex)
                    currentIndex--;
            }
        }

        /// <summary>
        /// Unity 销毁的 Object 通过接口引用时不会走 fake-null，必须显式检测。
        /// </summary>
        static bool IsDestroyed(object observer)
        {
            return observer is Object unityObject && unityObject == null;
        }
    }
}
