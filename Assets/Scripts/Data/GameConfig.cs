using Unity.Entities;
using Unity.Mathematics;

namespace PVZ.DOTS.Data
{
    /// <summary>
    /// 游戏配置数据 - 存储游戏全局配置
    /// </summary>
    public struct GameConfig
    {
        // 网格配置
        public int GridRows;
        public int GridColumns;
        public float GridCellWidth;
        public float GridCellHeight;

        // 资源配置
        public int StartingSun;
        public int MaxSun;

        // 僵尸生成配置
        public float ZombieSpawnInterval;
        public float ZombieSpawnStartDelay;
        public int MaxZombiesPerWave;

        // 游戏时间配置
        public float GameDuration;      // 单局游戏时长（秒）
        public int TotalWaves;          // 总波次数
        public int MaxZombiesReached;   // 最多允许几个僵尸到达终点

        // 默认配置
        public static GameConfig Default => new GameConfig
        {
            GridRows = 5,
            GridColumns = 9,
            GridCellWidth = 1.5f,
            GridCellHeight = 2f,
            StartingSun = 150,
            MaxSun = 9999,
            ZombieSpawnInterval = 5f,
            ZombieSpawnStartDelay = 10f,
            MaxZombiesPerWave = 10,
            GameDuration = 180f,        // 3分钟
            TotalWaves = 5,
            MaxZombiesReached = 5
        };
    }

    /// <summary>
    /// 植物配置数据
    /// </summary>
    public struct PlantConfig
    {
        public int SunCost;
        public float AttackDamage;
        public float AttackInterval;
        public float AttackRange;
        public float Health;
        public float CooldownTime;

        // 豌豆射手默认配置
        public static PlantConfig Peashooter => new PlantConfig
        {
            SunCost = 100,
            AttackDamage = 20f,
            AttackInterval = 1.5f,
            AttackRange = 10f,
            Health = 100f,
            CooldownTime = 7.5f
        };

        // 向日葵默认配置
        public static PlantConfig Sunflower => new PlantConfig
        {
            SunCost = 50,
            AttackDamage = 0f,
            AttackInterval = 0f,
            AttackRange = 0f,
            Health = 100f,
            CooldownTime = 7.5f
        };

        // 坚果墙默认配置
        public static PlantConfig WallNut => new PlantConfig
        {
            SunCost = 50,
            AttackDamage = 0f,
            AttackInterval = 0f,
            AttackRange = 0f,
            Health = 400f,
            CooldownTime = 30f
        };
    }

    /// <summary>
    /// 僵尸配置数据
    /// </summary>
    public struct ZombieConfig
    {
        public float Health;
        public float MovementSpeed;
        public float AttackDamage;
        public float AttackInterval;

        // 普通僵尸默认配置
        public static ZombieConfig Normal => new ZombieConfig
        {
            Health = 100f,
            MovementSpeed = 1f,
            AttackDamage = 10f,
            AttackInterval = 1f
        };

        // 路障僵尸默认配置
        public static ZombieConfig ConeHead => new ZombieConfig
        {
            Health = 200f,
            MovementSpeed = 1f,
            AttackDamage = 10f,
            AttackInterval = 1f
        };

        // 铁桶僵尸默认配置
        public static ZombieConfig BucketHead => new ZombieConfig
        {
            Health = 400f,
            MovementSpeed = 1f,
            AttackDamage = 10f,
            AttackInterval = 1f
        };
    }
}
