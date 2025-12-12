using Unity.Entities;

namespace PVZ
{
    /// <summary>
    /// 阳光生产者组�?- 用于向日葵等生产阳光的植�?
    /// </summary>
    public struct SunProducerComponent : IComponentData
    {
        public float ProductionInterval;
        public float LastProductionTime;
        public int SunAmount;
    }
}
