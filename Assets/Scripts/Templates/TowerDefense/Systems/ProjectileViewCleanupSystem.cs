using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Common;
using Framework;

namespace Game.TowerDefense
{
    /// <summary>
    /// 子弹视图清理系统 - 在实体销毁时回收子弹 GameObject 到对象池，继承自通用 ProjectileViewCleanupSystemBase
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
    public partial class ProjectileViewCleanupSystem : ProjectileViewCleanupSystemBase
    {
        protected override NativeArray<Entity> GetEntitiesToCleanup()
        {
            return SystemAPI.QueryBuilder()
                .WithAll<ProjectileViewComponent>()
                .WithNone<ProjectileComponent>()
                .Build()
                .ToEntityArray(Allocator.Temp);
        }

        protected override void CleanupViews(NativeArray<Entity> entities)
        {
            foreach (var entity in entities)
            {
                if (!SystemAPI.ManagedAPI.HasComponent<ProjectileViewComponent>(entity))
                    continue;

                var view = SystemAPI.ManagedAPI.GetComponent<ProjectileViewComponent>(entity);
                if (view != null && view.Instance != null)
                {
                    ReleaseViewInstance(view.PrefabPath, view.Instance);
                    view.Instance = null;
                }

                EntityManager.RemoveComponent<ProjectileViewComponent>(entity);
                if (EntityManager.HasComponent<ProjectileViewPrefabComponent>(entity))
                {
                    EntityManager.RemoveComponent<ProjectileViewPrefabComponent>(entity);
                }
            }
        }

        protected override void ReleaseViewInstance(string prefabPath, GameObject instance)
        {
            ProjectileViewPool.Release(prefabPath, instance);
        }
    }
}
