using System;
using Unity.Entities;
using UnityEngine;
using PVZ.DOTS.Components;
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
            public ZombieSpawnConfig zombieSpawn;
            public PlantConfigEntry[] plants;
            public ZombieConfigEntry[] zombies;
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

        void Awake()
        {
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
                    zombieSpawn = new ZombieSpawnConfig(),
                    plants = Array.Empty<PlantConfigEntry>(),
                    zombies = Array.Empty<ZombieConfigEntry>()
                };
                UnityEngine.Debug.LogWarning("GameConfigLoader: 未找到 JSON，使用默认值。");
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
                            zombieSpawn = new ZombieSpawnConfig(),
                            plants = Array.Empty<PlantConfigEntry>(),
                            zombies = Array.Empty<ZombieConfigEntry>()
                        };
                        UnityEngine.Debug.LogWarning("GameConfigLoader: JSON 解析失败，使用默认值。");
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("GameConfigLoader: JSON 解析异常: " + e.Message);
                    root = new Root
                    {
                        zombieSpawn = new ZombieSpawnConfig(),
                        plants = Array.Empty<PlantConfigEntry>(),
                        zombies = Array.Empty<ZombieConfigEntry>()
                    };
                }
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                UnityEngine.Debug.LogError("GameConfigLoader: 没有可用的 World。");
                return;
            }
            var entityManager = world.EntityManager;

            Entity configEntity;
            if (entityManager.CreateEntityQuery(typeof(GameConfigComponent)).CalculateEntityCount() > 0)
            {
                configEntity = entityManager.CreateEntityQuery(typeof(GameConfigComponent)).GetSingletonEntity();
                if (!overwriteExisting)
                {
                    UnityEngine.Debug.Log("GameConfigLoader: 已存在配置实体，未覆盖。");
                    return;
                }
            }
            else
            {
                configEntity = entityManager.CreateEntity();
            }

            // 设置或添加 GameConfigComponent（僵尸生成相关）
            var componentData = new GameConfigComponent
            {
                ZombieSpawnInterval = root.zombieSpawn.interval,
                ZombieSpawnStartDelay = root.zombieSpawn.startDelay,
                LaneCount = root.zombieSpawn.laneCount,
                SpawnX = root.zombieSpawn.spawnX,
                LaneZSpacing = root.zombieSpawn.laneZSpacing,
                LaneZOffset = root.zombieSpawn.laneZOffset,
                ZombieMovementSpeed = 0f,
                ZombieAttackDamage = 0f,
                ZombieAttackInterval = 0f,
                ZombieHealth = 0f
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
                    UnityEngine.Debug.LogWarning($"未知植物类型: {p.type}");
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
                    UnityEngine.Debug.LogWarning($"未知僵尸类型: {z.type}");
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

            UnityEngine.Debug.Log($"GameConfigLoader: 配置加载完成。植物:{plantBuffer.Length} 僵尸:{zombieBuffer.Length}");
        }
    }
}
