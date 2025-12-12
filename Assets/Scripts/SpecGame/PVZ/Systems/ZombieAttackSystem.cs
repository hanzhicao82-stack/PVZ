using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Common;
using Framework;
using Game.TowerDefense;

namespace PVZ
{
    /// <summary>
    /// 僵尸攻击系统 - 处理僵尸攻击植物的逻辑
    /// 优化：使用行分组减少碰撞检测次�?
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(ZombieMovementSystem))]
    public partial class ZombieAttackSystem : Common.AttackSystemBase
    {
        private const float ATTACK_RANGE = 0.8f; // 攻击范围
        private const float ATTACK_RANGE_SQ = ATTACK_RANGE * ATTACK_RANGE;

        protected override void ExecuteAttack()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            EntityCommandBuffer ecb = CreateCommandBuffer();

            // 构建植物的行索引 - 按行分组植物
            NativeParallelMultiHashMap<int, Entity> plantsByLane = new NativeParallelMultiHashMap<int, Entity>(100, Allocator.Temp);
            foreach (var (gridPos, plantEntity) in 
                SystemAPI.Query<RefRO<GridPositionComponent>>()
                .WithAll<PlantComponent>()
                .WithEntityAccess())
            {
                plantsByLane.Add(gridPos.ValueRO.Row, plantEntity);
            }

            // 遍历所有僵尸，检查是否遇到同一行的植物
            foreach (var (zombie, zombieTransform, gridPos, zombieEntity) in 
                SystemAPI.Query<RefRW<ZombieComponent>, RefRO<LocalTransform>, RefRO<GridPositionComponent>>()
                .WithEntityAccess())
            {
                int lane = gridPos.ValueRO.Row;
                bool isAttacking = false;

                // 只检查同一行的植物
                if (plantsByLane.TryGetFirstValue(lane, out Entity plantEntity, out var iterator))
                {
                    float3 zombiePos = zombieTransform.ValueRO.Position;

                    do
                    {
                        // 检查植物是否还存在且有效
                        if (!EntityManager.Exists(plantEntity))
                            continue;

                        var plantTransform = SystemAPI.GetComponent<LocalTransform>(plantEntity);
                        float distanceSq = math.distancesq(zombiePos.xz, plantTransform.Position.xz);

                        // 如果僵尸在攻击范围内
                        if (distanceSq < ATTACK_RANGE_SQ)
                        {
                            isAttacking = true;

                            // 检查攻击冷却时间
                            if (currentTime - zombie.ValueRO.LastAttackTime >= zombie.ValueRO.AttackInterval)
                            {
                                // 获取植物健康组件并造成伤害
                                var health = SystemAPI.GetComponent<HealthComponent>(plantEntity);
                                health.CurrentHealth -= zombie.ValueRO.AttackDamage;
                                GameLogger.Log("ZombieAttackSystem", $"僵尸攻击植物 Lane={lane} 伤害={zombie.ValueRO.AttackDamage}");

                                // 如果植物死亡
                                if (health.CurrentHealth <= 0)
                                {
                                    health.IsDead = true;
                                    ecb.DestroyEntity(plantEntity);
                                    GameLogger.Log("ZombieAttackSystem", $"植物被摧毁 Lane={lane}");
                                }
                                else
                                {
                                    // 更新植物健康
                                    SystemAPI.SetComponent(plantEntity, health);
                                }

                                // 更新僵尸最后攻击时间
                                zombie.ValueRW.LastAttackTime = currentTime;
                            }

                            // 找到一个可攻击的植物，停止继续搜索
                            break;
                        }
                    }
                    while (plantsByLane.TryGetNextValue(out plantEntity, ref iterator));
                }

                // 如果僵尸正在攻击，设置移动速度为0，否则恢复正常速度
                if (isAttacking)
                {
                    zombie.ValueRW.MovementSpeed = 0f;
                }
                else
                {
                    // 僵尸没有攻击目标时恢复移动速度
                    if (zombie.ValueRO.MovementSpeed == 0f)
                    {
                        zombie.ValueRW.MovementSpeed = GetDefaultSpeed(zombie.ValueRO.Type);
                    }
                }
            }

            plantsByLane.Dispose();
            PlaybackAndDispose(ecb);
        }

        /// <summary>
        /// 根据僵尸类型获取默认移动速度
        /// </summary>
        private float GetDefaultSpeed(ZombieType type)
        {
            return type switch
            {
                ZombieType.Normal => 1.0f,
                ZombieType.ConeHead => 0.8f,
                ZombieType.BucketHead => 0.6f,
                ZombieType.Flag => 1.5f,
                ZombieType.Newspaper => 1.2f,
                _ => 1.0f
            };
        }
    }
}
