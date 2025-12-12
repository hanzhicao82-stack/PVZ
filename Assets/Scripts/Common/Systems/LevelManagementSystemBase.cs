using Unity.Entities;

namespace Common
{
    /// <summary>
    /// 关卡管理系统抽象基类 - 提供通用的关卡管理逻辑
    /// 子类需要实现具体的波次管理和生成逻辑
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public abstract partial class LevelManagementSystemBase : SystemBase
    {
        protected bool Initialized { get; private set; }
        protected int CurrentWave { get; set; }
        protected float LastWaveTime { get; set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            // 要求 GameStateComponent 存在，否则系统不会运行，避免时序问题
            RequireForUpdate<GameStateComponent>();

            Initialized = false;
            CurrentWave = 0;
            LastWaveTime = 0f;
        }

        protected override void OnUpdate()
        {
            // 检查游戏状态
            if (!SystemAPI.TryGetSingleton<GameStateComponent>(out var gameState))
                return;

            if (gameState.CurrentState != GameState.Playing)
                return;

            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // 初始化关卡
            if (!Initialized)
            {
                InitializeLevel();
                Initialized = true;
            }

            // 更新关卡逻辑
            UpdateLevel(currentTime);
        }

        /// <summary>
        /// 初始化关卡，由子类实现
        /// </summary>
        protected abstract void InitializeLevel();

        /// <summary>
        /// 更新关卡逻辑，由子类实现
        /// </summary>
        protected abstract void UpdateLevel(float currentTime);

        /// <summary>
        /// 获取波次间隔时间，可由子类重写
        /// </summary>
        protected virtual float GetWaveInterval()
        {
            return 30f; // 默认30秒一波
        }

        /// <summary>
        /// 检查是否应该开始新波次
        /// </summary>
        protected bool ShouldStartNewWave(float currentTime)
        {
            return currentTime - LastWaveTime >= GetWaveInterval();
        }
    }
}
