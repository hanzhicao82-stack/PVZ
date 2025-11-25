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
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            // 遍历所有僵尸实体，更新它们的位置
            foreach (var (transform, zombie) in 
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<ZombieComponent>>())
            {
                // 僵尸向左移动
                float3 position = transform.ValueRO.Position;
                position.x -= zombie.ValueRO.MovementSpeed * deltaTime;
                transform.ValueRW.Position = position;
            }
        }
    }
}
