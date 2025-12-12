using Unity.Entities;

namespace Common
{
    public struct GameConfigComponent : IComponentData
    {
        public float ZombieSpawnInterval;
        public float ZombieSpawnStartDelay;
        public int LaneCount;
        public float SpawnX;
        public float LaneZSpacing;
        public float LaneZOffset;
        public float ZombieMovementSpeed;
        public float ZombieAttackDamage;
        public float ZombieAttackInterval;
        public float ZombieHealth;
    }
}
