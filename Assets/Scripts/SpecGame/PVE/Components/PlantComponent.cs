using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PVZ
{
    /// <summary>
    /// 植物组件 - 定义植物的基本属�?
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
        Sunflower,      // 向日�?
        WallNut,        // 坚果�?
        CherryBomb,     // 樱桃炸弹
        SnowPea,        // 寒冰射手
        Repeater        // 双发射手
    }
}
