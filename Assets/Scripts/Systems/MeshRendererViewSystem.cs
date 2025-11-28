using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Systems
{
    /// <summary>
    /// MeshRenderer 视图系统 - 处理使用 MeshRenderer/SpriteRenderer 的实体
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class MeshRendererViewSystem : ViewSystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();

            var config = Config.ViewSystemConfig.Instance;
            if (!config.enableMeshRendererSystem)
            {
                Enabled = false;
                UnityEngine.Debug.Log("MeshRendererViewSystem is disabled (MeshRenderer rendering is not enabled).");
            }
        }

        protected override void UpdateViews()
        {
            // 更新所有使用 MeshRenderer 渲染的实体
            foreach (var (viewState, transform, entity) in
                SystemAPI.Query<RefRW<ViewStateComponent>, RefRO<LocalTransform>>()
                .WithAll<MeshRenderComponent, ViewInstanceComponent>()
                .WithEntityAccess())
            {
                if (!EntityManager.HasComponent<ViewInstanceComponent>(entity))
                    continue;

                var viewInstance = EntityManager.GetComponentData<ViewInstanceComponent>(entity);

                // 更新颜色
                UpdateMeshColor(viewInstance, viewState.ValueRO);

                // 如果需要，可以通过纹理偏移实现帧动画
                if (viewState.ValueRO.NeedsAnimationUpdate)
                {
                    UpdateMeshAnimation(viewInstance, ref viewState.ValueRW);
                }
            }
        }

        /// <summary>
        /// 更新 MeshRenderer/SpriteRenderer 颜色
        /// </summary>
        private void UpdateMeshColor(ViewInstanceComponent viewInstance, ViewStateComponent viewState)
        {
            Color color = new Color(viewState.ColorTint, viewState.ColorTint, viewState.ColorTint);

            if (viewInstance.MeshRendererComponent != null)
            {
                var material = viewInstance.MeshRendererComponent.material;
                if (material != null)
                {
                    material.color = color;
                }
            }
            else if (viewInstance.SpriteRendererComponent != null)
            {
                viewInstance.SpriteRendererComponent.color = color;
            }
        }

        /// <summary>
        /// 更新 MeshRenderer 动画（通过纹理偏移或材质切换）
        /// </summary>
        private void UpdateMeshAnimation(ViewInstanceComponent viewInstance, ref ViewStateComponent viewState)
        {
            // TODO: 实现基于纹理偏移的帧动画
            // 例如：通过修改材质的 mainTextureOffset 实现 sprite sheet 动画

            if (viewInstance.MeshRendererComponent != null)
            {
                var material = viewInstance.MeshRendererComponent.material;
                if (material != null)
                {
                    // 根据动画状态设置纹理偏移
                    // material.mainTextureOffset = GetTextureOffsetForAnimation(viewState.CurrentAnimationState);
                }
            }

            viewState.NeedsAnimationUpdate = false;
        }
    }
}
