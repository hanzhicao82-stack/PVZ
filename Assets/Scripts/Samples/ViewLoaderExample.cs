using Unity.Entities;
using Unity.Transforms;
using Common;
using PVZ;

namespace Samples
{
    /// <summary>
    /// 视图加载系统使用示例
    /// </summary>
    public static class ViewLoaderExample
    {
        /// <summary>
        /// 示例 1: 创建�?Spine 视图的僵�?
        /// </summary>
        public static Entity CreateZombieWithSpineView(EntityManager entityManager)
        {
            // 1. 创建僵尸实体
            Entity zombie = entityManager.CreateEntity();

            // 2. 添加基础组件
            entityManager.AddComponentData(zombie, new LocalTransform
            {
                Position = new Unity.Mathematics.float3(10, 0, 2),
                Rotation = Unity.Mathematics.quaternion.identity,
                Scale = 1.0f
            });

            entityManager.AddComponentData(zombie, new ZombieComponent
            {
                Type = ZombieType.Normal,
                MovementSpeed = 1.0f,
                AttackDamage = 10f,
                AttackInterval = 1.5f,
                Lane = 2
            });

            entityManager.AddComponentData(zombie, new HealthComponent
            {
                MaxHealth = 100f,
                CurrentHealth = 100f,
                IsDead = false
            });

            // 3. 添加视图预制体组件（ViewLoaderSystem 会自动加载）
            entityManager.AddComponentData(zombie, new ViewPrefabComponent
            {
                PrefabPath = "Prefabs/Zombies/NormalZombie_Spine", // Resources 路径
                IsViewLoaded = false
            });

            // ViewLoaderSystem 会自动：
            // - 加载预制�?
            // - 实例�?GameObject
            // - 添加 SpineRenderComponent
            // - 添加 ViewInstanceComponent
            // - 添加 ViewStateComponent

            return zombie;
        }

        /// <summary>
        /// 示例 2: 创建�?MeshRenderer 视图的植�?
        /// </summary>
        public static Entity CreatePlantWithMeshView(EntityManager entityManager)
        {
            // 1. 创建植物实体
            Entity plant = entityManager.CreateEntity();

            // 2. 添加基础组件
            entityManager.AddComponentData(plant, new LocalTransform
            {
                Position = new Unity.Mathematics.float3(5, 0, 2),
                Rotation = Unity.Mathematics.quaternion.identity,
                Scale = 1.0f
            });

            entityManager.AddComponentData(plant, new PlantComponent
            {
                Type = PlantType.Peashooter,
                AttackDamage = 20f,
                AttackInterval = 1.5f,
                AttackRange = 10f,
                SunCost = 100
            });

            entityManager.AddComponentData(plant, new HealthComponent
            {
                MaxHealth = 300f,
                CurrentHealth = 300f,
                IsDead = false
            });

            entityManager.AddComponentData(plant, new GridPositionComponent
            {
                Row = 2,
                Column = 5
            });

            // 3. 添加视图预制体组�?
            entityManager.AddComponentData(plant, new ViewPrefabComponent
            {
                PrefabPath = "Prefabs/Plants/Peashooter_Mesh", // Resources 路径
                IsViewLoaded = false
            });

            return plant;
        }

        /// <summary>
        /// 示例 3: 手动更新视图动画
        /// </summary>
        public static void UpdateViewAnimation(EntityManager entityManager, Entity entity)
        {
            if (entityManager.HasComponent<ViewStateComponent>(entity))
            {
                var viewState = entityManager.GetComponentData<ViewStateComponent>(entity);
                
                // 切换到攻击动�?
                viewState.CurrentAnimationState = AnimationState.Attack;
                viewState.NeedsAnimationUpdate = true;
                
                entityManager.SetComponentData(entity, viewState);
            }
        }

        /// <summary>
        /// 示例 4: 批量创建带视图的实体
        /// </summary>
        public static void CreateMultipleZombiesWithViews(EntityManager entityManager, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Entity zombie = entityManager.CreateEntity();

                entityManager.AddComponentData(zombie, new LocalTransform
                {
                    Position = new Unity.Mathematics.float3(10 + i * 2, 0, i % 5),
                    Rotation = Unity.Mathematics.quaternion.identity,
                    Scale = 1.0f
                });

                entityManager.AddComponentData(zombie, new ZombieComponent
                {
                    Type = ZombieType.Normal,
                    MovementSpeed = 1.0f,
                    AttackDamage = 10f,
                    Lane = i % 5
                });

                entityManager.AddComponentData(zombie, new HealthComponent
                {
                    MaxHealth = 100f,
                    CurrentHealth = 100f
                });

                // 所有僵尸共享同一个预制体路径
                entityManager.AddComponentData(zombie, new ViewPrefabComponent
                {
                    PrefabPath = "Prefabs/Zombies/NormalZombie_Spine",
                    IsViewLoaded = false
                });
            }

            // ViewLoaderSystem 会在下一帧自动为所有僵尸加载视�?
        }
    }
}
