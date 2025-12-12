using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using Common;
using AnimationState = Common.AnimationState;

namespace PVZ
{
    /// <summary>
    /// 妞嶇墿瑙嗗浘鐘舵€佹洿鏂扮郴锟?- 鏇存柊妞嶇墿鐨勮鍥剧姸鎬侊紙鍔ㄧ敾銆侀鑹茬瓑锛夛紙鎬ц兘浼樺寲鐗堬級
    /// 涓嶈礋璐ｅ叿浣撴覆鏌擄紝锟?SpineViewSystem 锟?MeshRendererViewSystem 澶勭悊
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class PlantViewSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            
            // 鏍规嵁閰嶇疆鍐冲畾鏄惁鍚敤
            var config = ViewSystemConfig.Instance;
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

            // Job 1: 鏇存柊鎵€鏈夋鐗╃殑瑙嗗浘鐘讹拷?
            var updatePlantViewJob = new UpdatePlantViewStateJob
            {
                // 鍙互鍦ㄨ繖閲屼紶閫掗渶瑕佺殑鏁版嵁
            };
            updatePlantViewJob.ScheduleParallel();

            // Job 2: 澶勭悊鏈夎閲忕殑妞嶇墿锛堝鍧氭灉澧欙級
            var updateHealthVisualsJob = new UpdatePlantHealthVisualsJob();
            updateHealthVisualsJob.ScheduleParallel();
        }

        /// <summary>
        /// 鏇存柊妞嶇墿瑙嗗浘鐘舵€佺殑 Job锛堝彲骞惰鎵ц锟?
        /// </summary>
        [BurstCompile]
        partial struct UpdatePlantViewStateJob : IJobEntity
        {
            void Execute(
                ref ViewStateComponent viewState,
                in PlantComponent plant,
                [EntityIndexInQuery] int entityIndex)
            {
                // 纭畾褰撳墠搴旇鎾斁鐨勫姩锟?
                var targetAnimationState = AnimationState.Idle;

                // 鍚戞棩钁电敓浜ч槼鍏夌殑鐗规畩鍔ㄧ敾
                if (plant.Type == PlantType.Sunflower)
                {
                    targetAnimationState = AnimationState.Produce;
                }

                // 妫€鏌ユ槸鍚﹂渶瑕佸垏鎹㈠姩锟?
                if (viewState.CurrentAnimationState != targetAnimationState)
                {
                    viewState.CurrentAnimationState = targetAnimationState;
                    viewState.NeedsAnimationUpdate = true;
                }
            }
        }

        /// <summary>
        /// 鏇存柊妞嶇墿琛€閲忚瑙夋晥鏋滅殑 Job
        /// </summary>
        [BurstCompile]
        partial struct UpdatePlantHealthVisualsJob : IJobEntity
        {
            void Execute(
                ref ViewStateComponent viewState,
                in HealthComponent health,
                in PlantComponent plant)
            {
                // 鏍规嵁琛€閲忔洿鏂拌瑙夌姸锟?
                float healthPercent = health.CurrentHealth / health.MaxHealth;
                if (healthPercent < 0.3f)
                {
                    // 浣庤閲忔椂鐨勮瑙夋晥锟?
                    viewState.ColorTint = 0.7f + 0.3f * healthPercent;
                }
                else if (healthPercent < 0.6f)
                {
                    // 涓瓑琛€锟?
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
