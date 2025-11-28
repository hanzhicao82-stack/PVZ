using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using PVZ.DOTS.Components;
using PVZ.DOTS.Utils;

namespace PVZ.DOTS.Systems
{
    /// <summary>
    /// 视图加载系统 - 负责为实体加载和绑定视图模型
    /// 在 InitializationSystemGroup 中更新，确保在游戏逻辑之前完成视图加载
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(BeginInitializationEntityCommandBufferSystem))]
    public partial class ViewLoaderSystem : SystemBase
    {
        private EndInitializationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystemManaged<EndInitializationEntityCommandBufferSystem>();
            
            // 根据配置决定是否启用
            var config = Config.ViewSystemConfig.Instance;
            if (!config.enableSpineSystem && !config.enableMeshRendererSystem)
            {
                Enabled = false;
                UnityEngine.Debug.Log("ViewLoaderSystem is disabled (no view rendering enabled).");
            }
        }

        protected override void OnUpdate()
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            // 处理需要加载视图的实体
            foreach (var (viewPrefab, transform, entity) in
                SystemAPI.Query<RefRW<ViewPrefabComponent>, RefRO<LocalTransform>>()
                .WithNone<ViewInstanceComponent>()
                .WithEntityAccess())
            {
                if (!viewPrefab.ValueRO.IsViewLoaded)
                {
                    // 标记为正在加载，避免重复加载
                    viewPrefab.ValueRW.IsViewLoaded = true;

                    // 通过 ECB 添加加载请求（实际加载在主线程）
                    ecb.AddComponent(entity, new ViewLoadRequestTag());
                }
            }

            // 在主线程中实际加载 GameObject（无法在 Job 中执行）
            foreach (var (viewPrefab, transform, entity) in
                SystemAPI.Query<RefRO<ViewPrefabComponent>, RefRO<LocalTransform>>()
                .WithAll<ViewLoadRequestTag>()
                .WithNone<ViewInstanceComponent>()
                .WithEntityAccess())
            {
                LoadViewForEntity(entity, viewPrefab.ValueRO, transform.ValueRO);
                
                // 移除加载请求标记
                ecb.RemoveComponent<ViewLoadRequestTag>(entity);
            }

            // 更新已加载视图的位置
            foreach (var (transform, entity) in
                SystemAPI.Query<RefRO<LocalTransform>>()
                .WithAll<ViewInstanceComponent>()
                .WithEntityAccess())
            {
                if (EntityManager.HasComponent<ViewInstanceComponent>(entity))
                {
                    var viewInstance = EntityManager.GetComponentData<ViewInstanceComponent>(entity);
                    if (viewInstance.GameObjectInstance != null)
                    {
                        // 同步位置和旋转
                        viewInstance.GameObjectInstance.transform.position = transform.ValueRO.Position;
                        viewInstance.GameObjectInstance.transform.rotation = transform.ValueRO.Rotation;
                        viewInstance.GameObjectInstance.transform.localScale = new Vector3(
                            transform.ValueRO.Scale, transform.ValueRO.Scale, transform.ValueRO.Scale);
                    }
                }
            }
        }

        /// <summary>
        /// 为实体加载视图模型
        /// </summary>
        private void LoadViewForEntity(Entity entity, in ViewPrefabComponent viewPrefab, in LocalTransform transform)
        {
            // 从 Resources 加载预制体
            string prefabPath = viewPrefab.PrefabPath.ToString();
            GameObject prefab = Resources.Load<GameObject>(prefabPath);

            if (prefab == null)
            {
                GameLogger.LogWarning("ViewLoaderSystem", $"无法加载预制体: {prefabPath}");
                return;
            }

            // 实例化 GameObject
            GameObject instance = Object.Instantiate(prefab);
            instance.transform.position = transform.Position;
            instance.transform.rotation = transform.Rotation;
            instance.transform.localScale = new Vector3(transform.Scale, transform.Scale, transform.Scale);
            instance.name = $"{prefab.name}_{entity.Index}";

            // 创建视图实例组件
            var viewInstance = new ViewInstanceComponent
            {
                GameObjectInstance = instance
            };

            // 自动检测渲染类型（根据预制体上的组件）
            // 优先检测 Spine 组件
            var spineComponent = instance.GetComponent<Spine.Unity.SkeletonAnimation>();
            if (spineComponent != null)
            {
                viewInstance.SpineSkeletonAnimation = spineComponent;
                
                // 添加 Spine 渲染组件标记
                EntityManager.AddComponentData(entity, new SpineRenderComponent
                {
                    GameObjectEntity = entity
                });

                GameLogger.Log("ViewLoaderSystem", $"加载 Spine 视图: {prefabPath}");
            }
            else
            {
                // 尝试获取 MeshRenderer
                var meshRenderer = instance.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    viewInstance.MeshRendererComponent = meshRenderer;
                    
                    // 添加 MeshRenderer 渲染组件标记
                    EntityManager.AddComponentData(entity, new MeshRenderComponent
                    {
                        GameObjectEntity = entity
                    });

                    GameLogger.Log("ViewLoaderSystem", $"加载 MeshRenderer 视图: {prefabPath}");
                }
                else
                {
                    // 尝试 SpriteRenderer 作为备选
                    var spriteRenderer = instance.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        viewInstance.SpriteRendererComponent = spriteRenderer;
                        
                        EntityManager.AddComponentData(entity, new MeshRenderComponent
                        {
                            GameObjectEntity = entity
                        });

                        GameLogger.Log("ViewLoaderSystem", $"加载 SpriteRenderer 视图: {prefabPath}");
                    }
                    else
                    {
                        GameLogger.LogWarning("ViewLoaderSystem", 
                            $"预制体 {prefabPath} 缺少任何可识别的渲染组件 (SkeletonAnimation/MeshRenderer/SpriteRenderer)");
                    }
                }
            }

            // 添加视图实例组件
            EntityManager.AddComponentData(entity, viewInstance);

            // 添加视图状态组件（如果还没有）
            if (!EntityManager.HasComponent<ViewStateComponent>(entity))
            {
                EntityManager.AddComponentData(entity, new ViewStateComponent
                {
                    CurrentAnimationState = Components.AnimationState.Idle,
                    NeedsAnimationUpdate = true,
                    ColorTint = 1.0f
                });
            }
        }

        protected override void OnDestroy()
        {
            // 清理所有 GameObject 实例
            foreach (var entity in SystemAPI.QueryBuilder().WithAll<ViewInstanceComponent>().Build().ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                if (EntityManager.HasComponent<ViewInstanceComponent>(entity))
                {
                    var viewInstance = EntityManager.GetComponentData<ViewInstanceComponent>(entity);
                    if (viewInstance.GameObjectInstance != null)
                    {
                        Object.Destroy(viewInstance.GameObjectInstance);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 视图加载请求标记 - 用于标记需要在主线程加载的实体
    /// </summary>
    public struct ViewLoadRequestTag : IComponentData
    {
    }
}
