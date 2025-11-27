using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Systems
{
    /// <summary>
    /// 视图清理系统 - 负责清理被销毁实体的视图实例
    /// 当实体被销毁时，自动销毁对应的 GameObject
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
    public partial class ViewCleanupSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            // 清理那些实体已被销毁但 GameObject 还存在的视图
            // 注意：由于 ViewInstanceComponent 是 ICleanupComponentData，
            // 当实体销毁时，组件会保留，需要手动清理 GameObject
            var ecb = _ecbSystem.CreateCommandBuffer();
            
            foreach (var entity in SystemAPI.QueryBuilder()
                .WithAll<ViewInstanceComponent>()
                .WithNone<LocalTransform>()
                .Build()
                .ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                if (EntityManager.HasComponent<ViewInstanceComponent>(entity))
                {
                    var viewInstance = EntityManager.GetComponentData<ViewInstanceComponent>(entity);
                    if (viewInstance.GameObjectInstance != null)
                    {
                        Object.Destroy(viewInstance.GameObjectInstance);
                        viewInstance.GameObjectInstance = null;
                    }
                    // 移除清理组件
                    ecb.RemoveComponent<ViewInstanceComponent>(entity);
                }
            }
        }
    }
}
