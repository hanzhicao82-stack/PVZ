using Unity.Entities;
using UnityEngine;

namespace Common
{
    /// <summary>
    /// Spine 渲染组件 - 标记使用 Spine 动画的实�?
    /// </summary>
    public struct SpineRenderComponent : IComponentData
    {
        /// <summary>
        /// Spine 动画的引用（通过 GameObject 管理�?
        /// </summary>
        public Entity GameObjectEntity; // 关联�?GameObject Entity
    }

    /// <summary>
    /// MeshRenderer 渲染组件 - 标记使用 Unity MeshRenderer 的实�?
    /// </summary>
    public struct MeshRenderComponent : IComponentData
    {
        /// <summary>
        /// MeshRenderer 的引用（通过 GameObject 管理�?
        /// </summary>
        public Entity GameObjectEntity; // 关联�?GameObject Entity
    }

    /// <summary>
    /// 视图状态组�?- 存储当前的视觉状�?
    /// </summary>
    public struct ViewStateComponent : IComponentData
    {
        /// <summary>
        /// 当前动画状态（Idle, Walk, Attack, Death 等）
        /// </summary>
        public AnimationState CurrentAnimationState;

        /// <summary>
        /// 是否需要更新动�?
        /// </summary>
        public bool NeedsAnimationUpdate;

        /// <summary>
        /// 当前颜色调制（用于受伤闪烁等效果�?
        /// </summary>
        public float ColorTint; // 0-1�?为正常，0为完全变�?

        /// <summary>
        /// 上一次应用到渲染组件的颜色调�?
        /// </summary>
        public float LastAppliedColorTint;
    }

    /// <summary>
    /// 动画状态枚�?
    /// </summary>
    public enum AnimationState
    {
        Idle,       // 待机
        Walk,       // 行走
        Attack,     // 攻击
        Hurt,       // 受伤
        Death,      // 死亡
        Produce     // 生产（向日葵等）
    }
}
