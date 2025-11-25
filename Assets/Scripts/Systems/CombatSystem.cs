using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Systems
{
    /// <summary>
    /// 战斗系统 - 处理子弹碰撞和伤害计算
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileMovementSystem))]
    public partial struct CombatSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            // 遍历所有子弹
            foreach (var (projectile, projectileTransform, projectileEntity) in 
                SystemAPI.Query<RefRO<ProjectileComponent>, RefRO<LocalTransform>>()
                .WithEntityAccess())
            {
                // 检查与僵尸的碰撞
                foreach (var (health, zombieTransform, zombieGridPos, zombieEntity) in 
                    SystemAPI.Query<RefRW<HealthComponent>, RefRO<LocalTransform>, RefRO<GridPositionComponent>>()
                    .WithAll<ZombieComponent>()
                    .WithEntityAccess())
                {
                    // 检查是否在同一行
                    if (projectile.ValueRO.Lane != zombieGridPos.ValueRO.Row)
                        continue;

                    // 简单的碰撞检测
                    float distance = math.distance(
                        projectileTransform.ValueRO.Position.xy,
                        zombieTransform.ValueRO.Position.xy);

                    if (distance < 0.5f)
                    {
                        // 造成伤害
                        health.ValueRW.CurrentHealth -= projectile.ValueRO.Damage;
                        
                        // 销毁子弹
                        ecb.DestroyEntity(projectileEntity);
                        
                        // 如果僵尸死亡，标记为死亡
                        if (health.ValueRO.CurrentHealth <= 0)
                        {
                            health.ValueRW.IsDead = true;
                            ecb.DestroyEntity(zombieEntity);
                        }
                        
                        break;
                    }
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
