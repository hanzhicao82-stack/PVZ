using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Systems
{
    /// <summary>
    /// 植物攻击系统 - 处理植物发射子弹的逻辑
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PlantAttackSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            // 遍历所有植物
            foreach (var (plant, transform, gridPos, entity) in 
                SystemAPI.Query<RefRW<PlantComponent>, RefRO<LocalTransform>, RefRO<GridPositionComponent>>()
                .WithEntityAccess())
            {
                // 检查攻击冷却时间
                if (currentTime - plant.ValueRO.LastAttackTime < plant.ValueRO.AttackInterval)
                    continue;

                // 检查同一行是否有僵尸
                bool hasZombieInLane = false;
                foreach (var (zombieGridPos, zombieTransform) in 
                    SystemAPI.Query<RefRO<GridPositionComponent>, RefRO<LocalTransform>>()
                    .WithAll<ZombieComponent>())
                {
                    if (zombieGridPos.ValueRO.Row == gridPos.ValueRO.Row && 
                        zombieTransform.ValueRO.Position.x > transform.ValueRO.Position.x)
                    {
                        hasZombieInLane = true;
                        break;
                    }
                }

                // 如果有僵尸，发射子弹
                if (hasZombieInLane && plant.ValueRO.Type != PlantType.Sunflower && plant.ValueRO.Type != PlantType.WallNut)
                {
                    // 创建子弹实体
                    Entity projectileEntity = ecb.CreateEntity();
                    
                    ecb.AddComponent(projectileEntity, new ProjectileComponent
                    {
                        Damage = plant.ValueRO.AttackDamage,
                        Speed = 5f,
                        Direction = new float3(1, 0, 0),
                        Type = ProjectileType.Pea,
                        Lane = gridPos.ValueRO.Row
                    });

                    ecb.AddComponent(projectileEntity, LocalTransform.FromPosition(
                        transform.ValueRO.Position + new float3(0.5f, 0.5f, 0)));

                    // 更新最后攻击时间
                    plant.ValueRW.LastAttackTime = currentTime;
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
