using Unity.Entities;

namespace PVZ.DOTS.Components
{
    /// <summary>
    /// 阳光生产者组件 - 用于向日葵等生产阳光的植物
    /// </summary>
    public struct SunProducerComponent : IComponentData
    {
        public float ProductionInterval;
        public float LastProductionTime;
        public int SunAmount;
    }
}
