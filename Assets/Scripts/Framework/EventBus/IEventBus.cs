using System;

namespace Framework
{
    /// <summary>
    /// 事件总线接口 - 提供发布/订阅模式的事件通信
    /// 用于模块间的松耦合通信
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理�?/param>
        void Subscribe<T>(Action<T> handler) where T : class;

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理�?/param>
        void Unsubscribe<T>(Action<T> handler) where T : class;

        /// <summary>
        /// 发布事件（立即执行）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="evt">事件实例</param>
        void Publish<T>(T evt) where T : class;

        /// <summary>
        /// 发布事件（延迟到下一帧执行）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="evt">事件实例</param>
        void PublishDeferred<T>(T evt) where T : class;

        /// <summary>
        /// 清空所有订�?
        /// </summary>
        void Clear();

        /// <summary>
        /// 清空指定类型的订�?
        /// </summary>
        void Clear<T>() where T : class;
    }

    /// <summary>
    /// 游戏事件基类 - 所有事件的基类（可选）
    /// 提供时间戳等通用信息
    /// </summary>
    public abstract class GameEventBase
    {
        /// <summary>
        /// 事件触发时间
        /// </summary>
        public float Timestamp { get; set; }

        /// <summary>
        /// 事件来源模块ID（可选）
        /// </summary>
        public string SourceModuleId { get; set; }

        protected GameEventBase()
        {
            Timestamp = UnityEngine.Time.time;
        }
    }
}
