using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Framework;
using PVZ;

namespace Common
{
    /// <summary>
    /// 视图加载系统 - 负责为实体加载和绑定视图模型
    /// �?InitializationSystemGroup 中更新，确保在游戏逻辑之前完成视图加载
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(BeginInitializationEntityCommandBufferSystem))]
    public partial class ViewLoaderSystem : SystemBase
    {
        private EndInitializationEntityCommandBufferSystem _ecbSystem;
        private Framework.IResourceService _resourceService;
        private Framework.IPoolService _poolService;
        private Framework.IModuleContext _context;

        /// <summary>
        /// 获取模块上下文单�?
        /// </summary>
        private Framework.IModuleContext GetContext()
        {
            if (_context == null)
            {
                // �?GameBootstrap 单例获取全局 Context
                var bootstrap = Framework.GameBootstrap.Instance;
                if (bootstrap != null)
                {
                    _context = bootstrap.Context;
                }
                else
                {
                    GameLogger.LogError("ViewLoaderSystem", "GameBootstrap instance not found! Cannot get module context.");
                }
            }
            return _context;
        }

        /// <summary>
        /// 获取资源服务（从单例 Context�?
        /// </summary>
        private IResourceService GetResourceService()
        {
            if (_resourceService == null)
            {
                var context = GetContext();
                if (context != null)
                {
                    _resourceService = context.GetService<IResourceService>();
                }
            }
            return _resourceService;
        }

        /// <summary>
        /// 获取对象池服务（从单�?Context�?
        /// </summary>
        private IPoolService GetPoolService()
        {
            if (_poolService == null)
            {
                var context = GetContext();
                if (context != null)
                {
                    _poolService = context.GetService<IPoolService>();
                }
            }
            return _poolService;
        }

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystemManaged<EndInitializationEntityCommandBufferSystem>();
            
            // 根据配置决定是否启用
            var config = ViewSystemConfig.Instance;
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

            // 收集需要加载的实体，避免在枚举时做结构性修�?
            var loadRequests = new NativeList<ViewLoadTask>(Allocator.Temp);
            foreach (var (viewPrefab, transform, entity) in
                SystemAPI.Query<RefRO<ViewPrefabComponent>, RefRO<LocalTransform>>()
                .WithAll<ViewLoadRequestTag>()
                .WithNone<ViewInstanceComponent>()
                .WithEntityAccess())
            {
                loadRequests.Add(new ViewLoadTask
                {
                    Entity = entity,
                    PrefabPath = viewPrefab.ValueRO.PrefabPath,
                    Transform = transform.ValueRO
                });
            }

            // 在枚举结束后执行结构性修�?
            foreach (var request in loadRequests)
            {
                LoadViewForEntity(request.Entity, request.PrefabPath, request.Transform);
                EntityManager.RemoveComponent<ViewLoadRequestTag>(request.Entity);
            }

            loadRequests.Dispose();

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
                        // 同步位置和旋�?
                        viewInstance.GameObjectInstance.transform.position = transform.ValueRO.Position;
                        viewInstance.GameObjectInstance.transform.rotation = transform.ValueRO.Rotation;
                        viewInstance.GameObjectInstance.transform.localScale = new Vector3(
                            transform.ValueRO.Scale, transform.ValueRO.Scale, transform.ValueRO.Scale);
                    }
                }
            }
        }

        /// <summary>
        /// 为实体加载视图模�?
        /// </summary>
        private struct ViewLoadTask
        {
            public Entity Entity;
            public FixedString128Bytes PrefabPath;
            public LocalTransform Transform;
        }

        private void LoadViewForEntity(Entity entity, FixedString128Bytes prefabPathFixed, in LocalTransform transform)
        {
            // �?Resources 加载预制体（运行时使用）
            string prefabPath = prefabPathFixed.ToString();
            GameObject prefab = null;

            // 优先使用资源服务（从单例 Context 获取�?
            var resourceService = GetResourceService();
            if (resourceService != null)
            {
                prefab = resourceService.Load<GameObject>(prefabPath);
            }
            else
            {
                // 降级方案：直接使�?Resources.Load
                prefab = Resources.Load<GameObject>(prefabPath);
            }

#if UNITY_EDITOR
            // 编辑器下如果未能�?Resources 加载，则尝试通过 AssetDatabase 加载（支�?Assets/Res/Spine 目录�?
            if (prefab == null)
            {
                string assetPath = prefabPath;
                if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    assetPath = $"Assets/{assetPath}";
                }

                if (!assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    assetPath += ".prefab";
                }

                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (prefab == null)
                {
                    GameLogger.LogWarning("ViewLoaderSystem", $"AssetDatabase 无法加载预制�? {assetPath}");
                }
            }
#endif

            if (prefab == null)
            {
                GameLogger.LogWarning("ViewLoaderSystem", $"无法加载预制�? {prefabPath}");
                return;
            }

            // 实例�?GameObject
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
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
            // 优先检�?Spine 组件
            var spineComponent = instance.GetComponentInChildren<Spine.Unity.SkeletonAnimation>();
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
                    // 尝试 SpriteRenderer 作为备�?
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
                            $"预制�?{prefabPath} 缺少任何可识别的渲染组件 (SkeletonAnimation/MeshRenderer/SpriteRenderer)");
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
                    CurrentAnimationState = AnimationState.Idle,
                    NeedsAnimationUpdate = true,
                    ColorTint = 1.0f,
                    LastAppliedColorTint = 1.0f
                });
            }
        }

        /// <summary>
        /// 清理所有视图实例（可由模块调用�?
        /// </summary>
        public void CleanupAllViews()
        {
            // 清理所�?GameObject 实例
            foreach (var entity in SystemAPI.QueryBuilder().WithAll<ViewInstanceComponent>().Build().ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                if (EntityManager.HasComponent<ViewInstanceComponent>(entity))
                {
                    var viewInstance = EntityManager.GetComponentData<ViewInstanceComponent>(entity);
                    if (viewInstance.GameObjectInstance != null)
                    {
                        UnityEngine.Object.Destroy(viewInstance.GameObjectInstance);
                    }
                }
            }
        }

        protected override void OnDestroy()
        {
            CleanupAllViews();
        }
    }

    /// <summary>
    /// 视图加载请求标记 - 用于标记需要在主线程加载的实体
    /// </summary>
    public struct ViewLoadRequestTag : IComponentData
    {
    }
}
