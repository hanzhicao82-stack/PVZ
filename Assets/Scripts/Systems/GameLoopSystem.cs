using Unity.Entities;
using Unity.Mathematics;
using PVZ.DOTS.Components;

namespace PVZ.DOTS
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct GameLoopSystem : ISystem
    {
        private bool _loggedInitialState;
        private float _lastLogTime;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameStateComponent>();
            _loggedInitialState = false;
            _lastLogTime = 0f;
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonRW<GameStateComponent>(out var gameState))
                return;

            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // 首次运行时记录初始状态
            if (!_loggedInitialState)
            {
                UnityEngine.Debug.Log($"GameLoopSystem: 初始游戏状态={gameState.ValueRO.CurrentState} 剩余时间={gameState.ValueRO.RemainingTime}");
                _loggedInitialState = true;
                _lastLogTime = currentTime;
            }

            // 每10秒输出一次状态
            if (currentTime - _lastLogTime > 10f)
            {
                UnityEngine.Debug.Log($"GameLoopSystem: 状态={gameState.ValueRO.CurrentState} 剩余时间={gameState.ValueRO.RemainingTime:F1}秒");
                _lastLogTime = currentTime;
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
                CheckVictoryCondition(ref state, ref gameState);
                return;
            }

            // 检查失败条件：僵尸到达终点
            if (gameState.ValueRO.ZombiesReachedEnd >= 5)
            {
                gameState.ValueRW.CurrentState = GameState.Defeat;
                UnityEngine.Debug.Log("游戏失败：僵尸突破防线！");
                return;
            }

            // 检查僵尸是否到达终点 (x < -8)
            foreach (var (transform, zombie) in SystemAPI.Query<RefRO<Unity.Transforms.LocalTransform>, RefRO<ZombieComponent>>())
            {
                if (transform.ValueRO.Position.x < -8f)
                {
                    gameState.ValueRW.ZombiesReachedEnd++;
                }
            }
        }

        private void CheckVictoryCondition(ref SystemState state, ref RefRW<GameStateComponent> gameState)
        {
            // 统计场上剩余僵尸
            int remainingZombies = 0;
            foreach (var zombie in SystemAPI.Query<RefRO<ZombieComponent>>())
            {
                remainingZombies++;
            }

            if (remainingZombies == 0)
            {
                gameState.ValueRW.CurrentState = GameState.Victory;
                UnityEngine.Debug.Log("游戏胜利：成功坚守阵地！");
            }
            else
            {
                gameState.ValueRW.CurrentState = GameState.Defeat;
                UnityEngine.Debug.Log("游戏失败：时间耗尽！");
            }
        }
    }
}
