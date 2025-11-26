using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Systems
{
    /// <summary>
    /// 僵尸移动系统 - 处理僵尸向左移动的逻辑
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ZombieMovementSystem : ISystem
    {
        private bool _hasLoggedStart;

        public void OnUpdate(ref SystemState state)
        {
            // 检查游戏状态
            if (SystemAPI.TryGetSingleton<GameStateComponent>(out var gameState))
            {
                if (gameState.CurrentState != GameState.Playing)
                    return;
            }

            float deltaTime = SystemAPI.Time.DeltaTime;

            int zombieCount = 0;
            // 遍历所有僵尸实体，更新它们的位置
            foreach (var (transform, zombie) in 
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<ZombieComponent>>())
            {
                // 只有当移动速度大于0时才移动（攻击时速度为0）
                if (zombie.ValueRO.MovementSpeed > 0f)
                {
                    // 僵尸向左移动
                    float3 position = transform.ValueRO.Position;
                    position.x -= zombie.ValueRO.MovementSpeed * deltaTime;
                    transform.ValueRW.Position = position;
                }
                zombieCount++;
            }

            if (!_hasLoggedStart && zombieCount > 0)
            {
                UnityEngine.Debug.Log($"ZombieMovementSystem: 开始移动 {zombieCount} 个僵尸");
                _hasLoggedStart = true;
            }
        }
    }
}
