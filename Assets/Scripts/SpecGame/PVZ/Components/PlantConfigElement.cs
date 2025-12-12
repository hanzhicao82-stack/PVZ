using Unity.Collections;
using Unity.Entities;

namespace PVZ
{
    public struct PlantConfigElement : IBufferElementData
    {
        public PlantType Type;
        public int SunCost;
        public float AttackDamage;
        public float AttackInterval;
        public float AttackRange;
        public float Health;
        public float SunProductionInterval;
        public int SunProductionAmount;
        public FixedString128Bytes ProjectilePrefabPath;
    }
}
