using System;
using Unity.Entities;
using UnityEngine;
using PVZ.DOTS.Components;
using PVZ.DOTS.Utils;
using Debug = UnityEngine.Debug;

namespace PVZ.DOTS.Config
{
    /// <summary>
    /// 在场景中挂载此脚本，引用 JSON(TextAsset) 或自动从路径加载，生成一个包含 GameConfigComponent 以及植物/僵尸配置缓冲的实体。
    /// </summary>
    public class GameConfigLoader : MonoBehaviour
    {
        [Header("配置 JSON (可选，若为空则使用默认路径 Assets/Configs/GameConfig.json)")]
        public TextAsset configJson;

        [Header("加载时如果已有配置实体则覆盖更新")]
        public bool overwriteExisting = true;

        [Serializable]
        private class Root
        {
            public GameSettings gameSettings;
            public ZombieSpawnConfig zombieSpawn;
            public PlantConfigEntry[] plants;
            public ZombieConfigEntry[] zombies;
        }

        [Serializable]
        private class GameSettings
        {
            public float gameDuration = 180f;
            public int totalWaves = 5;
            public int maxZombiesReached = 5;
        }

        [Serializable]
        private class ZombieSpawnConfig
        {
            public float interval = 5f;
            public float startDelay = 0f;
            public int laneCount = 5;
            public float spawnX = 15f;
            public float laneZSpacing = 2f;
            public float laneZOffset = -4f;
        }

        [Serializable]
        private class PlantConfigEntry
        {
            public string type;
            public int sunCost;
            public float attackDamage;
            public float attackInterval;
            public float attackRange;
            public float health;
            public float sunProductionInterval;
            public int sunProductionAmount;
        }

        [Serializable]
        private class ZombieConfigEntry
        {
            public string type;
            public float movementSpeed;
            public float attackDamage;
            public float attackInterval;
            public float health;
        }

        void Start()  // 改为Start以确保World已初始化
        {
            GameLogger.Log("GameConfigLoader", "Start() 开始执行...");
            string json = null;
            if (configJson != null)
            {
                json = configJson.text;
            }
            else
            {
                // 允许使用 Resources 或磁盘路径（这里简单使用磁盘路径）
                var path = System.IO.Path.Combine(Application.dataPath, "Configs", "GameConfig.json");
                if (System.IO.File.Exists(path))
                {
                    json = System.IO.File.ReadAllText(path);
                }
            }

            Root root;
            if (string.IsNullOrEmpty(json))
            {
                root = new Root
                {
                    gameSettings = new GameSettings(),
                    zombieSpawn = new ZombieSpawnConfig(),
                    plants = Array.Empty<PlantConfigEntry>(),
                    zombies = Array.Empty<ZombieConfigEntry>()
                };
                GameLogger.LogWarning("GameConfigLoader", "未找到 JSON，使用默认值。");
            }
            else
            {
                try
                {
                    root = JsonUtility.FromJson<Root>(json);
                    if (root == null)
                    {
                        root = new Root
                        {
                            gameSettings = new GameSettings(),
                            zombieSpawn = new ZombieSpawnConfig(),
                            plants = Array.Empty<PlantConfigEntry>(),
                            zombies = Array.Empty<ZombieConfigEntry>()
                        };
                        GameLogger.LogWarning("GameConfigLoader", "JSON 解析失败，使用默认值。");
                    }
                }
                catch (Exception e)
                {
                    GameLogger.LogError("GameConfigLoader", "JSON 解析异常: " + e.Message);
                    root = new Root
                    {
                        gameSettings = new GameSettings(),
                        zombieSpawn = new ZombieSpawnConfig(),
                        plants = Array.Empty<PlantConfigEntry>(),
                        zombies = Array.Empty<ZombieConfigEntry>()
                    };
                }
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                GameLogger.LogError("GameConfigLoader", "没有可用的 World。");
                return;
            }
            var entityManager = world.EntityManager;

            Entity configEntity;
            if (entityManager.CreateEntityQuery(typeof(GameConfigComponent)).CalculateEntityCount() > 0)
            {
                configEntity = entityManager.CreateEntityQuery(typeof(GameConfigComponent)).GetSingletonEntity();
                if (!overwriteExisting)
                {
                    GameLogger.Log("GameConfigLoader", "已存在配置实体，未覆盖。");
                    return;
                }
            }
            else
            {
                configEntity = entityManager.CreateEntity();
            }

            // 设置或添加 GameConfigComponent（僵尸生成相关）
            // 从第一个僵尸配置获取默认属性，如果没有则使用硬编码默认值
            float defaultZombieSpeed = 1.0f;
            float defaultZombieAttack = 10.0f;
            float defaultZombieInterval = 1.0f;
            float defaultZombieHealth = 100.0f;

            if (root.zombies != null && root.zombies.Length > 0)
            {
                var firstZombie = root.zombies[0];
                defaultZombieSpeed = firstZombie.movementSpeed;
                defaultZombieAttack = firstZombie.attackDamage;
                defaultZombieInterval = firstZombie.attackInterval;
                defaultZombieHealth = firstZombie.health;
            }

            var componentData = new GameConfigComponent
            {
                ZombieSpawnInterval = root.zombieSpawn.interval,
                ZombieSpawnStartDelay = root.zombieSpawn.startDelay,
                LaneCount = root.zombieSpawn.laneCount,
                SpawnX = root.zombieSpawn.spawnX,
                LaneZSpacing = root.zombieSpawn.laneZSpacing,
                LaneZOffset = root.zombieSpawn.laneZOffset,
                ZombieMovementSpeed = defaultZombieSpeed,
                ZombieAttackDamage = defaultZombieAttack,
                ZombieAttackInterval = defaultZombieInterval,
                ZombieHealth = defaultZombieHealth
            };
            if (entityManager.HasComponent<GameConfigComponent>(configEntity))
                entityManager.SetComponentData(configEntity, componentData);
            else
                entityManager.AddComponentData(configEntity, componentData);

            // 植物配置缓冲
            DynamicBuffer<PlantConfigElement> plantBuffer;
            if (!entityManager.HasBuffer<PlantConfigElement>(configEntity))
                plantBuffer = entityManager.AddBuffer<PlantConfigElement>(configEntity);
            else
            {
                plantBuffer = entityManager.GetBuffer<PlantConfigElement>(configEntity);
                plantBuffer.Clear();
            }

            foreach (var p in root.plants)
            {
                if (!Enum.TryParse<PlantType>(p.type, true, out var plantType))
                {
                    GameLogger.LogWarning("GameConfigLoader", $"未知植物类型: {p.type}");
                    continue;
                }
                plantBuffer.Add(new PlantConfigElement
                {
                    Type = plantType,
                    SunCost = p.sunCost,
                    AttackDamage = p.attackDamage,
                    AttackInterval = p.attackInterval,
                    AttackRange = p.attackRange,
                    Health = p.health,
                    SunProductionInterval = p.sunProductionInterval,
                    SunProductionAmount = p.sunProductionAmount
                });
            }

            // 僵尸配置缓冲
            DynamicBuffer<ZombieConfigElement> zombieBuffer;
            if (!entityManager.HasBuffer<ZombieConfigElement>(configEntity))
                zombieBuffer = entityManager.AddBuffer<ZombieConfigElement>(configEntity);
            else
            {
                zombieBuffer = entityManager.GetBuffer<ZombieConfigElement>(configEntity);
                zombieBuffer.Clear();
            }

            foreach (var z in root.zombies)
            {
                if (!Enum.TryParse<ZombieType>(z.type, true, out var zombieType))
                {
                    GameLogger.LogWarning("GameConfigLoader", $"未知僵尸类型: {z.type}");
                    continue;
                }
                zombieBuffer.Add(new ZombieConfigElement
                {
                    Type = zombieType,
                    MovementSpeed = z.movementSpeed,
                    AttackDamage = z.attackDamage,
                    AttackInterval = z.attackInterval,
                    Health = z.health
                });
            }

            // UnityEngine.Debug.Log($"GameConfigLoader: 配置加载完成。植物:{plantBuffer.Length} 僵尸:{zombieBuffer.Length}");

            // 创建或更新 GameStateComponent (单局游戏状态)
            Entity gameStateEntity;
            if (entityManager.CreateEntityQuery(typeof(GameStateComponent)).CalculateEntityCount() > 0)
            {
                gameStateEntity = entityManager.CreateEntityQuery(typeof(GameStateComponent)).GetSingletonEntity();
            }
            else
            {
                gameStateEntity = entityManager.CreateEntity();
            }

            var gameStateData = new GameStateComponent
            {
                CurrentState = GameState.Playing,  // 自动开始游戏
                RemainingTime = root.gameSettings?.gameDuration ?? 180f,
                TotalGameTime = root.gameSettings?.gameDuration ?? 180f,
                CurrentWave = 0,
                TotalWaves = root.gameSettings?.totalWaves ?? 5,
                ZombiesKilled = 0,
                ZombiesReachedEnd = 0
            };

            if (entityManager.HasComponent<GameStateComponent>(gameStateEntity))
                entityManager.SetComponentData(gameStateEntity, gameStateData);
            else
                entityManager.AddComponentData(gameStateEntity, gameStateData);

            GameLogger.Log("GameConfigLoader", $"游戏状态初始化完成。状态:{gameStateData.CurrentState} 时长:{gameStateData.TotalGameTime}秒 波次:{gameStateData.TotalWaves}");
        }
    }
}
