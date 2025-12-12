using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Common;
using Framework;
using AnimationState = Common.AnimationState;

namespace Samples
{
    /// <summary>
    /// 视图系统模块化使用示�?
    /// 展示如何通过单例 Context 使用视图加载和渲染功�?
    /// </summary>
    public class ViewModuleExamples
    {
        private IModuleContext _context;
        private IResourceService _resourceService;
        private IPoolService _poolService;
        private IEventBus _eventBus;
        private World _world;

        public void Initialize()
        {
            // �?GameBootstrap 单例获取全局 Context
            _context = GameBootstrap.Instance?.Context;
            if (_context == null)
            {
                UnityEngine.Debug.LogError("GameBootstrap instance not found!");
                return;
            }

            // 获取依赖服务
            _resourceService = _context.GetService<IResourceService>();
            _poolService = _context.GetService<IPoolService>();
            _eventBus = _context.GetService<IEventBus>();
            _world = _context.GetService<World>();

            // 订阅相关事件
            SubscribeToEvents();
        }

        // ==================== 创建带视图的实体示例 ====================

        /// <summary>
        /// 创建僵尸实体（带 Spine 视图�?
        /// </summary>
        public Entity CreateZombieWithView(float3 position)
        {
            var entityManager = _world.EntityManager;

            // 创建实体
            var zombie = entityManager.CreateEntity();

            // 添加 Transform 组件
            entityManager.AddComponentData(zombie, new LocalTransform
            {
                Position = position,
                Rotation = quaternion.identity,
                Scale = 1f
            });

            // 添加视图预制体组件（ViewLoaderSystem 会自动加载）
            entityManager.AddComponentData(zombie, new ViewPrefabComponent
            {
                PrefabPath = "Res/Spine/Zombie_Normal",
                IsViewLoaded = false
            });

            // 添加视图状态组�?
            entityManager.AddComponentData(zombie, new ViewStateComponent
            {
                CurrentAnimationState = AnimationState.Walk,
                NeedsAnimationUpdate = true,
                ColorTint = 1.0f,
                LastAppliedColorTint = 1.0f
            });

            UnityEngine.Debug.Log($"创建僵尸实体，位�? {position}");
            return zombie;
        }

        /// <summary>
        /// 创建植物实体（带 Spine 视图�?
        /// </summary>
        public Entity CreatePlantWithView(float3 position, string plantType)
        {
            var entityManager = _world.EntityManager;

            var plant = entityManager.CreateEntity();

            entityManager.AddComponentData(plant, new LocalTransform
            {
                Position = position,
                Rotation = quaternion.identity,
                Scale = 1f
            });

            entityManager.AddComponentData(plant, new ViewPrefabComponent
            {
                PrefabPath = $"Res/Spine/Plant_{plantType}",
                IsViewLoaded = false
            });

            entityManager.AddComponentData(plant, new ViewStateComponent
            {
                CurrentAnimationState = AnimationState.Idle,
                NeedsAnimationUpdate = true,
                ColorTint = 1.0f,
                LastAppliedColorTint = 1.0f
            });

            // 发布植物种植事件
            _eventBus.Publish(new PlantPlacedEvent
            {
                PlantType = plantType,
                Row = (int)position.z,
                Column = (int)position.x,
                SunCost = 100
            });

            UnityEngine.Debug.Log($"创建植物实体: {plantType}，位�? {position}");
            return plant;
        }

        // ==================== 视图状态控制示�?====================

        /// <summary>
        /// 更改实体的动画状�?
        /// </summary>
        public void ChangeEntityAnimation(Entity entity, AnimationState newState)
        {
            var entityManager = _world.EntityManager;

            if (!entityManager.HasComponent<ViewStateComponent>(entity))
            {
                UnityEngine.Debug.LogWarning("实体没有 ViewStateComponent，无法更改动画");
                return;
            }

            var viewState = entityManager.GetComponentData<ViewStateComponent>(entity);
            viewState.CurrentAnimationState = newState;
            viewState.NeedsAnimationUpdate = true;
            entityManager.SetComponentData(entity, viewState);

            UnityEngine.Debug.Log($"更改实体动画: {newState}");
        }

        /// <summary>
        /// 设置实体颜色（用于受伤效果）
        /// </summary>
        public void SetEntityColorTint(Entity entity, float tint)
        {
            var entityManager = _world.EntityManager;

            if (!entityManager.HasComponent<ViewStateComponent>(entity))
                return;

            var viewState = entityManager.GetComponentData<ViewStateComponent>(entity);
            viewState.ColorTint = tint;
            entityManager.SetComponentData(entity, viewState);
        }

        // ==================== 批量创建示例 ====================

        /// <summary>
        /// 批量创建僵尸（用于波次生成）
        /// </summary>
        public void SpawnZombieWave(int zombieCount, float startX, float spacing)
        {
            for (int i = 0; i < zombieCount; i++)
            {
                float3 position = new float3(startX + i * spacing, 0, UnityEngine.Random.Range(0, 5));
                CreateZombieWithView(position);
            }

            UnityEngine.Debug.Log($"生成僵尸波次: {zombieCount} 只僵尸");
        }

        // ==================== 事件响应示例 ====================

        private void SubscribeToEvents()
        {
            // 僵尸死亡时播放死亡动�?
            _eventBus.Subscribe<ZombieDeathEvent>(OnZombieDeath);

            // 植物受伤时闪烁效�?
            _eventBus.Subscribe<PlantDamagedEvent>(OnPlantDamaged);
        }

        private void OnZombieDeath(ZombieDeathEvent evt)
        {
            // 这里需要找到对应的实体
            // 假设 evt 包含 Entity 引用
            // ChangeEntityAnimation(evt.Entity, AnimationState.Death);

            UnityEngine.Debug.Log($"僵尸死亡事件: 位置 {evt.Position}");
        }

        private void OnPlantDamaged(PlantDamagedEvent evt)
        {
            // 受伤时变�?
            // SetEntityColorTint(evt.Entity, 0.5f);

            // 0.2秒后恢复
            // TODO: 使用协程或定时器恢复颜色

            UnityEngine.Debug.Log($"植物受伤事件: 伤害 {evt.Damage}");
        }

        // ==================== 性能优化示例 ====================

        /// <summary>
        /// 预加载视图资源（关卡开始时�?
        /// </summary>
        public void PreloadViewAssets(string[] prefabPaths)
        {
            _resourceService.PreloadAssets(prefabPaths, () =>
            {
                UnityEngine.Debug.Log("视图资源预加载完成");
            });
        }

        /// <summary>
        /// 清理所有视图（关卡结束时）
        /// </summary>
        public void CleanupAllViews()
        {
            var viewLoaderSystem = _world?.GetExistingSystemManaged<ViewLoaderSystem>();
            if (viewLoaderSystem != null)
            {
                viewLoaderSystem.CleanupAllViews();
                UnityEngine.Debug.Log("所有视图已清理");
            }
        }

        // ==================== 完整使用场景示例 ====================

        /// <summary>
        /// 完整的关卡启动流�?
        /// </summary>
        public void StartLevel(int levelId)
        {
            // 1. 预加载资�?
            string[] assetsToPreload = new[]
            {
                "Res/Spine/Zombie_Normal",
                "Res/Spine/Zombie_Conehead",
                "Res/Spine/Plant_Peashooter",
                "Res/Spine/Plant_Sunflower"
            };

            _resourceService.PreloadAssets(assetsToPreload, () =>
            {
                // 2. 创建初始植物
                CreatePlantWithView(new float3(2, 0, 2), "Sunflower");
                CreatePlantWithView(new float3(3, 0, 2), "Peashooter");

                // 3. 延迟生成第一波僵�?
                // TODO: 使用定时器系�?
                // Timer.Delay(5f, () => SpawnZombieWave(5, 15, 2));

                // 4. 发布关卡开始事�?
                _eventBus.Publish(new GameStartedEvent
                {
                    LevelId = levelId,
                    LevelName = $"关卡 {levelId}"
                });

                UnityEngine.Debug.Log($"关卡 {levelId} 启动完成");
            });
        }

        /// <summary>
        /// 僵尸攻击植物的完整流�?
        /// </summary>
        public void ZombieAttackPlant(Entity zombieEntity, Entity plantEntity)
        {
            var entityManager = _world.EntityManager;

            // 1. 僵尸切换到攻击动�?
            ChangeEntityAnimation(zombieEntity, AnimationState.Attack);

            // 2. 植物受伤变红
            SetEntityColorTint(plantEntity, 0.5f);

            // 3. 发布攻击事件
            var targetPos = entityManager.GetComponentData<LocalTransform>(plantEntity).Position;
            UnityEngine.Debug.Log($"僵尸攻击植物: 位置 {targetPos}");

            // 4. 检查植物是否死�?
            // if (plantHealth <= 0)
            // {
            //     ChangeEntityAnimation(plantEntity, PVZ.DOTS.Components.AnimationState.Death);
            // }
        }

        // ==================== 调试和监�?====================

        /// <summary>
        /// 获取当前加载的视图数�?
        /// </summary>
        public int GetLoadedViewCount()
        {
            var entityManager = _world.EntityManager;
            var query = entityManager.CreateEntityQuery(typeof(ViewInstanceComponent));
            int count = query.CalculateEntityCount();
            query.Dispose();
            return count;
        }

        /// <summary>
        /// 打印视图系统状�?
        /// </summary>
        public void PrintViewSystemStatus()
        {
            var viewLoaderSystem = _world?.GetExistingSystemManaged<ViewLoaderSystem>();
            var spineViewSystem = _world?.GetExistingSystemManaged<SpineViewSystem>();

            UnityEngine.Debug.Log("=== 视图系统状�?===");
            UnityEngine.Debug.Log($"ViewLoaderSystem 启用: {viewLoaderSystem?.Enabled}");
            UnityEngine.Debug.Log($"SpineViewSystem 启用: {spineViewSystem?.Enabled}");
            UnityEngine.Debug.Log($"已加载视图数�? {GetLoadedViewCount()}");
        }
    }
}

