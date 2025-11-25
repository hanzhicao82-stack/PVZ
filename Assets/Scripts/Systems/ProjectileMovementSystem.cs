using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Systems
{
    /// <summary>
    /// 子弹移动系统 - 处理子弹的移动和销毁逻辑
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ProjectileMovementSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            // 遍历所有子弹实体
            foreach (var (transform, projectile, entity) in 
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<ProjectileComponent>>()
                .WithEntityAccess())
            {
                // 子弹向右移动
                float3 position = transform.ValueRO.Position;
                position += projectile.ValueRO.Direction * projectile.ValueRO.Speed * deltaTime;
                transform.ValueRW.Position = position;

                // 如果子弹超出边界，销毁它
                if (position.x > 20f || position.x < -20f)
                {
                    ecb.DestroyEntity(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
