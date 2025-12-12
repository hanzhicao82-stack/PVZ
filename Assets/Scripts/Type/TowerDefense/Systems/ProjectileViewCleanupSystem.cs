using Unity.Collections;
using Unity.Entities;
using Common;
using Framework;
using Game.TowerDefense;

namespace Game.TowerDefense
{
    /// <summary>
    /// 子弹视图清理系统 - 在实体销毁时回收子弹 GameObject 到对象池�?
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
    public partial class ProjectileViewCleanupSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var entities = SystemAPI.QueryBuilder()
                .WithAll<ProjectileViewComponent>()
                .WithNone<ProjectileComponent>()
                .Build()
                .ToEntityArray(Allocator.Temp);

            foreach (var entity in entities)
            {
                if (!SystemAPI.ManagedAPI.HasComponent<ProjectileViewComponent>(entity))
                    continue;

                var view = SystemAPI.ManagedAPI.GetComponent<ProjectileViewComponent>(entity);
                if (view != null && view.Instance != null)
                {
                    ProjectileViewPool.Release(view.PrefabPath, view.Instance);
                    view.Instance = null;
                }

                EntityManager.RemoveComponent<ProjectileViewComponent>(entity);
                if (EntityManager.HasComponent<ProjectileViewPrefabComponent>(entity))
                {
                    EntityManager.RemoveComponent<ProjectileViewPrefabComponent>(entity);
                }
            }
        }
    }
}
