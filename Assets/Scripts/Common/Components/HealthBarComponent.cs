using Unity.Entities;
using UnityEngine;

namespace Common
{
    /// <summary>
    /// 血�?UI 组件 - 关联实体与其血�?UI GameObject
    /// </summary>
    public struct HealthBarComponent : IComponentData
    {
        /// <summary>
        /// 血�?UI GameObject 的实�?ID
        /// </summary>
        public int HealthBarInstanceID;
        
        /// <summary>
        /// 血�?UI 是否已创�?
        /// </summary>
        public bool IsCreated;
        
        /// <summary>
        /// 血条在屏幕上的 Y 轴偏移（世界空间�?
        /// </summary>
        public float YOffset;
    }
    
    /// <summary>
    /// 血�?UI 配置组件 - 控制血条的显示样式
    /// </summary>
    public struct HealthBarConfigComponent : IComponentData
    {
        /// <summary>
        /// 血条宽度（像素�?
        /// </summary>
        public float Width;
        
        /// <summary>
        /// 血条高度（像素�?
        /// </summary>
        public float Height;
        
        /// <summary>
        /// 是否始终显示血�?
        /// </summary>
        public bool AlwaysShow;
        
        /// <summary>
        /// 血条是否跟随世界坐�?
        /// </summary>
        public bool WorldSpace;
        
        /// <summary>
        /// 低血量阈值（百分比）
        /// </summary>
        public float LowHealthThreshold;
    }
}
