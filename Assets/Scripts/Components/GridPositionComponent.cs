using Unity.Entities;
using Unity.Mathematics;

namespace PVZ.DOTS.Components
{
    /// <summary>
    /// 网格位置组件 - 用于在游戏场地上定位实体
    /// </summary>
    public struct GridPositionComponent : IComponentData
    {
        public int Row;
        public int Column;
        public float3 WorldPosition;
    }
}
