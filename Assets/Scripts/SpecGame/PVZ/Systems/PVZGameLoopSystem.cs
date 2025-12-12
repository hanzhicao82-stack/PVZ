using Unity.Entities;
using Common;

namespace PVZ
{
    /// <summary>
    /// PVZ游戏循环系统 - 实现具体的僵尸检查逻辑
    /// </summary>
    public partial class PVZGameLoopSystem : Common.GameLoopSystemBase
    {
        protected override bool CheckDefeatCondition(ref RefRW<GameStateComponent> gameState)
        {
            // 检查失败条件：僵尸到达终点数量超过5个
            if (gameState.ValueRO.ZombiesReachedEnd >= 5)
            {
                return true;
            }

            // 检查僵尸是否到达终点(x < 0，植物防线最左侧)
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
                    UnityEngine.Debug.Log($"PVZGameLoopSystem: 僵尸突破防线! 位置={transform.ValueRO.Position.x:F2} 累计={gameState.ValueRO.ZombiesReachedEnd}");
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();

            return false;
        }

        protected override bool CheckVictoryCondition(ref RefRW<GameStateComponent> gameState)
        {
            // 统计场上剩余僵尸
            int remainingZombies = 0;
            foreach (var zombie in SystemAPI.Query<RefRO<ZombieComponent>>())
            {
                remainingZombies++;
            }

            // 如果场上没有僵尸了，胜利
            return remainingZombies == 0;
        }

        protected override void HandleStateChange(GameState previousState, GameState newState)
        {
            base.HandleStateChange(previousState, newState);

            if (newState == GameState.Victory)
            {
                UnityEngine.Debug.Log("PVZGameLoopSystem: 成功坚守阵地！");
                StopAllGameplaySystems();
            }
            else if (newState == GameState.Defeat)
            {
                UnityEngine.Debug.Log("PVZGameLoopSystem: 僵尸突破了防线！");
                StopAllGameplaySystems();
            }
        }

        /// <summary>
        /// 停止所有游戏系统
        /// </summary>
        private void StopAllGameplaySystems()
        {
            UnityEngine.Debug.Log("PVZGameLoopSystem: 停止所有游戏系统");

            var world = World;

            // 停止所有游戏逻辑相关的系统
            DisableISystem(world, world.GetExistingSystem<ZombieMovementSystem>(), "ZombieMovementSystem");
            DisableISystem(world, world.GetExistingSystem<ZombieSpawnSystem>(), "ZombieSpawnSystem");
            DisableISystem(world, world.GetExistingSystem<SunProductionSystem>(), "SunProductionSystem");
            
            // 对于 SystemBase（class），使用 GetExistingSystemManaged
            DisableManagedSystem<ZombieAttackSystem>(world);
            DisableManagedSystem<PlantAttackSystem>(world);
        }

        /// <summary>
        /// 禁用基于 ISystem 的系统（通过 SystemHandle）
        /// </summary>
        private void DisableISystem(World world, SystemHandle systemHandle, string systemName)
        {
            if (systemHandle != SystemHandle.Null)
            {
                ref var systemState = ref world.Unmanaged.ResolveSystemStateRef(systemHandle);
                systemState.Enabled = false;
                UnityEngine.Debug.Log($"PVZGameLoopSystem: 已停止系统 {systemName}");
            }
        }

        /// <summary>
        /// 禁用派生自 SystemBase 的托管系统类
        /// </summary>
        private void DisableManagedSystem<T>(World world) where T : SystemBase
        {
            var system = world.GetExistingSystemManaged<T>();
            if (system != null)
            {
                system.Enabled = false;
                UnityEngine.Debug.Log($"PVZGameLoopSystem: 已停止系统：{typeof(T).Name}");
            }
        }
    }
}
