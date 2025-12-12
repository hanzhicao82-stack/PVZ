using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Common;

namespace PVZ
{
    /// <summary>
    /// 僵尸 Authoring - �?MonoBehaviour 转换�?ECS 组件
    /// </summary>
    public class ZombieAuthoring : MonoBehaviour
    {
        [Header("僵尸类型")]
        public ZombieType zombieType = ZombieType.Normal;

        [Header("移动属性")]
        public float movementSpeed = 1f;

        [Header("攻击属性")]
        public float attackDamage = 10f;
        public float attackInterval = 1f;

        [Header("生命值")]
        public float maxHealth = 100f;

        [Header("所在行")]
        public int lane = 0;

        [Header("子弹资源路径 (Resources �?AssetDatabase 路径)")]
        public string projectilePrefabPath = string.Empty;

        class Baker : Baker<ZombieAuthoring>
        {
            public override void Bake(ZombieAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                // 添加僵尸组件
                AddComponent(entity, new ZombieComponent
                {
                    Type = authoring.zombieType,
                    MovementSpeed = authoring.movementSpeed,
                    AttackDamage = authoring.attackDamage,
                    AttackInterval = authoring.attackInterval,
                    LastAttackTime = 0f,
                    Lane = authoring.lane,
                    ProjectilePrefabPath = new FixedString128Bytes(authoring.projectilePrefabPath ?? string.Empty)
                });

                // 添加健康值组�?
                AddComponent(entity, new HealthComponent
                {
                    CurrentHealth = authoring.maxHealth,
                    MaxHealth = authoring.maxHealth,
                    IsDead = false
                });

                // 添加网格位置组件
                AddComponent(entity, new GridPositionComponent
                {
                    Row = authoring.lane,
                    Column = 9, // 僵尸从右侧生�?
                    WorldPosition = authoring.transform.position
                });
            }
        }
    }
}
