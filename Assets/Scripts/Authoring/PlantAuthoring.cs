using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Authoring
{
    /// <summary>
    /// 植物 Authoring - 将 MonoBehaviour 转换为 ECS 组件
    /// </summary>
    public class PlantAuthoring : MonoBehaviour
    {
        [Header("植物类型")]
        public PlantType plantType = PlantType.Peashooter;

        [Header("基础属性")]
        public float attackDamage = 20f;
        public float attackInterval = 1.5f;
        public float attackRange = 10f;
        public int sunCost = 100;

        [Header("网格位置")]
        public int row = 0;
        public int column = 0;

        [Header("生命值")]
        public float maxHealth = 100f;

        [Header("阳光生产（仅向日葵）")]
        public bool isSunProducer = false;
        public float sunProductionInterval = 10f;
        public int sunAmount = 25;

        class Baker : Baker<PlantAuthoring>
        {
            public override void Bake(PlantAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                // 添加植物组件
                AddComponent(entity, new PlantComponent
                {
                    Type = authoring.plantType,
                    AttackDamage = authoring.attackDamage,
                    AttackInterval = authoring.attackInterval,
                    LastAttackTime = 0f,
                    AttackRange = authoring.attackRange,
                    SunCost = authoring.sunCost
                });

                // 添加健康值组件
                AddComponent(entity, new HealthComponent
                {
                    CurrentHealth = authoring.maxHealth,
                    MaxHealth = authoring.maxHealth,
                    IsDead = false
                });

                // 添加网格位置组件
                AddComponent(entity, new GridPositionComponent
                {
                    Row = authoring.row,
                    Column = authoring.column,
                    WorldPosition = authoring.transform.position
                });

                // 如果是阳光生产者，添加相应组件
                if (authoring.isSunProducer)
                {
                    AddComponent(entity, new SunProducerComponent
                    {
                        ProductionInterval = authoring.sunProductionInterval,
                        LastProductionTime = 0f,
                        SunAmount = authoring.sunAmount
                    });
                }
            }
        }
    }
}
