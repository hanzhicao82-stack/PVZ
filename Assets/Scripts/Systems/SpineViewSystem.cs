using Spine.Unity;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Systems
{
    /// <summary>
    /// Spine 视图系统 - 处理使用 Spine 动画的实体（性能优化版）
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class SpineViewSystem : ViewSystemBase
    {
        private Camera _mainCamera;
        private int _frameCounter = 0;
        private const int BASE_UPDATE_FREQUENCY = 1; // 基础更新频率
        private const float LOD_DISTANCE_NEAR = 15f; // 近距离阈值
        private const float LOD_DISTANCE_FAR = 30f;  // 远距离阈值

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

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                UnityEngine.Debug.LogWarning("[SpineViewSystem] Main camera not found!");
            }
        }

        protected override void UpdateViews()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            _frameCounter++;
            var cameraPos = _mainCamera.transform.position;

            // 更新所有使用 Spine 渲染的实体
            foreach (var (viewState, transform, entity) in
                SystemAPI.Query<RefRW<ViewStateComponent>, RefRO<LocalTransform>>()
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

                // 计算距离（用于 LOD）
                var entityPos = transform.ValueRO.Position;
                float distanceSqr = math.distancesq(new float3(cameraPos.x, cameraPos.y, cameraPos.z), entityPos);
                float distance = math.sqrt(distanceSqr);

                // 视锥剔除
                if (!IsInCameraView(entityPos))
                {
                    // 屏幕外的直接禁用
                    if (skeleton.enabled)
                    {
                        skeleton.enabled = false;
                    }
                    continue;
                }

                // 确保启用
                if (!skeleton.enabled)
                {
                    skeleton.enabled = true;
                }

                // LOD 距离分级更新
                int updateFrequency = CalculateUpdateFrequency(distance);
                if (_frameCounter % updateFrequency != 0)
                {
                    continue; // 跳过此帧更新
                }

                // 更新 Spine 动画
                if (viewStateRef.NeedsAnimationUpdate)
                {
                    UpdateSpineAnimation(skeleton, ref viewStateRef);
                }

                // 更新颜色（仅在近距离）
                if (distance < LOD_DISTANCE_NEAR)
                {
                    UpdateSpineColor(skeleton, ref viewStateRef);
                }
            }
        }

        /// <summary>
        /// 根据距离计算更新频率
        /// </summary>
        private int CalculateUpdateFrequency(float distance)
        {
            if (distance < LOD_DISTANCE_NEAR)
            {
                return 1; // 每帧更新
            }
            else if (distance < LOD_DISTANCE_FAR)
            {
                return 2; // 每2帧更新
            }
            else
            {
                return 4; // 每4帧更新
            }
        }

        /// <summary>
        /// 快速检查是否在相机视野内
        /// </summary>
        private bool IsInCameraView(float3 worldPos)
        {
            var viewportPos = _mainCamera.WorldToViewportPoint(new Vector3(worldPos.x, worldPos.y, worldPos.z));
            return viewportPos.x >= -0.1f && viewportPos.x <= 1.1f &&
                   viewportPos.y >= -0.1f && viewportPos.y <= 1.1f &&
                   viewportPos.z > 0;
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
