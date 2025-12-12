using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Common;

namespace PVZ
{
    /// <summary>
    /// 在场景中挂载此脚本，引用 JSON(TextAsset) 或自动从路径加载，生成一个包�?GameConfigComponent 以及植物/僵尸配置缓冲的实体�?
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
            public string bulletPrefabPath;
        }

        [Serializable]
        private class ZombieConfigEntry
        {
            public string type;
            public float movementSpeed;
            public float attackDamage;
            public float attackInterval;
            public float health;
            public string bulletPrefabPath;
        }

        private bool _isSubscribedToBootstrap = false;
        private GameStateComponent _pendingGameState;
        private bool _hasPendingGameState = false;

        void Awake()  // 改为 Awake 以尽早初始化（保证在系统 Update 之前）
        {
            UnityEngine.Debug.Log("GameConfigLoader: Awake() 开始执行...");
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
                UnityEngine.Debug.LogWarning("GameConfigLoader: 未找到 JSON，使用默认值");
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
                        UnityEngine.Debug.LogWarning("GameConfigLoader: JSON 解析失败，使用默认值");
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("GameConfigLoader: JSON 解析异常: " + e.Message);
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
                UnityEngine.Debug.LogError("GameConfigLoader: 没有可用的 World");
                return;
            }
            var entityManager = world.EntityManager;

            Entity configEntity;
            if (entityManager.CreateEntityQuery(typeof(GameConfigComponent)).CalculateEntityCount() > 0)
            {
                configEntity = entityManager.CreateEntityQuery(typeof(GameConfigComponent)).GetSingletonEntity();
                if (!overwriteExisting)
                {
                    UnityEngine.Debug.Log("GameConfigLoader: 已存在配置实体，未覆盖");
                    return;
                }
            }
            else
            {
                configEntity = entityManager.CreateEntity();
            }

            // 设置或添�?GameConfigComponent（僵尸生成相关）
            // 从第一个僵尸配置获取默认属性，如果没有则使用硬编码默认�?
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
                    SunProductionAmount = p.sunProductionAmount,
                    ProjectilePrefabPath = new FixedString128Bytes(p.bulletPrefabPath ?? string.Empty)
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
                    Health = z.health,
                    ProjectilePrefabPath = new FixedString128Bytes(z.bulletPrefabPath ?? string.Empty)
                });
            }

            // UnityEngine.Debug.Log($"GameConfigLoader: 配置加载完成。植�?{plantBuffer.Length} 僵尸:{zombieBuffer.Length}");

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

            // 尝试立即应用游戏状态到已存在的 GameStateComponent 单例；若不存在，则订阅 Bootstrap 事件以便稍后应用
            TryApplyOrDeferGameState(entityManager, gameStateData);
        }

        private void TryApplyOrDeferGameState(EntityManager entityManager, GameStateComponent gameStateData)
        {
            var gsQuery = entityManager.CreateEntityQuery(typeof(GameStateComponent));
            if (gsQuery.CalculateEntityCount() == 0)
            {
                UnityEngine.Debug.LogWarning("GameConfigLoader: 未找到 GameStateComponent 单例，延迟初始化并订阅 GameBootstrap.OnGameStateSingletonCreated");
                _pendingGameState = gameStateData;
                _hasPendingGameState = true;
                if (!_isSubscribedToBootstrap)
                {
                    Framework.GameBootstrap.OnGameStateSingletonCreated += OnGameStateSingletonCreated;
                    _isSubscribedToBootstrap = true;
                }
            }
            else
            {
                var gameStateEntity = gsQuery.GetSingletonEntity();
                if (entityManager.HasComponent<GameStateComponent>(gameStateEntity))
                    entityManager.SetComponentData(gameStateEntity, gameStateData);
                else
                    entityManager.AddComponentData(gameStateEntity, gameStateData);

                UnityEngine.Debug.Log($"GameConfigLoader: 游戏状态初始化完成。状态={gameStateData.CurrentState} 时长:{gameStateData.TotalGameTime} 波次:{gameStateData.TotalWaves}");
            }
            gsQuery.Dispose();
        }

        private void OnGameStateSingletonCreated()
        {
            try
            {
                if (!_hasPendingGameState)
                    return;

                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null)
                {
                    UnityEngine.Debug.LogError("GameConfigLoader: 收到 GameState 创建事件，但没有可用的 World");
                    return;
                }
                var em = world.EntityManager;
                // 再次尝试应用
                TryApplyOrDeferGameState(em, _pendingGameState);
                // 已应用后清理和退订
                _hasPendingGameState = false;
                if (_isSubscribedToBootstrap)
                {
                    Framework.GameBootstrap.OnGameStateSingletonCreated -= OnGameStateSingletonCreated;
                    _isSubscribedToBootstrap = false;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"GameConfigLoader.OnGameStateSingletonCreated 错误: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            if (_isSubscribedToBootstrap)
            {
                Framework.GameBootstrap.OnGameStateSingletonCreated -= OnGameStateSingletonCreated;
                _isSubscribedToBootstrap = false;
            }
        }
    }
}
