using Unity.Entities;
using Unity.Mathematics;

namespace Game.TowerDefense
{
    /// <summary>
    /// 子弹组件 - 定义子弹的基本属�?
    /// </summary>
    public struct ProjectileComponent : IComponentData
    {
        public float Damage;
        public float Speed;
        public float3 Direction;
        public ProjectileType Type;
        public int Lane; // 所在行
    }

    /// <summary>
    /// 子弹类型枚举
    /// </summary>
    public enum ProjectileType
    {
        Pea,            // 豌豆
        FrozenPea,      // 冰冻豌豆
        Melon,          // 西瓜
        Spore           // 孢子
    }
}
