using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Common;
using Framework;

namespace Game.TowerDefense
{
    /// <summary>
    /// 子弹视图系统 - 管理子弹 GameObject 的创建与同步�?
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class ProjectileViewSystem : SystemBase
    {
        private struct ViewRequest
        {
            public Entity Entity;
            public FixedString128Bytes PrefabPath;
            public LocalTransform Transform;
        }

        protected override void OnUpdate()
        {
            var requests = new NativeList<ViewRequest>(Allocator.Temp);

            foreach (var (prefab, transform, entity) in
                     SystemAPI.Query<RefRO<ProjectileViewPrefabComponent>, RefRO<LocalTransform>>()
                         .WithNone<ProjectileViewComponent>()
                         .WithEntityAccess())
            {
                if (prefab.ValueRO.PrefabPath.IsEmpty)
                    continue;

                requests.Add(new ViewRequest
                {
                    Entity = entity,
                    PrefabPath = prefab.ValueRO.PrefabPath,
                    Transform = transform.ValueRO
                });
            }

            foreach (var request in requests)
            {
                string path = request.PrefabPath.ToString();
                var instance = ProjectileViewPool.Acquire(path);
                if (instance == null)
                    continue;

                var instanceTransform = instance.transform;
                instanceTransform.position = request.Transform.Position;
                instanceTransform.rotation = request.Transform.Rotation;
                instanceTransform.localScale = new Vector3(
                    request.Transform.Scale,
                    request.Transform.Scale,
                    request.Transform.Scale);

                var viewComponent = new ProjectileViewComponent
                {
                    Instance = instance,
                    PrefabPath = path
                };

                EntityManager.AddComponentData(request.Entity, viewComponent);
            }

            requests.Dispose();

            foreach (var (transform, entity) in
                     SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<ProjectileViewComponent>()
                         .WithEntityAccess())
            {
                var view = SystemAPI.ManagedAPI.GetComponent<ProjectileViewComponent>(entity);
                if (view?.Instance == null)
                    continue;

                var instanceTransform = view.Instance.transform;
                instanceTransform.position = transform.ValueRO.Position;
                instanceTransform.rotation = transform.ValueRO.Rotation;
                instanceTransform.localScale = new Vector3(
                    transform.ValueRO.Scale,
                    transform.ValueRO.Scale,
                    transform.ValueRO.Scale);

                if (!view.Instance.activeSelf)
                {
                    view.Instance.SetActive(true);
                }
            }
        }
    }
}
