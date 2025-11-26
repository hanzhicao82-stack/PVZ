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
        private GameState _previousState;

        public void OnCreate(ref SystemState state)
        {
            UnityEngine.Debug.Log("GameLoopSystem: OnCreate 被调用 - 系统已创建");
            
            // 检查是否存在GameStateComponent
            var query = state.EntityManager.CreateEntityQuery(typeof(GameStateComponent));
            int count = query.CalculateEntityCount();
            UnityEngine.Debug.Log($"GameLoopSystem: OnCreate时 GameStateComponent 数量 = {count}");
            query.Dispose();
            
            // state.RequireForUpdate<GameStateComponent>();
            _loggedInitialState = false;
            _lastLogTime = 0f;
            _previousState = GameState.Preparing;
        }

        public void OnUpdate(ref SystemState state)
        {
            // UnityEngine.Debug.Log("GameLoopSystem: OnUpdate 被调用");
            if (!SystemAPI.TryGetSingletonRW<GameStateComponent>(out var gameState))
            {
                UnityEngine.Debug.LogWarning("GameLoopSystem: 无法获取 GameStateComponent，这不应该发生！");
                return;
            }

            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // 首次运行时记录初始状态
            if (!_loggedInitialState)
            {
                UnityEngine.Debug.Log($"GameLoopSystem: 初始游戏状态={gameState.ValueRO.CurrentState} 剩余时间={gameState.ValueRO.RemainingTime}");
                _loggedInitialState = true;
                _lastLogTime = currentTime;
            }

            // 检测状态切换
            if (gameState.ValueRO.CurrentState != _previousState)
            {
                HandleStateChange(ref state, _previousState, gameState.ValueRO.CurrentState);
                _previousState = gameState.ValueRO.CurrentState;
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
                return;
            }

            // 检查僵尸是否到达终点 (x < -10，植物防线最左侧)
            // 使用EntityCommandBuffer来销毁到达终点的僵尸，避免重复计数
            EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (transform, zombie, entity) in
                SystemAPI.Query<RefRO<Unity.Transforms.LocalTransform>, RefRO<ZombieComponent>>()
                .WithEntityAccess())
            {
                if (transform.ValueRO.Position.x < 0f)
                {
                    gameState.ValueRW.ZombiesReachedEnd++;
                    ecb.DestroyEntity(entity);
                    UnityEngine.Debug.Log($"GameLoopSystem: 僵尸突破防线! 位置={transform.ValueRO.Position.x:F2} 累计={gameState.ValueRO.ZombiesReachedEnd}");
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
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
            }
            else
            {
                gameState.ValueRW.CurrentState = GameState.Defeat;
            }
        }

        /// <summary>
        /// 处理游戏状态切换
        /// </summary>
        private void HandleStateChange(ref SystemState state, GameState oldState, GameState newState)
        {
            UnityEngine.Debug.Log($"GameLoopSystem: 状态切换 {oldState} -> {newState}");

            switch (newState)
            {
                case GameState.Preparing:
                    OnEnterPreparing(ref state);
                    break;

                case GameState.Playing:
                    OnEnterPlaying(ref state);
                    break;

                case GameState.Victory:
                    OnEnterVictory(ref state);
                    break;

                case GameState.Defeat:
                    OnEnterDefeat(ref state);
                    break;
            }
        }

        /// <summary>
        /// 进入准备阶段
        /// </summary>
        private void OnEnterPreparing(ref SystemState state)
        {
            UnityEngine.Debug.Log("GameLoopSystem: 进入准备阶段");
        }

        /// <summary>
        /// 进入游戏阶段
        /// </summary>
        private void OnEnterPlaying(ref SystemState state)
        {
            UnityEngine.Debug.Log("GameLoopSystem: 游戏开始！");
        }

        /// <summary>
        /// 进入胜利状态
        /// </summary>
        private void OnEnterVictory(ref SystemState state)
        {
            UnityEngine.Debug.Log("GameLoopSystem: 游戏胜利！成功坚守阵地！");
            StopAllGameplaySystems(ref state);
        }

        /// <summary>
        /// 进入失败状态
        /// </summary>
        private void OnEnterDefeat(ref SystemState state)
        {
            UnityEngine.Debug.Log("GameLoopSystem: 游戏失败！");
            StopAllGameplaySystems(ref state);
        }

        /// <summary>
        /// 停止所有游戏系统
        /// </summary>
        private void StopAllGameplaySystems(ref SystemState state)
        {
            UnityEngine.Debug.Log("GameLoopSystem: 停止所有游戏系统");

            var world = state.World;

            // 停止所有游戏逻辑相关的系统
            DisableSystem<Systems.ZombieMovementSystem>(world);
            DisableSystem<Systems.ZombieAttackSystem>(world);
            DisableSystem<Systems.ZombieSpawnSystem>(world);
            DisableSystem<Systems.PlantAttackSystem>(world);
            DisableSystem<Systems.ProjectileMovementSystem>(world);
            DisableSystem<Systems.CombatSystem>(world);
            DisableSystem<Systems.SunProductionSystem>(world);
        }

        /// <summary>
        /// 禁用指定的系统
        /// </summary>
        private void DisableSystem<T>(World world) where T : unmanaged, ISystem
        {
            var systemHandle = world.GetExistingSystem<T>();
            if (systemHandle != SystemHandle.Null)
            {
                // 在Unity ECS中，通过设置系统的Enabled状态来停用系统
                ref var systemState = ref world.Unmanaged.ResolveSystemStateRef(systemHandle);
                systemState.Enabled = false;
                UnityEngine.Debug.Log($"GameLoopSystem: 已停止系统 {typeof(T).Name}");
            }
        }
    }
}
