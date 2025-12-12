using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Common
{
    /// <summary>
    /// 瑙嗗浘绯荤粺鍩虹被 - 澶勭悊瀹炰綋鐨勫彲瑙嗗寲鏇存柊
    /// 鎻愪緵閫氱敤鐨勮鍥炬洿鏂伴€昏緫锛屽瓙绫诲疄鐜板叿浣撶殑娓叉煋鏂瑰紡
    /// </summary>
    public abstract partial class ViewSystemBase : SystemBase
    {
        protected float DeltaTime { get; private set; }

        protected override void OnUpdate()
        {
            DeltaTime = SystemAPI.Time.DeltaTime;

            // 瀛愮被瀹炵幇鍏蜂綋鐨勬洿鏂伴€昏緫
            UpdateViews();
        }

        /// <summary>
        /// 瀛愮被瀹炵幇鍏蜂綋鐨勮鍥炬洿鏂伴€昏緫
        /// </summary>
        protected abstract void UpdateViews();

        /// <summary>
        /// 鏇存柊瀹炰綋鐨勮鍥剧姸锟?
        /// </summary>
        protected void UpdateViewState<TComponent>(
            ref ViewStateComponent viewState,
            TComponent component,
            HealthComponent? health,
            float deltaTime,
            System.Func<TComponent, HealthComponent?, AnimationState> getAnimationState)
            where TComponent : struct, IComponentData
        {
            // 纭畾鐩爣鍔ㄧ敾鐘讹拷?
            var targetAnimationState = getAnimationState(component, health);

            // 妫€鏌ユ槸鍚﹂渶瑕佸垏鎹㈠姩锟?
            if (viewState.CurrentAnimationState != targetAnimationState)
            {
                viewState.CurrentAnimationState = targetAnimationState;
                viewState.NeedsAnimationUpdate = true;
            }

            // 鏍规嵁琛€閲忔洿鏂伴鑹茶皟锟?
            if (health.HasValue)
            {
                float healthPercent = health.Value.CurrentHealth / health.Value.MaxHealth;
                if (healthPercent < 0.3f)
                {
                    // 浣庤閲忔椂闂儊
                    viewState.ColorTint = 0.5f + 0.5f * Mathf.Sin((float)SystemAPI.Time.ElapsedTime * 10f);
                }
                else
                {
                    viewState.ColorTint = 1.0f;
                }
            }
        }

        /// <summary>
        /// 鑾峰彇鍔ㄧ敾鍚嶇О
        /// </summary>
        protected virtual string GetAnimationName(AnimationState state)
        {
            return state switch
            {
                AnimationState.Idle => "idle",
                AnimationState.Walk => "move",
                AnimationState.Attack => "atk",
                AnimationState.Hurt => "hurt",
                AnimationState.Death => "die",
                AnimationState.Produce => "produce",
                _ => "idle"
            };
        }
    }
}
