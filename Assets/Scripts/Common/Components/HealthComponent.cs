using Unity.Entities;

namespace Common
{
    /// <summary>
    /// 健康值组�?- 用于所有具有生命值的实体
    /// </summary>
    public struct HealthComponent : IComponentData
    {
        public float CurrentHealth;
        public float MaxHealth;
        public bool IsDead;
    }
}
