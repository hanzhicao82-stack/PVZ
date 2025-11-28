using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Systems
{
    /// <summary>
    /// 植物视图状态更新系统 - 更新植物的视图状态（动画、颜色等）
    /// 不负责具体渲染，由 SpineViewSystem 和 MeshRendererViewSystem 处理
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class PlantViewSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            
            // 根据配置决定是否启用
            var config = Config.ViewSystemConfig.Instance;
            if (!config.enableSpineSystem && !config.enableMeshRendererSystem)
            {
                Enabled = false;
                UnityEngine.Debug.Log("PlantViewSystem is disabled (no view rendering enabled).");
            }
        }

        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var currentTime = (float)SystemAPI.Time.ElapsedTime;

            // 更新所有植物的视图状态
            // 根据 AttackStateComponent 的存在来决定是否显示攻击动画
            foreach (var (plant, viewState, entity) in
                SystemAPI.Query<RefRO<PlantComponent>, RefRW<ViewStateComponent>>()
                .WithEntityAccess())
            {
                // 检查是否有攻击状态组件
                bool isAttacking = SystemAPI.HasComponent<AttackStateComponent>(entity);
                
                UpdatePlantViewState(ref viewState.ValueRW, plant.ValueRO, isAttacking);
            }

            // 处理有血量的植物（如坚果墙）
            foreach (var (viewState, health) in
                SystemAPI.Query<RefRW<ViewStateComponent>, RefRO<HealthComponent>>()
                .WithAll<PlantComponent>())
            {
                UpdatePlantHealthVisuals(ref viewState.ValueRW, health.ValueRO);
            }
        }

        /// <summary>
        /// 更新植物的视图状态（动画、特效等）
        /// </summary>
        private void UpdatePlantViewState(
            ref ViewStateComponent viewState,
            PlantComponent plant,
            bool isAttacking)
        {
            // 确定当前应该播放的动画
            var targetAnimationState = Components.AnimationState.Idle;

            // 根据攻击状态组件决定动画
            if (isAttacking)
            {
                targetAnimationState = Components.AnimationState.Attack;
            }
            // 向日葵生产阳光的特殊动画
            else if (plant.Type == PlantType.Sunflower)
            {
                // 根据生产周期决定动画
                targetAnimationState = Components.AnimationState.Produce;
            }

            // 检查是否需要切换动画
            if (viewState.CurrentAnimationState != targetAnimationState)
            {
                viewState.CurrentAnimationState = targetAnimationState;
                viewState.NeedsAnimationUpdate = true;
            }
        }

        /// <summary>
        /// 更新有血量植物的视觉效果（如坚果墙）
        /// </summary>
        private void UpdatePlantHealthVisuals(
            ref ViewStateComponent viewState,
            HealthComponent health)
        {
            // 根据血量更新视觉状态
            float healthPercent = health.CurrentHealth / health.MaxHealth;
            if (healthPercent < 0.3f)
            {
                // 低血量时的视觉效果
                viewState.ColorTint = 0.7f + 0.3f * healthPercent;
            }
            else if (healthPercent < 0.6f)
            {
                // 中等血量
                viewState.ColorTint = 0.85f;
            }
            else
            {
                viewState.ColorTint = 1.0f;
            }
        }
    }
}
