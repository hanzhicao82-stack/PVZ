using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Framework;

namespace Samples
{
    /// <summary>
    /// 事件总线使用示例
    /// 展示如何在不同模块间使用事件进行通信
    /// </summary>
    public partial class EventBusExamples
    {
        private IEventBus _eventBus;

        public void Initialize(IModuleContext context)
        {
            _eventBus = context.GetService<IEventBus>();
        }

        // ==================== 示例1: 简单的发布/订阅 ====================

        /// <summary>
        /// 订阅僵尸死亡事件
        /// </summary>
        public void SubscribeToZombieDeath()
        {
            _eventBus.Subscribe<ZombieDeathEvent>(OnZombieDeath);
        }

        private void OnZombieDeath(ZombieDeathEvent evt)
        {
            UnityEngine.Debug.Log($"僵尸死亡！类�? {evt.ZombieType}, 位置: {evt.Position}, 得分: {evt.ScoreValue}");
            
            // 这里可以触发其他逻辑�?
            // - 更新分数UI
            // - 播放死亡音效
            // - 产生掉落�?
            // - 更新成就进度
        }

        /// <summary>
        /// 发布僵尸死亡事件
        /// </summary>
        public void PublishZombieDeath(Entity zombie, float3 position)
        {
            _eventBus.Publish(new ZombieDeathEvent
            {
                ZombieEntity = zombie,
                ZombieType = "Normal",
                Position = position,
                ScoreValue = 100,
                WasKilledByPlant = true
            });
        }

        // ==================== 示例2: 链式事件处理 ====================

        /// <summary>
        /// 处理波次完成事件，可能触发新的事�?
        /// </summary>
        public void SetupWaveChain()
        {
            _eventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
        }

        private void OnWaveCompleted(WaveCompletedEvent evt)
        {
            UnityEngine.Debug.Log($"波次 {evt.WaveNumber}/{evt.TotalWaves} 完成");

            if (evt.IsFinalWave)
            {
                // 最后一波，发布关卡完成事件
                _eventBus.Publish(new LevelCompletedEvent
                {
                    LevelId = 1,
                    TotalScore = 5000,
                    StarsEarned = 3
                });
            }
            else
            {
                // 还有下一波，发布准备事件
                _eventBus.Publish(new WaveStartedEvent
                {
                    WaveNumber = evt.WaveNumber + 1,
                    TotalWaves = evt.TotalWaves,
                    ZombieCount = 10
                });
            }
        }

        // ==================== 示例3: 延迟事件处理 ====================

        /// <summary>
        /// 延迟发布事件（下一帧执行）
        /// 适用于需要等待当前帧结束的情�?
        /// </summary>
        public void PublishDeferredEvent()
        {
            // 立即执行可能会导致问题（比如在迭代集合时修改集合�?
            // 使用延迟发布可以避免这类问题
            _eventBus.PublishDeferred(new GameEndedEvent
            {
                IsVictory = true,
                FinalScore = 10000,
                PlayTime = 300f
            });

            UnityEngine.Debug.Log("事件已加入延迟队列，将在下一帧执行");
        }

        // ==================== 示例4: 多个订阅�?====================

        /// <summary>
        /// 多个系统订阅同一事件
        /// </summary>
        public void SetupMultipleSubscribers()
        {
            // UI系统订阅
            _eventBus.Subscribe<SunCollectedEvent>(evt =>
            {
                UnityEngine.Debug.Log($"[UI] 更新阳光显示: {evt.TotalSun}");
            });

            // 音效系统订阅
            _eventBus.Subscribe<SunCollectedEvent>(evt =>
            {
                UnityEngine.Debug.Log($"[Audio] 播放阳光收集音效");
            });

            // 成就系统订阅
            _eventBus.Subscribe<SunCollectedEvent>(evt =>
            {
                UnityEngine.Debug.Log($"[Achievement] 检查阳光收集成就");
            });

            // 发布一次，所有订阅者都会收�?
            _eventBus.Publish(new SunCollectedEvent
            {
                SunAmount = 25,
                TotalSun = 175,
                CollectionPosition = new float3(5, 0, 3)
            });
        }

        // ==================== 示例5: 取消订阅（避免内存泄漏） ====================

        private void OnPlantPlaced(PlantPlacedEvent evt)
        {
            UnityEngine.Debug.Log($"植物种植: {evt.PlantType}");
        }

        /// <summary>
        /// 正确的订阅和取消订阅
        /// </summary>
        public void SubscribeAndUnsubscribe()
        {
            // 订阅
            _eventBus.Subscribe<PlantPlacedEvent>(OnPlantPlaced);

            // ... 使用一段时间后 ...

            // 取消订阅（重要！避免内存泄漏�?
            _eventBus.Unsubscribe<PlantPlacedEvent>(OnPlantPlaced);
        }

        // ==================== 示例6: 在模块中使用 ====================

        /// <summary>
        /// 在自定义模块中使用事件总线
        /// </summary>
        public class MyGameModule : GameModuleBase
        {
            public override string ModuleId => "example.my-module";
            public override string DisplayName => "示例模块";
            public override string[] Dependencies => new[] { "core.event-bus" };

            private IEventBus _eventBus;

            protected override void OnInitialize()
            {
                // 获取事件总线
                _eventBus = Context.GetService<IEventBus>();

                // 订阅感兴趣的事件
                _eventBus.Subscribe<GameStartedEvent>(OnGameStarted);
                _eventBus.Subscribe<ZombieDeathEvent>(OnZombieDeath);
            }

            protected override void OnShutdown()
            {
                // 清理订阅
                _eventBus?.Unsubscribe<GameStartedEvent>(OnGameStarted);
                _eventBus?.Unsubscribe<ZombieDeathEvent>(OnZombieDeath);
            }

            private void OnGameStarted(GameStartedEvent evt)
            {
                UnityEngine.Debug.Log($"游戏开始！关卡: {evt.LevelName}");
            }

            private void OnZombieDeath(ZombieDeathEvent evt)
            {
                UnityEngine.Debug.Log($"僵尸死亡！得�? {evt.ScoreValue}");
            }
        }

        // ==================== 示例7: 在ECS System中使�?====================

        /// <summary>
        /// 在ECS System中使用事件总线示例
        /// 注意：实际使用时需要通过某种方式传递EventBus引用
        /// 比如通过Singleton组件、静态服务定位器�?
        /// </summary>
        public partial class ExampleZombieDeathSystem : SystemBase
        {
            private IEventBus _eventBus;

            protected override void OnCreate()
            {
                // 这里需要获取EventBus的引�?
                // 可以通过以下方式�?
                // 1. 静态服务定位器
                // 2. Singleton组件传�?
                // 3. 系统参数注入
            }

            protected override void OnUpdate()
            {
                // 示例：检查僵尸健康并发布死亡事件
                // 实际项目中需要根据具体组件结构调�?
                
                // if (_eventBus == null) return;
                
                // foreach (var (health, entity) in 
                //     SystemAPI.Query<RefRO<HealthComponent>>()
                //         .WithEntityAccess())
                // {
                //     if (health.ValueRO.CurrentHealth <= 0)
                //     {
                //         _eventBus.Publish(new ZombieDeathEvent
                //         {
                //             ZombieEntity = entity,
                //             ZombieType = "Normal",
                //             ScoreValue = 100,
                //             WasKilledByPlant = true
                //         });
                //         
                //         EntityManager.DestroyEntity(entity);
                //     }
                // }
            }
        }

        // ==================== 示例8: 自定义事�?====================

        /// <summary>
        /// 创建自定义事�?
        /// </summary>
        public class CustomGameEvent : GameEventBase
        {
            public string CustomData;
            public int CustomValue;
        }

        public void UseCustomEvent()
        {
            // 订阅自定义事�?
            _eventBus.Subscribe<CustomGameEvent>(evt =>
            {
                UnityEngine.Debug.Log($"自定义事�? {evt.CustomData}, �? {evt.CustomValue}");
            });

            // 发布自定义事�?
            _eventBus.Publish(new CustomGameEvent
            {
                CustomData = "测试数据",
                CustomValue = 42
            });
        }
    }
}


