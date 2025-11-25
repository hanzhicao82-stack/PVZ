using Unity.Entities;
using Unity.Mathematics;

namespace PVZ.DOTS.Components
{
    /// <summary>
    /// 僵尸组件 - 定义僵尸的基本属性
    /// </summary>
    public struct ZombieComponent : IComponentData
    {
        public ZombieType Type;
        public float MovementSpeed;
        public float AttackDamage;
        public float AttackInterval;
        public float LastAttackTime;
        public int Lane; // 所在行
    }

    /// <summary>
    /// 僵尸类型枚举
    /// </summary>
    public enum ZombieType
    {
        Normal,         // 普通僵尸
        ConeHead,       // 路障僵尸
        BucketHead,     // 铁桶僵尸
        Flag,           // 旗帜僵尸
        Newspaper       // 读报僵尸
    }
}
