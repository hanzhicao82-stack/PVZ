using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Systems
{
    /// <summary>
    /// 植物攻击系统 - 处理植物攻击逻辑，包括检测目标、触发攻击状态、发射子弹
    /// 通过 AttackStateComponent 与视图系统解耦，视图系统根据攻击状态显示动画
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PlantAttackSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            // 第一阶段：检测攻击条件，添加攻击状态
            // 处理没有攻击状态的植物，检查是否需要开始攻击
            foreach (var (plant, transform, gridPos, entity) in 
                SystemAPI.Query<RefRW<PlantComponent>, RefRO<LocalTransform>, RefRO<GridPositionComponent>>()
                .WithNone<AttackStateComponent>()
                .WithEntityAccess())
            {
                // 跳过不攻击的植物类型
                if (plant.ValueRO.Type == PlantType.Sunflower || plant.ValueRO.Type == PlantType.WallNut)
                    continue;

                // 检查攻击冷却时间
                if (currentTime - plant.ValueRO.LastAttackTime < plant.ValueRO.AttackInterval)
                    continue;

                // 检查同一行是否有僵尸
                bool hasZombieInLane = HasZombieInLane(ref state, gridPos.ValueRO.Row, transform.ValueRO.Position.x);

                // 如果有目标，进入攻击状态
                if (hasZombieInLane)
                {
                    ecb.AddComponent(entity, new AttackStateComponent
                    {
                        AttackStartTime = currentTime,
                        AttackAnimationDuration = GetAttackAnimationDuration(plant.ValueRO.Type),
                        HasDealtDamage = false
                    });

                    // 更新最后攻击时间（用于冷却计算）
                    plant.ValueRW.LastAttackTime = currentTime;
                }
            }

            // 第二阶段：处理正在攻击状态的植物
            // 在合适的时机发射子弹，攻击动画结束后移除攻击状态
            foreach (var (plant, attackState, transform, gridPos, entity) in 
                SystemAPI.Query<RefRO<PlantComponent>, RefRW<AttackStateComponent>, RefRO<LocalTransform>, RefRO<GridPositionComponent>>()
                .WithEntityAccess())
            {
                float timeSinceAttackStart = currentTime - attackState.ValueRO.AttackStartTime;

                // 在攻击动画的特定时刻发射子弹（例如动画进行到 40% 时）
                float damageTimingPercent = 0.4f; // 攻击动画进行到 40% 时发射子弹
                float damageTime = attackState.ValueRO.AttackAnimationDuration * damageTimingPercent;

                if (!attackState.ValueRO.HasDealtDamage && timeSinceAttackStart >= damageTime)
                {
                    // 发射子弹
                    SpawnProjectile(ref ecb, plant.ValueRO, transform.ValueRO, gridPos.ValueRO.Row);
                    
                    // 标记已发射子弹
                    attackState.ValueRW.HasDealtDamage = true;
                }

                // 攻击动画结束后，移除攻击状态
                if (timeSinceAttackStart >= attackState.ValueRO.AttackAnimationDuration)
                {
                    ecb.RemoveComponent<AttackStateComponent>(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        /// <summary>
        /// 检查指定行是否有僵尸（且在植物前方）
        /// </summary>
        private bool HasZombieInLane(ref SystemState state, int row, float plantPositionX)
        {
            foreach (var (zombieGridPos, zombieTransform) in 
                SystemAPI.Query<RefRO<GridPositionComponent>, RefRO<LocalTransform>>()
                .WithAll<ZombieComponent>())
            {
                if (zombieGridPos.ValueRO.Row == row && 
                    zombieTransform.ValueRO.Position.x > plantPositionX)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取植物类型的攻击动画持续时间
        /// </summary>
        private float GetAttackAnimationDuration(PlantType plantType)
        {
            return plantType switch
            {
                PlantType.Peashooter => 0.5f,   // 豌豆射手攻击动画 0.5 秒
                PlantType.SnowPea => 0.5f,      // 寒冰射手攻击动画 0.5 秒
                PlantType.Repeater => 0.8f,     // 双发射手攻击动画 0.8 秒（发射两次）
                PlantType.CherryBomb => 1.0f,   // 樱桃炸弹爆炸动画 1 秒
                _ => 0.5f
            };
        }

        /// <summary>
        /// 生成子弹实体
        /// </summary>
        private void SpawnProjectile(ref EntityCommandBuffer ecb, PlantComponent plant, LocalTransform plantTransform, int lane)
        {
            Entity projectileEntity = ecb.CreateEntity();
            
            ecb.AddComponent(projectileEntity, new ProjectileComponent
            {
                Damage = plant.AttackDamage,
                Speed = 5f,
                Direction = new float3(1, 0, 0),
                Type = GetProjectileType(plant.Type),
                Lane = lane
            });

            ecb.AddComponent(projectileEntity, LocalTransform.FromPosition(
                plantTransform.Position + new float3(0.5f, 0.5f, 0)));
        }

        /// <summary>
        /// 根据植物类型获取子弹类型
        /// </summary>
        private ProjectileType GetProjectileType(PlantType plantType)
        {
            return plantType switch
            {
                PlantType.Peashooter => ProjectileType.Pea,
                PlantType.SnowPea => ProjectileType.FrozenPea,
                PlantType.Repeater => ProjectileType.Pea,
                _ => ProjectileType.Pea
            };
        }
    }
}
