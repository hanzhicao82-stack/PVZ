using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Systems
{
    /// <summary>
    /// 植物攻击系统 - 处理植物攻击逻辑，包括检测目标、触发攻击状态、发射子弹
    /// 通过 AttackStateComponent 与视图系统解耦，视图系统根据攻击状态显示动画
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class PlantAttackSystem : AttackSystemBase
    {
        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            EntityCommandBuffer ecb = CreateCommandBuffer();

            NativeParallelMultiHashMap<int, float> zombiesByLane = new NativeParallelMultiHashMap<int, float>(64, Allocator.Temp);

            foreach (var (zombieGridPos, zombieTransform) in
                     SystemAPI.Query<RefRO<GridPositionComponent>, RefRO<LocalTransform>>()
                         .WithAll<ZombieComponent>())
            {
                zombiesByLane.Add(zombieGridPos.ValueRO.Row, zombieTransform.ValueRO.Position.x);
            }

            foreach (var (plant, transform, gridPos, entity) in
                     SystemAPI.Query<RefRW<PlantComponent>, RefRO<LocalTransform>, RefRO<GridPositionComponent>>()
                         .WithNone<AttackStateComponent>()
                         .WithEntityAccess())
            {
                if (plant.ValueRO.Type == PlantType.Sunflower || plant.ValueRO.Type == PlantType.WallNut)
                    continue;

                if (currentTime - plant.ValueRO.LastAttackTime < plant.ValueRO.AttackInterval)
                    continue;

                if (!HasTargetAhead(zombiesByLane, gridPos.ValueRO.Row, transform.ValueRO.Position.x))
                    continue;

                ecb.AddComponent(entity, new AttackStateComponent
                {
                    AttackStartTime = currentTime,
                    AttackAnimationDuration = GetAttackAnimationDuration(plant.ValueRO.Type),
                    HasDealtDamage = false
                });

                plant.ValueRW.LastAttackTime = currentTime;
            }

            foreach (var (plant, attackState, transform, gridPos, entity) in
                     SystemAPI.Query<RefRO<PlantComponent>, RefRW<AttackStateComponent>, RefRO<LocalTransform>, RefRO<GridPositionComponent>>()
                         .WithEntityAccess())
            {
                float timeSinceAttackStart = currentTime - attackState.ValueRO.AttackStartTime;

                float damageTimingPercent = 0.4f;
                float damageTime = attackState.ValueRO.AttackAnimationDuration * damageTimingPercent;

                if (!attackState.ValueRO.HasDealtDamage && timeSinceAttackStart >= damageTime)
                {
                    SpawnProjectile(ref ecb, plant.ValueRO, transform.ValueRO, gridPos.ValueRO.Row);
                    attackState.ValueRW.HasDealtDamage = true;
                }

                if (timeSinceAttackStart >= attackState.ValueRO.AttackAnimationDuration)
                {
                    ecb.RemoveComponent<AttackStateComponent>(entity);
                }
            }

            zombiesByLane.Dispose();
            PlaybackAndDispose(ecb);
        }

        private float GetAttackAnimationDuration(PlantType plantType)
        {
            return plantType switch
            {
                PlantType.Peashooter => 0.5f,
                PlantType.SnowPea => 0.5f,
                PlantType.Repeater => 0.8f,
                PlantType.CherryBomb => 1.0f,
                _ => 0.5f
            };
        }

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

            // 子弹视图优化：只在配置启用时创建视图
            // 对于大量子弹场景，建议禁用子弹视图以提升性能
            var viewConfig = Config.ViewSystemConfig.Instance;
            if (viewConfig.enableSpineSystem && !plant.ProjectilePrefabPath.IsEmpty)
            {
                ecb.AddComponent(projectileEntity, new ProjectileViewPrefabComponent
                {
                    PrefabPath = plant.ProjectilePrefabPath
                });
            }
            // 否则子弹将只有逻辑，无视图（性能最优）
        }

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
