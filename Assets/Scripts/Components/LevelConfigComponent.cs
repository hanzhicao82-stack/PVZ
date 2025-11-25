using Unity.Entities;
using Unity.Mathematics;

namespace PVZ.DOTS.Components
{
    public enum LevelType
    {
        Day,            // 白天关卡
        Night,          // 夜晚关卡
        Pool,           // 泳池关卡
        Fog,            // 迷雾关卡
        Roof,           // 屋顶关卡
        BossFight       // Boss战
    }

    public enum DifficultyLevel
    {
        Easy,
        Normal,
        Hard,
        Expert
    }

    /// <summary>
    /// 关卡配置组件 - 存储单个关卡的配置信息
    /// </summary>
    public struct LevelConfigComponent : IComponentData
    {
        public int LevelId;                     // 关卡ID
        public LevelType Type;                  // 关卡类型
        public DifficultyLevel Difficulty;      // 难度等级
        
        // 地图配置
        public int RowCount;                    // 行数（通常5行，泳池6行）
        public int ColumnCount;                 // 列数（通常9列）
        public float CellWidth;                 // 格子宽度
        public float CellHeight;                // 格子高度
        
        // 游戏规则
        public float GameDuration;              // 关卡时长（秒）
        public int TotalWaves;                  // 总波次
        public int MaxZombiesReached;           // 最多允许到达的僵尸数
        public int StartingSun;                 // 初始阳光
        
        // 僵尸生成
        public float ZombieSpawnInterval;       // 生成间隔
        public float ZombieSpawnStartDelay;     // 开始延迟
        public float WaveIntensityMultiplier;   // 波次强度倍数（每波僵尸数量增加）
        
        // 特殊规则
        public bool HasFog;                     // 是否有迷雾
        public bool HasPool;                    // 是否有泳池
        public bool HasRoof;                    // 是否是屋顶（抛物线攻击）
        public bool IsNightLevel;               // 是否是夜晚（向日葵不能用）
        public bool HasGrave;                   // 是否有墓碑
        public bool IsBossLevel;                // 是否是Boss关卡
    }

    /// <summary>
    /// 关卡波次配置 - 每波的僵尸组成
    /// </summary>
    public struct WaveConfigElement : IBufferElementData
    {
        public int WaveNumber;                  // 波次编号
        public ZombieType ZombieType;           // 僵尸类型
        public int Count;                       // 该类型僵尸数量
        public float SpawnDelay;                // 该波开始后的生成延迟
    }

    /// <summary>
    /// 关卡解锁的植物配置
    /// </summary>
    public struct LevelPlantUnlockElement : IBufferElementData
    {
        public PlantType PlantType;             // 植物类型
        public bool IsAvailable;                // 该关卡是否可用
    }
}
