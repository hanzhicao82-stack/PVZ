using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Common;
using AnimationState = Common.AnimationState;

namespace PVZ
{
    /// <summary>
    /// 僵尸视图状态更新系�?- 更新僵尸的视图状态（动画、颜色等�?
    /// 不负责具体渲染，�?SpineViewSystem �?MeshRendererViewSystem 处理
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class ZombieViewSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            
            // 根据配置决定是否启用
            var config = ViewSystemConfig.Instance;
            if (!config.enableSpineSystem && !config.enableMeshRendererSystem)
            {
                Enabled = false;
                UnityEngine.Debug.Log("ZombieViewSystem is disabled (no view rendering enabled).");
            }
        }

        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var currentTime = (float)SystemAPI.Time.ElapsedTime;

            // 更新所有僵尸的视图状�?
            foreach (var (zombie, viewState, health) in
                SystemAPI.Query<RefRO<ZombieComponent>, RefRW<ViewStateComponent>, RefRO<HealthComponent>>())
            {
                UpdateZombieViewState(ref viewState.ValueRW, zombie.ValueRO, health.ValueRO, currentTime);
            }
        }

        /// <summary>
        /// 更新僵尸的视图状态（动画、颜色等�?
        /// </summary>
        private void UpdateZombieViewState(
            ref ViewStateComponent viewState,
            ZombieComponent zombie,
            HealthComponent health,
            float currentTime)
        {
            // 确定当前应该播放的动�?
            var targetAnimationState = AnimationState.Walk;

            if (health.IsDead)
            {
                targetAnimationState = AnimationState.Death;
            }
            else if (zombie.LastAttackTime > 0 && currentTime - zombie.LastAttackTime < 0.5f)
            {
                targetAnimationState = AnimationState.Attack;
            }

            // 检查是否需要切换动�?
            if (viewState.CurrentAnimationState != targetAnimationState)
            {
                viewState.CurrentAnimationState = targetAnimationState;
                viewState.NeedsAnimationUpdate = true;
            }

            // 根据血量更新颜色调制（受伤闪烁效果�?
            float healthPercent = health.CurrentHealth / health.MaxHealth;
            if (healthPercent < 0.3f)
            {
                // 低血量时闪烁
                viewState.ColorTint = 0.5f + 0.5f * Mathf.Sin(currentTime * 10f);
            }
            else
            {
                viewState.ColorTint = 1.0f;
            }
        }
    }
}
