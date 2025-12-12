using UnityEngine;
using Framework;

namespace Common
{
    /// <summary>
    /// 事件总线模块 - 提供全局事件通信服务
    /// 优先级很高，几乎所有其他模块都可能依赖它
    /// </summary>
    public class EventBusModule : GameModuleBase
    {
        public override string ModuleId => "core.event-bus";
        public override string DisplayName => "事件总线";
        public override string Version => "1.0.0";
        public override string[] Dependencies => System.Array.Empty<string>();
        public override int Priority => 10; // 高优先级，仅次于ECS核心

        private EventBusService _eventBus;
        private EventBusUpdater _updater;

        protected override void OnInitialize()
        {
            // 创建事件总线服务
            _eventBus = new EventBusService();
            Context.RegisterService<IEventBus>(_eventBus);

            // 创建Update组件（用于处理延迟事件）
            var updaterObj = new GameObject("EventBusUpdater");
            _updater = updaterObj.AddComponent<EventBusUpdater>();
            _updater.SetEventBus(_eventBus);
            GameObject.DontDestroyOnLoad(updaterObj);

            UnityEngine.Debug.Log("事件总线已初始化");

            // 可选：启用详细日志
            bool verboseLogging = Context.GetConfigParameter("eventbus.verbose", false);
            if (verboseLogging)
            {
                SubscribeToAllEvents();
            }
        }

        protected override void OnShutdown()
        {
            if (_eventBus != null)
            {
                _eventBus.PrintStatistics();
                _eventBus.Clear();
            }

            if (_updater != null)
            {
                GameObject.Destroy(_updater.gameObject);
            }
        }

        /// <summary>
        /// 订阅所有事件用于调试日志
        /// </summary>
        private void SubscribeToAllEvents()
        {
            _eventBus.Subscribe<GameStartedEvent>(e => 
                UnityEngine.Debug.Log($"[Event] 游戏开始: 关卡{e.LevelId} - {e.LevelName}"));
            
            _eventBus.Subscribe<ZombieSpawnedEvent>(e => 
                UnityEngine.Debug.Log($"[Event] 僵尸生成: {e.ZombieType} 在第{e.Row}行"));
            
            _eventBus.Subscribe<PlantPlacedEvent>(e => 
                UnityEngine.Debug.Log($"[Event] 植物种植: {e.PlantType} 位置({e.Row},{e.Column})"));
            
            _eventBus.Subscribe<ZombieDeathEvent>(e => 
                UnityEngine.Debug.Log($"[Event] 僵尸死亡: {e.ZombieType} 得分+{e.ScoreValue}"));
        }
    }

    /// <summary>
    /// 事件总线更新器 - MonoBehaviour组件
    /// 用于在Update中处理延迟事件
    /// </summary>
    public class EventBusUpdater : MonoBehaviour
    {
        private EventBusService _eventBus;

        public void SetEventBus(EventBusService eventBus)
        {
            _eventBus = eventBus;
        }

        private void Update()
        {
            _eventBus?.ProcessDeferredEvents();
        }
    }
}
