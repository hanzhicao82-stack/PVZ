using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Systems
{
    /// <summary>
    /// Spine 视图系统 - 处理使用 Spine 动画的实体
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class SpineViewSystem : ViewSystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();

            // 根据配置决定是否启用
            if (!Config.ViewSystemConfig.Instance.enableSpineSystem)
            {
                Enabled = false;
            }
            else
            {
                UnityEngine.Debug.Log("SpineViewSystem is enabled.");
            }
        }

        protected override void UpdateViews()
        {
            // 更新所有使用 Spine 渲染的实体
            foreach (var (viewState, transform, entity) in
                SystemAPI.Query<RefRW<ViewStateComponent>, RefRO<LocalTransform>>()
                .WithAll<SpineRenderComponent, ViewInstanceComponent>()
                .WithEntityAccess())
            {
                if (!EntityManager.HasComponent<ViewInstanceComponent>(entity))
                    continue;

                var viewInstance = EntityManager.GetComponentData<ViewInstanceComponent>(entity);

                // 更新 Spine 动画
                if (viewInstance.SpineSkeletonAnimation != null && viewState.ValueRO.NeedsAnimationUpdate)
                {
                    UpdateSpineAnimation(viewInstance, ref viewState.ValueRW);
                }

                // 更新颜色
                UpdateSpineColor(viewInstance, viewState.ValueRO);
            }
        }

        /// <summary>
        /// 更新 Spine 动画
        /// </summary>
        private void UpdateSpineAnimation(ViewInstanceComponent viewInstance, ref ViewStateComponent viewState)
        {
            var skeleton = viewInstance.SpineSkeletonAnimation as Spine.Unity.SkeletonAnimation;
            if (skeleton != null)
            {
                string animationName = GetAnimationName(viewState.CurrentAnimationState);

                // 根据动画状态决定是否循环
                bool loop = viewState.CurrentAnimationState != Components.AnimationState.Death
                         && viewState.CurrentAnimationState != Components.AnimationState.Hurt;

                skeleton.AnimationState.SetAnimation(0, animationName, loop);
                viewState.NeedsAnimationUpdate = false;
            }
        }

        /// <summary>
        /// 更新 Spine 颜色
        /// </summary>
        private void UpdateSpineColor(ViewInstanceComponent viewInstance, ViewStateComponent viewState)
        {
            var skeleton = viewInstance.SpineSkeletonAnimation as Spine.Unity.SkeletonAnimation;
            if (skeleton != null && viewState.ColorTint < 1.0f)
            {
                // 设置 Spine 骨骼颜色
                var color = new Color(viewState.ColorTint, viewState.ColorTint, viewState.ColorTint, 1.0f);
                skeleton.skeleton.R = color.r;
                skeleton.skeleton.G = color.g;
                skeleton.skeleton.B = color.b;
            }
        }
    }
}
