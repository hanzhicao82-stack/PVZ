using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Common
{
    /// <summary>
    /// 子弹视图系统抽象基类 - 提供通用的子弹视图创建和同步逻辑
    /// 子类需要实现具体的视图实例化和池化逻辑
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public abstract partial class ProjectileViewSystemBase : SystemBase
    {
        protected override void OnUpdate()
        {
            // 创建新的视图
            CreateViews();

            // 更新现有视图的位置
            UpdateViewPositions();
        }

        /// <summary>
        /// 创建新的子弹视图，由子类实现
        /// </summary>
        protected abstract void CreateViews();

        /// <summary>
        /// 更新视图位置，可由子类重写
        /// </summary>
        protected virtual void UpdateViewPositions()
        {
            // 默认实现：同步所有子弹的位置到视图
            foreach (var (transform, view) in
                SystemAPI.Query<RefRO<LocalTransform>, ProjectileViewComponent>())
            {
                if (view.Instance != null)
                {
                    view.Instance.transform.position = transform.ValueRO.Position;
                    view.Instance.transform.rotation = transform.ValueRO.Rotation;
                }
            }
        }

        /// <summary>
        /// 从对象池获取视图实例，由子类实现
        /// </summary>
        protected abstract GameObject AcquireViewInstance(string prefabPath);

        /// <summary>
        /// 释放视图实例到对象池，由子类实现
        /// </summary>
        protected abstract void ReleaseViewInstance(string prefabPath, GameObject instance);
    }

    /// <summary>
    /// 子弹视图组件 - 存储子弹的视图实例引用
    /// 这是一个通用的视图组件类
    /// </summary>
    public class ProjectileViewComponent : IComponentData
    {
        public GameObject Instance;
        public string PrefabPath;
    }
}
