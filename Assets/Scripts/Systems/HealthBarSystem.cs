using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using PVZ.DOTS.Components;
using UnityEngine;

namespace PVZ.DOTS.Systems
{
    /// <summary>
    /// 血条系统 - 为实体创建和更新头顶血条 UI
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial class HealthBarSystem : SystemBase
    {
        private HealthBarManager _healthBarManager;
        private EntityQuery _newEntityQuery;
        private EntityQuery _existingEntityQuery;
        private EntityQuery _deadEntityQuery;
        private Camera _mainCamera;
        private int _frameCounter = 0;
        private const int UPDATE_FREQUENCY = 2; // 每2帧更新一次

        protected override void OnCreate()
        {
            // 查询需要创建血条的新实体（有 HealthComponent 但没有 HealthBarComponent）
            _newEntityQuery = GetEntityQuery(
                ComponentType.ReadOnly<HealthComponent>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.Exclude<HealthBarComponent>(),
                ComponentType.Exclude<ProjectileComponent>() // 排除子弹
            );

            // 查询已有血条的实体
            _existingEntityQuery = GetEntityQuery(
                ComponentType.ReadOnly<HealthComponent>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadWrite<HealthBarComponent>()
            );

            // 查询已死亡的实体
            _deadEntityQuery = GetEntityQuery(
                ComponentType.ReadOnly<HealthComponent>(),
                ComponentType.ReadWrite<HealthBarComponent>()
            );
        }

        protected override void OnStartRunning()
        {
            _healthBarManager = HealthBarManager.Instance;
            if (_healthBarManager != null)
            {
                _healthBarManager.EnsureInitialized();
                UnityEngine.Debug.Log("[HealthBarSystem] OnStartRunning - HealthBarManager initialized");
            }
            else
            {
                UnityEngine.Debug.LogError("[HealthBarSystem] OnStartRunning - Failed to get HealthBarManager instance!");
            }
            
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                UnityEngine.Debug.LogWarning("[HealthBarSystem] Main camera not found!");
            }
        }

        protected override void OnUpdate()
        {
            if (_healthBarManager == null)
            {
                _healthBarManager = HealthBarManager.Instance;
                if (_healthBarManager != null)
                {
                    _healthBarManager.EnsureInitialized();
                }
                return;
            }

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            // 1. 为新实体创建血条（始终执行）
            CreateHealthBarsForNewEntities();

            // 2. 更新现有血条（降低频率）
            _frameCounter++;
            if (_frameCounter % UPDATE_FREQUENCY == 0)
            {
                UpdateExistingHealthBars();
            }

            // 3. 清理死亡实体的血条（始终执行）
            CleanupDeadEntityHealthBars();
        }

        private void CreateHealthBarsForNewEntities()
        {
            if (_newEntityQuery.IsEmpty)
                return;

            var entities = _newEntityQuery.ToEntityArray(Allocator.Temp);
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                float yOffset = _healthBarManager.defaultYOffset;

                // 根据实体类型调整偏移
                if (EntityManager.HasComponent<ZombieComponent>(entity))
                {
                    yOffset = 2.5f;
                }
                else if (EntityManager.HasComponent<PlantComponent>(entity))
                {
                    yOffset = 1.5f;
                }

                // 创建血条 UI
                var healthBar = _healthBarManager.CreateHealthBar(entity, yOffset);
                if (healthBar != null)
                {
                    // 使用 ECB 批量添加组件
                    ecb.AddComponent(entity, new HealthBarComponent
                    {
                        HealthBarInstanceID = healthBar.GetInstanceID(),
                        IsCreated = true,
                        YOffset = yOffset
                    });
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
            entities.Dispose();
        }

        private void UpdateExistingHealthBars()
        {
            if (_existingEntityQuery.IsEmpty)
                return;

            var healthBarManager = _healthBarManager;
            var mainCamera = _mainCamera;
            
            // 计算视锥体平面（用于快速剔除）
            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

            Entities
                .WithAll<HealthComponent, LocalTransform, HealthBarComponent>()
                .WithoutBurst()
                .ForEach((
                    Entity entity,
                    ref HealthBarComponent healthBarComp,
                    in HealthComponent health,
                    in LocalTransform transform) =>
                {
                    if (!healthBarComp.IsCreated)
                        return;

                    // 视锥剔除：快速检查是否在相机视野内
                    var worldPos = transform.Position;
                    var bounds = new Bounds(new Vector3(worldPos.x, worldPos.y, worldPos.z), Vector3.one * 2f);
                    bool isVisible = GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);

                    // 从 HealthBarManager 获取血条 GameObject
                    if (!healthBarManager._healthBars.TryGetValue(healthBarComp.HealthBarInstanceID, out var healthBar))
                    {
                        healthBarComp.IsCreated = false;
                        return;
                    }

                    if (healthBar != null)
                    {
                        if (isVisible)
                        {
                            healthBarManager.UpdateHealthBar(
                                healthBar,
                                transform.Position,
                                healthBarComp.YOffset,
                                health.CurrentHealth,
                                health.MaxHealth
                            );
                        }
                        else
                        {
                            // 屏幕外的血条直接隐藏，不更新位置
                            if (healthBar.activeSelf)
                            {
                                healthBar.SetActive(false);
                            }
                        }
                    }
                }).Run();
        }

        private void CleanupDeadEntityHealthBars()
        {
            var healthBarManager = _healthBarManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            Entities
                .WithAll<HealthComponent, HealthBarComponent>()
                .WithoutBurst()
                .ForEach((
                    Entity entity,
                    in HealthBarComponent healthBarComp,
                    in HealthComponent health) =>
                {
                    if (health.IsDead && healthBarComp.IsCreated)
                    {
                        // 销毁血条 UI
                        healthBarManager.DestroyHealthBar(healthBarComp.HealthBarInstanceID);

                        // 移除组件
                        ecb.RemoveComponent<HealthBarComponent>(entity);
                    }
                }).Run();

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        protected override void OnDestroy()
        {
            // 清理所有血条
            if (_healthBarManager != null)
            {
                _healthBarManager.ClearAllHealthBars();
            }
        }
    }
}
