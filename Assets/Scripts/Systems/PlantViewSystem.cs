using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Systems
{
    /// <summary>
    /// 植物视图状态更新系统 - 更新植物的视图状态（动画、颜色等）（性能优化版）
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

            // Job 1: 更新所有植物的视图状态
            var updatePlantViewJob = new UpdatePlantViewStateJob
            {
                // 可以在这里传递需要的数据
            };
            updatePlantViewJob.ScheduleParallel();

            // Job 2: 处理有血量的植物（如坚果墙）
            var updateHealthVisualsJob = new UpdatePlantHealthVisualsJob();
            updateHealthVisualsJob.ScheduleParallel();
        }

        /// <summary>
        /// 更新植物视图状态的 Job（可并行执行）
        /// </summary>
        [BurstCompile]
        partial struct UpdatePlantViewStateJob : IJobEntity
        {
            void Execute(
                ref ViewStateComponent viewState,
                in PlantComponent plant,
                [EntityIndexInQuery] int entityIndex)
            {
                // 确定当前应该播放的动画
                var targetAnimationState = Components.AnimationState.Idle;

                // 向日葵生产阳光的特殊动画
                if (plant.Type == PlantType.Sunflower)
                {
                    targetAnimationState = Components.AnimationState.Produce;
                }

                // 检查是否需要切换动画
                if (viewState.CurrentAnimationState != targetAnimationState)
                {
                    viewState.CurrentAnimationState = targetAnimationState;
                    viewState.NeedsAnimationUpdate = true;
                }
            }
        }

        /// <summary>
        /// 更新植物血量视觉效果的 Job
        /// </summary>
        [BurstCompile]
        partial struct UpdatePlantHealthVisualsJob : IJobEntity
        {
            void Execute(
                ref ViewStateComponent viewState,
                in HealthComponent health,
                in PlantComponent plant)
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
}
