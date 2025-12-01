using Spine.Unity;
using Unity.Entities;
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

            var config = Config.ViewSystemConfig.Instance;
            if (!config.enableSpineSystem)
            {
                Enabled = false;
                UnityEngine.Debug.Log("SpineViewSystem is disabled (Spine rendering is not enabled).");
            }
        }

        protected override void UpdateViews()
        {
            // 更新所有使用 Spine 渲染的实体
            foreach (var (viewState, entity) in
                SystemAPI.Query<RefRW<ViewStateComponent>>()
                .WithAll<SpineRenderComponent, ViewInstanceComponent>()
                .WithEntityAccess())
            {
                if (!SystemAPI.ManagedAPI.HasComponent<ViewInstanceComponent>(entity))
                    continue;

                var viewInstance = SystemAPI.ManagedAPI.GetComponent<ViewInstanceComponent>(entity);
                ref var viewStateRef = ref viewState.ValueRW;
                var skeleton = viewInstance.SpineSkeletonAnimation;

                if (skeleton == null)
                    continue;

                // 更新 Spine 动画
                if (viewStateRef.NeedsAnimationUpdate)
                {
                    UpdateSpineAnimation(skeleton, ref viewStateRef);
                }

                // 更新颜色
                UpdateSpineColor(skeleton, ref viewStateRef);
            }
        }

        /// <summary>
        /// 更新 Spine 动画
        /// </summary>
        private void UpdateSpineAnimation(SkeletonAnimation skeleton, ref ViewStateComponent viewState)
        {
            string animationName = GetAnimationName(viewState.CurrentAnimationState);

            // 根据动画状态决定是否循环
            bool loop = viewState.CurrentAnimationState != Components.AnimationState.Death
                     && viewState.CurrentAnimationState != Components.AnimationState.Hurt;

            var current = skeleton.AnimationState.GetCurrent(0);
            if (current != null && current.Animation != null)
            {
                if (current.Animation.Name == animationName && current.Loop == loop)
                {
                    viewState.NeedsAnimationUpdate = false;
                    return;
                }
            }

            skeleton.AnimationState.SetAnimation(0, animationName, loop);
            viewState.NeedsAnimationUpdate = false;
        }

        /// <summary>
        /// 更新 Spine 颜色
        /// </summary>
        private void UpdateSpineColor(SkeletonAnimation skeleton, ref ViewStateComponent viewState)
        {
            float targetTint = Mathf.Clamp01(viewState.ColorTint);

            if (Mathf.Approximately(viewState.LastAppliedColorTint, targetTint))
                return;

            var skeletonData = skeleton.skeleton;
            skeletonData.R = targetTint;
            skeletonData.G = targetTint;
            skeletonData.B = targetTint;
            skeletonData.A = 1.0f;

            viewState.LastAppliedColorTint = targetTint;
        }
    }
}
