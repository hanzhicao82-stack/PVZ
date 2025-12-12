using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Common;
using Framework;
using PVZ;

namespace Game.TowerDefense
{
    /// <summary>
    /// 子弹击中系统 - 处理子弹碰撞和伤害计�?
    /// 优化：使用行分组减少碰撞检测次�?
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileMovementSystem))]
    public partial struct ProjectileHitSystem : ISystem
    {
        private const float COLLISION_RADIUS = 0.5f;
        private const float COLLISION_RADIUS_SQ = COLLISION_RADIUS * COLLISION_RADIUS;

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            // 构建僵尸的行索引 - 只需要遍历一次僵�?
            NativeParallelMultiHashMap<int, Entity> zombiesByLane = new NativeParallelMultiHashMap<int, Entity>(100, Allocator.Temp);
            
            foreach (var (gridPos, zombieEntity) in 
                SystemAPI.Query<RefRO<GridPositionComponent>>()
                .WithAll<ZombieComponent>()
                .WithEntityAccess())
            {
                zombiesByLane.Add(gridPos.ValueRO.Row, zombieEntity);
            }

            // 遍历所有子弹，只检查同一行的僵尸
            foreach (var (projectile, projectileTransform, projectileEntity) in 
                SystemAPI.Query<RefRO<ProjectileComponent>, RefRO<LocalTransform>>()
                .WithEntityAccess())
            {
                int lane = projectile.ValueRO.Lane;
                
                // 只获取同一行的僵尸
                if (zombiesByLane.TryGetFirstValue(lane, out Entity zombieEntity, out var iterator))
                {
                    float3 projectilePos = projectileTransform.ValueRO.Position;
                    bool hitTarget = false;

                    do
                    {
                        // 检查僵尸是否还存在且有�?
                        if (!state.EntityManager.Exists(zombieEntity))
                            continue;

                        var zombieTransform = SystemAPI.GetComponent<LocalTransform>(zombieEntity);
                        
                        // 使用平方距离避免开方运�?
                        float distanceSq = math.distancesq(projectilePos.xz, zombieTransform.Position.xz);

                        if (distanceSq < COLLISION_RADIUS_SQ)
                        {
                            // 获取健康组件并造成伤害
                            var health = SystemAPI.GetComponent<HealthComponent>(zombieEntity);
                            health.CurrentHealth -= projectile.ValueRO.Damage;
                            
                            // 如果僵尸死亡
                            if (health.CurrentHealth <= 0)
                            {
                                health.IsDead = true;
                                ecb.DestroyEntity(zombieEntity);
                                GameLogger.Log("ProjectileHitSystem", $"僵尸被击杀 Lane={lane}");
                            }
                            else
                            {
                                // 更新健康�?
                                SystemAPI.SetComponent(zombieEntity, health);
                            }
                            
                            // 销毁子�?
                            ecb.DestroyEntity(projectileEntity);
                            hitTarget = true;
                            break;
                        }
                    }
                    while (zombiesByLane.TryGetNextValue(out zombieEntity, ref iterator) && !hitTarget);
                }
            }

            zombiesByLane.Dispose();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
