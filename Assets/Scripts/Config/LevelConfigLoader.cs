using System;
using Unity.Entities;
using UnityEngine;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Config
{
    /// <summary>
    /// 关卡配置加载器 - 从JSON加载关卡配置到ECS
    /// </summary>
    public class LevelConfigLoader : MonoBehaviour
    {
        [Header("关卡配置文件")]
        public TextAsset levelConfigJson;

        [Header("自动加载设置")]
        public bool loadOnStart = true;
        public int levelToLoad = 1; // 默认加载第一关

        [Serializable]
        private class LevelConfigRoot
        {
            public LevelEntry[] levels;
            public int currentLevelId;
        }

        [Serializable]
        private class LevelEntry
        {
            public int levelId;
            public string levelName;
            public string type;
            public string difficulty;
            public MapConfig mapConfig;
            public GameRules gameRules;
            public ZombieSpawnConfig zombieSpawn;
            public SpecialRules specialRules;
            public WaveEntry[] waves;
            public string[] availablePlants;
        }

        [Serializable]
        private class MapConfig
        {
            public int rowCount;
            public int columnCount;
            public float cellWidth;
            public float cellHeight;
        }

        [Serializable]
        private class GameRules
        {
            public float gameDuration;
            public int totalWaves;
            public int maxZombiesReached;
            public int startingSun;
            public float waveIntensityMultiplier;
        }

        [Serializable]
        private class ZombieSpawnConfig
        {
            public float interval;
            public float startDelay;
        }

        [Serializable]
        private class SpecialRules
        {
            public bool hasFog;
            public bool hasPool;
            public bool hasRoof;
            public bool isNightLevel;
            public bool hasGrave;
            public bool isBossLevel;
        }

        [Serializable]
        private class WaveEntry
        {
            public int waveNumber;
            public string zombieType;
            public int count;
            public float spawnDelay;
        }

        void Start()
        {
            if (loadOnStart)
            {
                LoadLevel(levelToLoad);
            }
        }

        public void LoadLevel(int levelId)
        {
            if (levelConfigJson == null)
            {
                UnityEngine.Debug.LogError("LevelConfigLoader: levelConfigJson 未设置！");
                return;
            }

            var root = JsonUtility.FromJson<LevelConfigRoot>(levelConfigJson.text);
            if (root == null || root.levels == null || root.levels.Length == 0)
            {
                UnityEngine.Debug.LogError("LevelConfigLoader: JSON解析失败或没有关卡数据");
                return;
            }

            LevelEntry targetLevel = null;
            foreach (var level in root.levels)
            {
                if (level.levelId == levelId)
                {
                    targetLevel = level;
                    break;
                }
            }

            if (targetLevel == null)
            {
                UnityEngine.Debug.LogError($"LevelConfigLoader: 找不到关卡ID={levelId}");
                return;
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                UnityEngine.Debug.LogError("LevelConfigLoader: World未初始化");
                return;
            }

            var entityManager = world.EntityManager;

            // 创建关卡配置实体
            Entity levelEntity = entityManager.CreateEntity();

            // 解析枚举
            if (!Enum.TryParse<LevelType>(targetLevel.type, true, out var levelType))
                levelType = LevelType.Day;
            if (!Enum.TryParse<DifficultyLevel>(targetLevel.difficulty, true, out var difficulty))
                difficulty = DifficultyLevel.Normal;

            // 添加关卡配置组件
            entityManager.AddComponentData(levelEntity, new LevelConfigComponent
            {
                LevelId = targetLevel.levelId,
                Type = levelType,
                Difficulty = difficulty,
                RowCount = targetLevel.mapConfig.rowCount,
                ColumnCount = targetLevel.mapConfig.columnCount,
                CellWidth = targetLevel.mapConfig.cellWidth,
                CellHeight = targetLevel.mapConfig.cellHeight,
                GameDuration = targetLevel.gameRules.gameDuration,
                TotalWaves = targetLevel.gameRules.totalWaves,
                MaxZombiesReached = targetLevel.gameRules.maxZombiesReached,
                StartingSun = targetLevel.gameRules.startingSun,
                ZombieSpawnInterval = targetLevel.zombieSpawn.interval,
                ZombieSpawnStartDelay = targetLevel.zombieSpawn.startDelay,
                WaveIntensityMultiplier = targetLevel.gameRules.waveIntensityMultiplier,
                HasFog = targetLevel.specialRules.hasFog,
                HasPool = targetLevel.specialRules.hasPool,
                HasRoof = targetLevel.specialRules.hasRoof,
                IsNightLevel = targetLevel.specialRules.isNightLevel,
                HasGrave = targetLevel.specialRules.hasGrave,
                IsBossLevel = targetLevel.specialRules.isBossLevel
            });

            // 添加波次配置缓冲
            var waveBuffer = entityManager.AddBuffer<WaveConfigElement>(levelEntity);
            foreach (var wave in targetLevel.waves)
            {
                if (Enum.TryParse<ZombieType>(wave.zombieType, true, out var zombieType))
                {
                    waveBuffer.Add(new WaveConfigElement
                    {
                        WaveNumber = wave.waveNumber,
                        ZombieType = zombieType,
                        Count = wave.count,
                        SpawnDelay = wave.spawnDelay
                    });
                }
            }

            // 添加植物解锁配置
            var plantUnlockBuffer = entityManager.AddBuffer<LevelPlantUnlockElement>(levelEntity);
            if (targetLevel.availablePlants != null)
            {
                foreach (var plantName in targetLevel.availablePlants)
                {
                    if (Enum.TryParse<PlantType>(plantName, true, out var plantType))
                    {
                        plantUnlockBuffer.Add(new LevelPlantUnlockElement
                        {
                            PlantType = plantType,
                            IsAvailable = true
                        });
                    }
                }
            }

            UnityEngine.Debug.Log($"LevelConfigLoader: 成功加载关卡 [{targetLevel.levelName}] " +
                $"类型={levelType} 难度={difficulty} 波次={targetLevel.gameRules.totalWaves} " +
                $"时长={targetLevel.gameRules.gameDuration}秒");

            // 更新GameStateComponent以使用关卡配置
            UpdateGameStateFromLevel(entityManager, targetLevel);
        }

        private void UpdateGameStateFromLevel(EntityManager entityManager, LevelEntry level)
        {
            var query = entityManager.CreateEntityQuery(typeof(GameStateComponent));
            if (!query.IsEmpty)
            {
                var gameState = query.GetSingleton<GameStateComponent>();
                gameState.TotalGameTime = level.gameRules.gameDuration;
                gameState.RemainingTime = level.gameRules.gameDuration;
                gameState.TotalWaves = level.gameRules.totalWaves;
                gameState.CurrentWave = 0;
                query.SetSingleton(gameState);

                UnityEngine.Debug.Log($"LevelConfigLoader: 已更新GameState - 时长={gameState.TotalGameTime}秒 波次={gameState.TotalWaves}");
            }
            query.Dispose();
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("PVZ/Load Level 1")]
        private static void LoadLevel1()
        {
            var loader = FindObjectOfType<LevelConfigLoader>();
            if (loader != null)
                loader.LoadLevel(1);
            else
                UnityEngine.Debug.LogWarning("场景中没有 LevelConfigLoader");
        }

        [UnityEditor.MenuItem("PVZ/Load Level 2")]
        private static void LoadLevel2()
        {
            var loader = FindObjectOfType<LevelConfigLoader>();
            if (loader != null)
                loader.LoadLevel(2);
            else
                UnityEngine.Debug.LogWarning("场景中没有 LevelConfigLoader");
        }
#endif
    }
}
