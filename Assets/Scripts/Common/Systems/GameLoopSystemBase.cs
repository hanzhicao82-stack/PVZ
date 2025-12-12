using Unity.Entities;

namespace Common
{
    /// <summary>
    /// 游戏循环系统抽象基类 - 提供通用的游戏状态管理和循环逻辑
    /// 子类需要实现具体的胜利和失败条件检查逻辑
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public abstract partial class GameLoopSystemBase : SystemBase
    {
        private bool m_LoggedInitialState;
        private float m_LastLogTime;
        private GameState m_PreviousState;

        protected override void OnCreate()
        {
            UnityEngine.Debug.Log($"{GetType().Name}: OnCreate 被调用 - 系统已创建");
            
            // 要求 GameStateComponent 存在才能运行
            RequireForUpdate<GameStateComponent>();
            
            m_LoggedInitialState = false;
            m_LastLogTime = 0f;
            m_PreviousState = GameState.Preparing;
        }

        protected override void OnUpdate()
        {
            if (!SystemAPI.TryGetSingletonRW<GameStateComponent>(out var gameState))
            {
                UnityEngine.Debug.LogWarning($"{GetType().Name}: 无法获取 GameStateComponent");
                return;
            }

            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // 首次运行时记录初始状态
            if (!m_LoggedInitialState)
            {
                UnityEngine.Debug.Log($"{GetType().Name}: 初始游戏状态={gameState.ValueRO.CurrentState} 剩余时间={gameState.ValueRO.RemainingTime}");
                m_LoggedInitialState = true;
                m_LastLogTime = currentTime;
            }

            // 检测状态切换
            if (gameState.ValueRO.CurrentState != m_PreviousState)
            {
                HandleStateChange(m_PreviousState, gameState.ValueRO.CurrentState);
                m_PreviousState = gameState.ValueRO.CurrentState;
            }

            // 定期输出状态日志
            if (currentTime - m_LastLogTime > 10f)
            {
                LogGameState(gameState.ValueRO);
                m_LastLogTime = currentTime;
            }

            // 只在游戏进行中更新
            if (gameState.ValueRO.CurrentState != GameState.Playing)
                return;

            float deltaTime = SystemAPI.Time.DeltaTime;

            // 更新倒计时
            gameState.ValueRW.RemainingTime -= deltaTime;

            // 时间耗尽检查
            if (gameState.ValueRO.RemainingTime <= 0)
            {
                gameState.ValueRW.RemainingTime = 0;
                if (CheckVictoryCondition(ref gameState))
                {
                    gameState.ValueRW.CurrentState = GameState.Victory;
                }
                return;
            }

            // 检查失败条件
            if (CheckDefeatCondition(ref gameState))
            {
                gameState.ValueRW.CurrentState = GameState.Defeat;
                return;
            }

            // 执行具体的游戏循环逻辑
            UpdateGameLoop(deltaTime, ref gameState);
        }

        /// <summary>
        /// 处理游戏状态切换，可由子类重写
        /// </summary>
        protected virtual void HandleStateChange(GameState previousState, GameState newState)
        {
            UnityEngine.Debug.Log($"{GetType().Name}: 游戏状态切换: {previousState} -> {newState}");
        }

        /// <summary>
        /// 记录游戏状态日志，可由子类重写
        /// </summary>
        protected virtual void LogGameState(GameStateComponent gameState)
        {
            UnityEngine.Debug.Log($"{GetType().Name}: 状态={gameState.CurrentState} 剩余时间={gameState.RemainingTime:F1}秒");
        }

        /// <summary>
        /// 检查失败条件，由子类实现
        /// </summary>
        protected abstract bool CheckDefeatCondition(ref RefRW<GameStateComponent> gameState);

        /// <summary>
        /// 检查胜利条件，由子类实现
        /// </summary>
        protected abstract bool CheckVictoryCondition(ref RefRW<GameStateComponent> gameState);

        /// <summary>
        /// 执行具体的游戏循环逻辑，由子类实现
        /// </summary>
        protected virtual void UpdateGameLoop(float deltaTime, ref RefRW<GameStateComponent> gameState)
        {
            // 默认实现为空，子类可选择性重写
        }
    }
}
