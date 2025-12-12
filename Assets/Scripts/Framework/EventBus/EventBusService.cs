using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 事件总线实现 - 线程不安全版本（适用于主线程�?
    /// 提供高性能的事件发布订阅机�?
    /// </summary>
    public class EventBusService : IEventBus
    {
        // 存储每种事件类型的订阅者列�?
        private readonly Dictionary<System.Type, Delegate> _subscribers = new Dictionary<System.Type, Delegate>();
        
        // 延迟执行的事件队�?
        private readonly Queue<Action> _deferredEvents = new Queue<Action>();
        
        // 统计信息
        private int _totalPublished = 0;
        private int _totalDeferred = 0;

        /// <summary>
        /// 订阅事件
        /// </summary>
        public void Subscribe<T>(Action<T> handler) where T : class
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var eventType = typeof(T);

            if (_subscribers.TryGetValue(eventType, out var existingDelegate))
            {
                // 合并委托
                _subscribers[eventType] = Delegate.Combine(existingDelegate, handler);
            }
            else
            {
                // 首次订阅
                _subscribers[eventType] = handler;
            }

            UnityEngine.Debug.Log($"[EventBus] 订阅事件: {eventType.Name}, 当前订阅者数: {GetSubscriberCount<T>()}");
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler) where T : class
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var eventType = typeof(T);

            if (_subscribers.TryGetValue(eventType, out var existingDelegate))
            {
                var newDelegate = Delegate.Remove(existingDelegate, handler);
                
                if (newDelegate == null)
                {
                    // 没有订阅者了，移除这个事件类�?
                    _subscribers.Remove(eventType);
                    UnityEngine.Debug.Log($"[EventBus] 取消订阅: {eventType.Name}, 该事件已无订阅者");
                }
                else
                {
                    _subscribers[eventType] = newDelegate;
                    UnityEngine.Debug.Log($"[EventBus] 取消订阅: {eventType.Name}, 剩余订阅者数: {GetSubscriberCount<T>()}");
                }
            }
        }

        /// <summary>
        /// 发布事件（立即执行）
        /// </summary>
        public void Publish<T>(T evt) where T : class
        {
            if (evt == null)
                throw new ArgumentNullException(nameof(evt));

            var eventType = typeof(T);
            _totalPublished++;

            if (!_subscribers.TryGetValue(eventType, out var del))
            {
                // 没有订阅者，静默返回
                return;
            }

            // 调用所有订阅�?
            var action = del as Action<T>;
            if (action != null)
            {
                try
                {
                    action.Invoke(evt);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[EventBus] 发布事件 {eventType.Name} 时发生异�? {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// 发布事件（延迟到下一帧执行）
        /// </summary>
        public void PublishDeferred<T>(T evt) where T : class
        {
            if (evt == null)
                throw new ArgumentNullException(nameof(evt));

            _totalDeferred++;
            _deferredEvents.Enqueue(() => Publish(evt));
        }

        /// <summary>
        /// 处理所有延迟事件（应该在Update中调用）
        /// </summary>
        public void ProcessDeferredEvents()
        {
            var count = _deferredEvents.Count;
            if (count == 0)
                return;

            // 处理当前队列中的所有事�?
            // 注意：处理过程中可能有新的延迟事件加入，这些会在下一帧处�?
            for (int i = 0; i < count; i++)
            {
                if (_deferredEvents.Count == 0)
                    break;

                var action = _deferredEvents.Dequeue();
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[EventBus] 处理延迟事件时发生异�? {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// 清空所有订�?
        /// </summary>
        public void Clear()
        {
            _subscribers.Clear();
            _deferredEvents.Clear();
            UnityEngine.Debug.Log("[EventBus] 已清空所有订阅和延迟事件");
        }

        /// <summary>
        /// 清空指定类型的订�?
        /// </summary>
        public void Clear<T>() where T : class
        {
            var eventType = typeof(T);
            if (_subscribers.Remove(eventType))
            {
                UnityEngine.Debug.Log($"[EventBus] 已清空事件类�? {eventType.Name}");
            }
        }

        /// <summary>
        /// 获取指定事件类型的订阅者数�?
        /// </summary>
        public int GetSubscriberCount<T>() where T : class
        {
            var eventType = typeof(T);
            if (_subscribers.TryGetValue(eventType, out var del))
            {
                return del.GetInvocationList().Length;
            }
            return 0;
        }

        /// <summary>
        /// 获取所有注册的事件类型
        /// </summary>
        public IEnumerable<System.Type> GetRegisteredEventTypes()
        {
            return _subscribers.Keys;
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public void PrintStatistics()
        {
            UnityEngine.Debug.Log($"[EventBus] 统计信息:");
            UnityEngine.Debug.Log($"  - 注册的事件类型数: {_subscribers.Count}");
            UnityEngine.Debug.Log($"  - 总发布事件数: {_totalPublished}");
            UnityEngine.Debug.Log($"  - 总延迟事件数: {_totalDeferred}");
            UnityEngine.Debug.Log($"  - 待处理延迟事�? {_deferredEvents.Count}");

            foreach (var kvp in _subscribers)
            {
                var count = kvp.Value.GetInvocationList().Length;
                UnityEngine.Debug.Log($"  - {kvp.Key.Name}: {count} 个订阅者");
            }
        }
    }
}
