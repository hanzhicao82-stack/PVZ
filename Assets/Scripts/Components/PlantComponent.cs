using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PVZ.DOTS.Components
{
    /// <summary>
    /// 植物组件 - 定义植物的基本属性
    /// </summary>
    public struct PlantComponent : IComponentData
    {
        public PlantType Type;
        public float AttackDamage;
        public float AttackInterval;
        public float LastAttackTime;
        public float AttackRange;
        public int SunCost;
        public FixedString128Bytes ProjectilePrefabPath;
    }

    /// <summary>
    /// 植物类型枚举
    /// </summary>
    public enum PlantType
    {
        Peashooter,     // 豌豆射手
        Sunflower,      // 向日葵
        WallNut,        // 坚果墙
        CherryBomb,     // 樱桃炸弹
        SnowPea,        // 寒冰射手
        Repeater        // 双发射手
    }
}
