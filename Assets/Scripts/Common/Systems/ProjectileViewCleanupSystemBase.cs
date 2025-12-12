using Unity.Collections;
using Unity.Entities;

namespace Common
{
    /// <summary>
    /// 子弹视图清理系统抽象基类 - 提供通用的视图清理逻辑
    /// 子类需要实现具体的视图释放逻辑和实体查询条件
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
    public abstract partial class ProjectileViewCleanupSystemBase : SystemBase
    {
        protected override void OnUpdate()
        {
            // 查找需要清理的视图实体，由子类实现具体查询逻辑
            var entitiesToCleanup = GetEntitiesToCleanup();

            // 清理视图
            CleanupViews(entitiesToCleanup);

            entitiesToCleanup.Dispose();
        }

        /// <summary>
        /// 获取需要清理的实体列表，由子类实现具体的查询逻辑
        /// </summary>
        protected abstract NativeArray<Entity> GetEntitiesToCleanup();

        /// <summary>
        /// 清理视图，由子类实现具体的释放逻辑
        /// </summary>
        protected abstract void CleanupViews(NativeArray<Entity> entities);

        /// <summary>
        /// 释放单个视图实例到对象池，由子类实现
        /// </summary>
        protected abstract void ReleaseViewInstance(string prefabPath, UnityEngine.GameObject instance);
    }
}
