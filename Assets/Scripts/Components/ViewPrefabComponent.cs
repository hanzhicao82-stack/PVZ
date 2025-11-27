using Unity.Entities;
using Unity.Collections;
using UnityEngine;

namespace PVZ.DOTS.Components
{
    /// <summary>
    /// 视图预制体配置组件 - 存储实体对应的视图预制体信息
    /// </summary>
    public struct ViewPrefabComponent : IComponentData
    {
        /// <summary>
        /// 预制体路径（Resources 路径）
        /// 例如："Prefabs/Zombies/NormalZombie"
        /// </summary>
        public FixedString128Bytes PrefabPath;

        /// <summary>
        /// 是否已加载视图
        /// </summary>
        public bool IsViewLoaded;
    }

    /// <summary>
    /// 视图实例组件 - 存储已加载的 GameObject 实例
    /// 这是一个 ICleanupComponentData，当实体销毁时会自动清理
    /// </summary>
    public class ViewInstanceComponent : ICleanupComponentData
    {
        /// <summary>
        /// 实例化的 GameObject
        /// </summary>
        public GameObject GameObjectInstance;

        /// <summary>
        /// Spine 组件引用（如果是 Spine 渲染）
        /// </summary>
        public object SpineSkeletonAnimation; // 类型为 Spine.Unity.SkeletonAnimation

        /// <summary>
        /// MeshRenderer 组件引用（如果是 MeshRenderer 渲染）
        /// </summary>
        public MeshRenderer MeshRendererComponent;

        /// <summary>
        /// SpriteRenderer 组件引用（如果是 Sprite 渲染）
        /// </summary>
        public SpriteRenderer SpriteRendererComponent;
    }
}
