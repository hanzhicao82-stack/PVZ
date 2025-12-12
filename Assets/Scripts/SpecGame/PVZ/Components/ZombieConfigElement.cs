using Unity.Collections;
using Unity.Entities;

namespace PVZ
{
    public struct ZombieConfigElement : IBufferElementData
    {
        public ZombieType Type;
        public float MovementSpeed;
        public float AttackDamage;
        public float AttackInterval;
        public float Health;
        public FixedString128Bytes ProjectilePrefabPath;
    }
}
