using Unity.Entities;
using Common;

namespace Game.TowerDefense
{
    /// <summary>
    /// 游戏循环系统基类 - 提供通用的游戏状态管理和循环逻辑
    /// 具体游戏需要继承并实现CheckDefeatCondition和CheckVictoryCondition
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public abstract partial class GameLoopSystemBase : SystemBase
    {
        private bool m_LoggedInitialState;
        private float m_LastLogTime;
        private GameState m_PreviousState;

        protected override void OnCreate()
        {
            UnityEngine.Debug.Log("GameLoopSystemBase: OnCreate 被调用 - 系统已创建");
            
            m_LoggedInitialState = false;
            m_LastLogTime = 0f;
            m_PreviousState = GameState.Preparing;
        }

        protected override void OnUpdate()
        {
            if (!SystemAPI.TryGetSingletonRW<GameStateComponent>(out var gameState))
            {
                UnityEngine.Debug.LogWarning("GameLoopSystemBase: 无法获取 GameStateComponent");
                return;
            }

            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // 首次运行时记录初始状态
            if (!m_LoggedInitialState)
            {
                UnityEngine.Debug.Log($"GameLoopSystemBase: 初始游戏状态={gameState.ValueRO.CurrentState} 剩余时间={gameState.ValueRO.RemainingTime}");
                m_LoggedInitialState = true;
                m_LastLogTime = currentTime;
            }

            // 检测状态切换
            if (gameState.ValueRO.CurrentState != m_PreviousState)
            {
                HandleStateChange(m_PreviousState, gameState.ValueRO.CurrentState);
                m_PreviousState = gameState.ValueRO.CurrentState;
            }

            // 每10秒输出一次状态
            if (currentTime - m_LastLogTime > 10f)
            {
                UnityEngine.Debug.Log($"GameLoopSystemBase: 状态={gameState.ValueRO.CurrentState} 剩余时间={gameState.ValueRO.RemainingTime:F1}秒");
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

            // 检查失败条件（由子类实现）
            if (CheckDefeatCondition(ref gameState))
            {
                gameState.ValueRW.CurrentState = GameState.Defeat;
                return;
            }
        }

        /// <summary>
        /// 检查失败条件 - 子类必须实现
        /// </summary>
        /// <returns>如果满足失败条件返回true</returns>
        protected abstract bool CheckDefeatCondition(ref RefRW<GameStateComponent> gameState);

        /// <summary>
        /// 检查胜利条件 - 子类必须实现
        /// </summary>
        /// <returns>如果满足胜利条件返回true</returns>
        protected abstract bool CheckVictoryCondition(ref RefRW<GameStateComponent> gameState);

        /// <summary>
        /// 处理游戏状态切换
        /// </summary>
        protected virtual void HandleStateChange(GameState oldState, GameState newState)
        {
            UnityEngine.Debug.Log($"GameLoopSystemBase: 状态切换：{oldState} -> {newState}");

            switch (newState)
            {
                case GameState.Preparing:
                    OnEnterPreparing();
                    break;

                case GameState.Playing:
                    OnEnterPlaying();
                    break;

                case GameState.Victory:
                    OnEnterVictory();
                    break;

                case GameState.Defeat:
                    OnEnterDefeat();
                    break;
            }
        }

        /// <summary>
        /// 进入准备阶段
        /// </summary>
        protected virtual void OnEnterPreparing()
        {
            UnityEngine.Debug.Log("GameLoopSystemBase: 进入准备阶段");
        }

        /// <summary>
        /// 进入游戏阶段
        /// </summary>
        protected virtual void OnEnterPlaying()
        {
            UnityEngine.Debug.Log("GameLoopSystemBase: 游戏开始！");
        }

        /// <summary>
        /// 进入胜利状态
        /// </summary>
        protected virtual void OnEnterVictory()
        {
            UnityEngine.Debug.Log("GameLoopSystemBase: 游戏胜利！");
        }

        /// <summary>
        /// 进入失败状态
        /// </summary>
        protected virtual void OnEnterDefeat()
        {
            UnityEngine.Debug.Log("GameLoopSystemBase: 游戏失败！");
        }
    }
}
