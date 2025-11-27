using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Systems
{
    /// <summary>
    /// 视图系统基类 - 处理实体的可视化更新
    /// 提供通用的视图更新逻辑，子类实现具体的渲染方式
    /// </summary>
    public abstract partial class ViewSystemBase : SystemBase
    {
        protected float DeltaTime { get; private set; }

        protected override void OnUpdate()
        {
            DeltaTime = SystemAPI.Time.DeltaTime;
            
            // 子类实现具体的更新逻辑
            UpdateViews();
        }

        /// <summary>
        /// 子类实现具体的视图更新逻辑
        /// </summary>
        protected abstract void UpdateViews();

        /// <summary>
        /// 更新实体的视图状态
        /// </summary>
        protected void UpdateViewState<TComponent>(
            ref ViewStateComponent viewState,
            TComponent component,
            HealthComponent? health,
            float deltaTime,
            System.Func<TComponent, HealthComponent?, Components.AnimationState> getAnimationState)
            where TComponent : struct, IComponentData
        {
            // 确定目标动画状态
            var targetAnimationState = getAnimationState(component, health);

            // 检查是否需要切换动画
            if (viewState.CurrentAnimationState != targetAnimationState)
            {
                viewState.CurrentAnimationState = targetAnimationState;
                viewState.NeedsAnimationUpdate = true;
            }

            // 根据血量更新颜色调制
            if (health.HasValue)
            {
                float healthPercent = health.Value.CurrentHealth / health.Value.MaxHealth;
                if (healthPercent < 0.3f)
                {
                    // 低血量时闪烁
                    viewState.ColorTint = 0.5f + 0.5f * Mathf.Sin((float)SystemAPI.Time.ElapsedTime * 10f);
                }
                else
                {
                    viewState.ColorTint = 1.0f;
                }
            }
        }

        /// <summary>
        /// 获取动画名称
        /// </summary>
        protected virtual string GetAnimationName(Components.AnimationState state)
        {
            return state switch
            {
                Components.AnimationState.Idle => "idle",
                Components.AnimationState.Walk => "walk",
                Components.AnimationState.Attack => "attack",
                Components.AnimationState.Hurt => "hurt",
                Components.AnimationState.Death => "death",
                Components.AnimationState.Produce => "produce",
                _ => "idle"
            };
        }
    }
}
