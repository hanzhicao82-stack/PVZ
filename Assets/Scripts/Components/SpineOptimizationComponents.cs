using Unity.Entities;
using Unity.Mathematics;

namespace PVZ.DOTS.Components
{
    /// <summary>
    /// 视锥体剔除组件 - 标记实体是否在相机视野内
    /// </summary>
    public struct ViewCullingComponent : IComponentData
    {
        public bool IsVisible;
        public float CullingRadius;
        public float LastCheckTime;
    }

    /// <summary>
    /// LOD 层级细节组件
    /// </summary>
    public struct LODComponent : IComponentData
    {
        public int CurrentLODLevel;
        public float3 LODDistances; // x=LOD0->1, y=LOD1->2, z=LOD2->3
        public float DistanceSquaredToCamera;
    }

    /// <summary>
    /// Spine 优化配置组件
    /// </summary>
    public struct SpineOptimizationComponent : IComponentData
    {
        public bool EnableAnimationUpdate;
        public int AnimationUpdateInterval;
        public int FrameCounter;
        public bool EnableMeshUpdate;
    }
}
