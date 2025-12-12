using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using PVZ;
using Game.TowerDefense;

namespace Common
{
    /// <summary>
    /// 血条系�?- 为实体创建和更新头顶血�?UI
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
        private const int UPDATE_FREQUENCY = 2; // �?帧更新一�?

        protected override void OnCreate()
        {
            // 查询需要创建血条的新实体（�?HealthComponent 但没�?HealthBarComponent�?
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

            // 1. 为新实体创建血条（始终执行�?
            CreateHealthBarsForNewEntities();

            // 2. 更新现有血条（降低频率�?
            _frameCounter++;
            if (_frameCounter % UPDATE_FREQUENCY == 0)
            {
                UpdateHealthBars();
            }

            // 3. 清理死亡实体的血条（始终执行�?
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

                // 创建血�?UI
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

        private void UpdateHealthBars()
        {
            if (_existingEntityQuery.IsEmpty)
                return;

            var healthBarManager = _healthBarManager;
            var mainCamera = _mainCamera;
            
            // 计算视锥体平面（用于快速剔除）
            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

            foreach (var (healthBarComp, health, transform, entity) in 
                SystemAPI.Query<RefRW<HealthBarComponent>, RefRO<HealthComponent>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (!healthBarComp.ValueRO.IsCreated)
                    continue;

                // 视锥剔除：快速检查是否在相机视野内
                var worldPos = transform.ValueRO.Position;
                var bounds = new Bounds(new Vector3(worldPos.x, worldPos.y, worldPos.z), Vector3.one * 2f);
                bool isVisible = GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);

                // 从 HealthBarManager 获取血条 GameObject
                if (!healthBarManager._healthBars.TryGetValue(healthBarComp.ValueRO.HealthBarInstanceID, out var healthBar))
                {
                    healthBarComp.ValueRW.IsCreated = false;
                    continue;
                }

                if (healthBar != null)
                {
                    if (isVisible)
                    {
                        healthBarManager.UpdateHealthBar(
                            healthBar,
                            transform.ValueRO.Position,
                            healthBarComp.ValueRO.YOffset,
                            health.ValueRO.CurrentHealth,
                            health.ValueRO.MaxHealth
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
            }
        }

        private void CleanupDeadEntityHealthBars()
        {
            var healthBarManager = _healthBarManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (healthBarComp, health, entity) in 
                SystemAPI.Query<RefRO<HealthBarComponent>, RefRO<HealthComponent>>().WithEntityAccess())
            {
                if (health.ValueRO.IsDead && healthBarComp.ValueRO.IsCreated)
                {
                    // 销毁血条 UI
                    healthBarManager.DestroyHealthBar(healthBarComp.ValueRO.HealthBarInstanceID);

                    // 移除组件
                    ecb.RemoveComponent<HealthBarComponent>(entity);
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
        protected override void OnDestroy()
        {
            // 清理所有血�?
            if (_healthBarManager != null)
            {
                _healthBarManager.ClearAllHealthBars();
            }
        }
    }
}




