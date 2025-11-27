using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using PVZ.DOTS.Components;
using PVZ.DOTS.Utils;
using System;

namespace PVZ.DOTS.Debug
{
    /// <summary>
    /// 性能测试生成器 - 自动生成植物和僵尸用于性能测试
    /// 完整模拟游戏流程，包括关卡加载和游戏状态管理
    /// </summary>
    public class PerformanceTestSpawner : MonoBehaviour
    {
        [Header("测试控制")]
        [Tooltip("是否启用自动生成")]
        public bool enableAutoSpawn = false;

        [Tooltip("是否在Start时自动开始")]
        public bool autoStartOnPlay = false;

        [Header("视图模型配置")]
        [Tooltip("是否加载和显示视图模型")]
        public bool enableViewLoading = true;

        [Tooltip("Mesh预制体路径（Resources相对路径，例如：Prefabs/TestMesh）")]
        public string meshPrefabPath = "Prefabs/TestMesh";

        [Tooltip("Spine预制体路径（Resources相对路径，例如：Prefabs/TestSpine）")]
        public string spinePrefabPath = "Prefabs/TestSpine";

        [Header("游戏配置")]
        [Tooltip("游戏配置文件")]
        public TextAsset gameConfigJson;

        [Header("关卡配置")]
        [Tooltip("关卡配置文件")]
        public TextAsset levelConfigJson;

        [Tooltip("要加载的关卡ID")]
        [Range(1, 5)]
        public int testLevelId = 2;

        [Tooltip("是否自动启动游戏状态为Playing")]
        public bool autoSetGamePlaying = true;

        [Header("植物生成配置")]
        [Tooltip("是否自动生成植物（性能测试专用）")]
        public bool enableAutoPlantSpawn = true;

        [Tooltip("植物生成间隔（秒）")]
        [Range(0.01f, 5f)]
        public float plantSpawnInterval = 0.1f;

        [Tooltip("每批次生成植物数量")]
        [Range(1, 100)]
        public int plantsPerBatch = 1;

        [Tooltip("植物生成类型")]
        public PlantType plantType = PlantType.Peashooter;

        [Tooltip("最大植物数量（0=无限制）")]
        [Range(0, 10000)]
        public int maxPlants = 1000;

        [Header("僵尸生成配置")]
        [Tooltip("使用关卡配置的僵尸生成（推荐）")]
        public bool useDefaultZombieSpawn = true;

        [Tooltip("是否额外生成僵尸（性能测试）")]
        public bool enableExtraZombieSpawn = false;

        [Tooltip("僵尸生成间隔（秒）")]
        [Range(0.01f, 5f)]
        public float zombieSpawnInterval = 0.5f;

        [Tooltip("每批次生成僵尸数量")]
        [Range(1, 100)]
        public int zombiesPerBatch = 1;

        [Tooltip("僵尸生成类型")]
        public ZombieType zombieType = ZombieType.Normal;

        [Tooltip("最大僵尸数量（0=无限制）")]
        [Range(0, 10000)]
        public int maxZombies = 500;

        [Header("统计信息")]
        [Tooltip("当前植物数量")]
        [SerializeField]
        private int currentPlantCount = 0;

        [Tooltip("当前僵尸数量")]
        [SerializeField]
        private int currentZombieCount = 0;

        [Tooltip("总生成植物数")]
        [SerializeField]
        private int totalPlantsSpawned = 0;

        [Tooltip("总生成僵尸数")]
        [SerializeField]
        private int totalZombiesSpawned = 0;

        private EntityManager _entityManager;
        private float _lastPlantSpawnTime;
        private float _lastZombieSpawnTime;
        private Unity.Mathematics.Random _random;
        private int _rowCount;
        private int _columnCount;
        private float _cellSize;
        private bool _initialized = false;
        private PVZ.DOTS.GameLoader _gameLoader;
        private bool _lastGamePlayingState = true; // 记录上一次的游戏状态

        private void Start()
        {
            UnityEngine.Debug.Log("=== PerformanceTestSpawner: Start 开始 ===");
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                _entityManager = world.EntityManager;
                UnityEngine.Debug.Log("PerformanceTestSpawner: EntityManager 已获取");
            }
            else
            {
                UnityEngine.Debug.LogError("PerformanceTestSpawner: 无法获取 World!");
            }

            _random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
            _lastPlantSpawnTime = Time.time;
            _lastZombieSpawnTime = Time.time;

            // 使用GameLoader加载关卡配置
            LoadLevelConfig();

            if (autoStartOnPlay)
            {
                enableAutoSpawn = true;
                GameLogger.Log("PerformanceTest", "性能测试自动启动");
            }
            
            UnityEngine.Debug.Log("=== PerformanceTestSpawner: Start 结束 ===");
        }

        private void LoadLevelConfig()
        {
            UnityEngine.Debug.Log("=== PerformanceTestSpawner: LoadLevelConfig 开始 ===");
            
            if (levelConfigJson == null)
            {
                GameLogger.LogWarning("PerformanceTest", "未设置关卡配置文件，使用默认配置");
                UnityEngine.Debug.LogWarning("PerformanceTestSpawner: levelConfigJson 为 null!");
                return;
            }

            UnityEngine.Debug.Log($"PerformanceTestSpawner: levelConfigJson 已设置，准备加载关卡 {testLevelId}");

            // 查找或创建GameLoader
            var loaderObj = GameObject.Find("GameLoader");
            if (loaderObj == null)
            {
                UnityEngine.Debug.Log("PerformanceTestSpawner: 未找到 GameLoader，创建新的");
                loaderObj = new GameObject("GameLoader");
                _gameLoader = loaderObj.AddComponent<PVZ.DOTS.GameLoader>();
            }
            else
            {
                UnityEngine.Debug.Log("PerformanceTestSpawner: 找到已存在的 GameLoader");
                _gameLoader = loaderObj.GetComponent<PVZ.DOTS.GameLoader>();
            }

            if (_gameLoader != null)
            {
                _gameLoader.levelConfigJson = levelConfigJson;
                _gameLoader.gameConfigJson = gameConfigJson;
                _gameLoader.levelToLoad = testLevelId;

                // 注册回调
                _gameLoader.OnLoadComplete += OnLoadComplete;
                _gameLoader.OnLevelConfigLoaded += OnLevelConfigLoaded;

                UnityEngine.Debug.Log("PerformanceTestSpawner: 回调已注册，开始加载...");
                
                // 开始加载
                _gameLoader.StartLoad();

                GameLogger.Log("PerformanceTest", $"使用GameLoader加载关卡 {testLevelId}");
            }
            else
            {
                UnityEngine.Debug.LogError("PerformanceTestSpawner: GameLoader 组件为 null!");
            }
            
            UnityEngine.Debug.Log("=== PerformanceTestSpawner: LoadLevelConfig 结束 ===");
        }

        private void OnLoadComplete()
        {
            UnityEngine.Debug.Log("=== PerformanceTestSpawner: OnLoadComplete 回调触发 ===");
            GameLogger.Log("PerformanceTest", "关卡加载完成");
            GameStateManager.Instance.SetGameStatePlaying();
            UnityEngine.Debug.Log("PerformanceTestSpawner: 游戏状态已设置为 Playing");
        }

        private void OnLevelConfigLoaded(PVZ.DOTS.Components.LevelConfigComponent levelConfig)
        {
            UnityEngine.Debug.Log("=== PerformanceTestSpawner: OnLevelConfigLoaded 回调触发 ===");
            _rowCount = levelConfig.RowCount;
            _columnCount = levelConfig.ColumnCount;
            _cellSize = levelConfig.CellWidth;
            _initialized = true;
            GameLogger.Log("PerformanceTest", $"关卡配置已加载：{_rowCount}行 × {_columnCount}列，格子大小={_cellSize}");
            UnityEngine.Debug.Log($"PerformanceTestSpawner: _initialized = true, 行={_rowCount}, 列={_columnCount}");
        }

        private void Update()
        {
            if (!enableAutoSpawn || _entityManager == null)
                return;

            // 检查游戏状态，只在Playing时生成
            bool isPlaying = IsGamePlaying();
            
            // 状态变化时输出日志
            if (isPlaying != _lastGamePlayingState)
            {
                if (!isPlaying)
                {
                    UnityEngine.Debug.Log("PerformanceTestSpawner: 游戏已结束，停止生成实体");
                }
                else
                {
                    UnityEngine.Debug.Log("PerformanceTestSpawner: 游戏已开始，恢复生成实体");
                }
                _lastGamePlayingState = isPlaying;
            }
            
            if (!isPlaying)
            {
                return;
            }

            // 初始化地图配置
            if (!_initialized)
            {
                InitializeMapConfig();
            }

            // 更新统计信息
            UpdateStatistics();

            // 自动生成植物（性能测试专用）
            if (enableAutoPlantSpawn && (maxPlants == 0 || currentPlantCount < maxPlants))
            {
                if (Time.time - _lastPlantSpawnTime >= plantSpawnInterval)
                {
                    SpawnPlants();
                    _lastPlantSpawnTime = Time.time;
                }
            }

            // 额外生成僵尸（性能测试，不影响关卡正常僵尸生成）
            if (enableExtraZombieSpawn && (maxZombies == 0 || currentZombieCount < maxZombies))
            {
                if (Time.time - _lastZombieSpawnTime >= zombieSpawnInterval)
                {
                    SpawnZombies();
                    _lastZombieSpawnTime = Time.time;
                }
            }
        }

        private void InitializeMapConfig()
        {
            if (_entityManager == null)
            {
                GameLogger.LogWarning("PerformanceTest", "EntityManager未初始化");
                return;
            }

            // 从关卡配置读取
            var query = _entityManager.CreateEntityQuery(typeof(LevelConfigComponent));

            if (query.TryGetSingleton<LevelConfigComponent>(out var levelConfig))
            {
                _rowCount = levelConfig.RowCount;
                _columnCount = levelConfig.ColumnCount;
                _cellSize = levelConfig.CellWidth;
                _initialized = true;
                GameLogger.Log("PerformanceTest", $"使用关卡配置：{_rowCount}行 × {_columnCount}列，格子大小={_cellSize}");
            }
            else
            {
                GameLogger.LogWarning("PerformanceTest", "未找到关卡配置，等待加载");
            }

            query.Dispose();
        }

        /// <summary>
        /// 检查游戏是否在Playing状态
        /// </summary>
        private bool IsGamePlaying()
        {
            if (_entityManager == null)
                return false;

            var query = _entityManager.CreateEntityQuery(typeof(GameStateComponent));
            bool isPlaying = false;

            if (query.TryGetSingleton<GameStateComponent>(out var gameState))
            {
                isPlaying = gameState.CurrentState == GameState.Playing;
            }

            query.Dispose();
            return isPlaying;
        }

        private void SpawnPlants()
        {
            for (int i = 0; i < plantsPerBatch; i++)
            {
                // 随机选择网格位置
                int row = _random.NextInt(0, _rowCount);
                int column = _random.NextInt(0, Mathf.FloorToInt(_columnCount / 2));

                // 计算世界坐标（左下角为原点0,0,0）
                float worldX = column * _cellSize;
                float worldZ = row * _cellSize;

                // 创建植物实体
                Entity plantEntity = _entityManager.CreateEntity();

                _entityManager.AddComponentData(plantEntity, new PlantComponent
                {
                    Type = plantType,
                    SunCost = 100,
                    AttackDamage = 20f,
                    AttackInterval = 1.5f,
                    AttackRange = 10f,
                    LastAttackTime = 0f
                });

                _entityManager.AddComponentData(plantEntity, new HealthComponent
                {
                    CurrentHealth = 300f,
                    MaxHealth = 300f,
                    IsDead = false
                });

                _entityManager.AddComponentData(plantEntity, new GridPositionComponent
                {
                    Row = row,
                    Column = column,
                    WorldPosition = new float3(worldX, 0, worldZ)
                });

                _entityManager.AddComponentData(plantEntity, LocalTransform.FromPosition(new float3(worldX, 0, worldZ)));

                // 添加视图预制体组件（如果启用）
                if (enableViewLoading)
                {
                    // 根据ViewSystemConfig自动选择预制体类型
                    var config = Config.ViewSystemConfig.Instance;
                    string prefabPath = config.enableSpineSystem ? spinePrefabPath : meshPrefabPath;
                    
                    _entityManager.AddComponentData(plantEntity, new ViewPrefabComponent
                    {
                        PrefabPath = prefabPath,
                        IsViewLoaded = false
                    });
                }

                totalPlantsSpawned++;
            }
        }

        private void SpawnZombies()
        {
            for (int i = 0; i < zombiesPerBatch; i++)
            {
                // 随机选择行
                int row = _random.NextInt(0, _rowCount);
                // 僵尸从地图右侧外部生成
                int column = _columnCount;

                // 计算世界坐标
                float worldX = column * _cellSize;
                float worldZ = row * _cellSize;

                // 创建僵尸实体
                Entity zombieEntity = _entityManager.CreateEntity();

                _entityManager.AddComponentData(zombieEntity, new ZombieComponent
                {
                    Type = zombieType,
                    MovementSpeed = 1.0f,
                    AttackDamage = 10f,
                    AttackInterval = 1.0f,
                    LastAttackTime = 0f,
                    Lane = row
                });

                _entityManager.AddComponentData(zombieEntity, new HealthComponent
                {
                    CurrentHealth = 100f,
                    MaxHealth = 100f,
                    IsDead = false
                });

                _entityManager.AddComponentData(zombieEntity, new GridPositionComponent
                {
                    Row = row,
                    Column = column,
                    WorldPosition = new float3(worldX, 0, worldZ)
                });

                _entityManager.AddComponentData(zombieEntity, LocalTransform.FromPosition(new float3(worldX, 0, worldZ)));

                // 添加视图预制体组件（如果启用）
                if (enableViewLoading)
                {
                    // 根据ViewSystemConfig自动选择预制体类型
                    var config = Config.ViewSystemConfig.Instance;
                    string prefabPath = config.enableSpineSystem ? spinePrefabPath : meshPrefabPath;
                    
                    _entityManager.AddComponentData(zombieEntity, new ViewPrefabComponent
                    {
                        PrefabPath = prefabPath,
                        IsViewLoaded = false
                    });
                }

                totalZombiesSpawned++;
            }
        }

        private void UpdateStatistics()
        {
            // 统计当前实体数量
            var plantQuery = _entityManager.CreateEntityQuery(typeof(PlantComponent));
            currentPlantCount = plantQuery.CalculateEntityCount();
            plantQuery.Dispose();

            var zombieQuery = _entityManager.CreateEntityQuery(typeof(ZombieComponent));
            currentZombieCount = zombieQuery.CalculateEntityCount();
            zombieQuery.Dispose();
        }

        private void OnGUI()
        {
            // 绘制性能测试面板
            GUILayout.BeginArea(new Rect(10, 400, 350, 350));
            GUILayout.BeginVertical("box");

            GUILayout.Label("=== 性能测试控制面板 ===", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
            GUILayout.Space(10);

            // 统计信息
            GUILayout.Label($"当前植物数量: {currentPlantCount}", new GUIStyle(GUI.skin.label) { fontSize = 14 });
            GUILayout.Label($"当前僵尸数量: {currentZombieCount}", new GUIStyle(GUI.skin.label) { fontSize = 14 });
            GUILayout.Label($"总生成植物数: {totalPlantsSpawned}", new GUIStyle(GUI.skin.label) { fontSize = 12 });
            GUILayout.Label($"总生成僵尸数: {totalZombiesSpawned}", new GUIStyle(GUI.skin.label) { fontSize = 12 });

            GUILayout.Space(10);

            // 性能信息
            float fps = 1f / Time.deltaTime;
            Color fpsColor = fps > 30 ? Color.green : (fps > 15 ? Color.yellow : Color.red);
            GUIStyle fpsStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = fpsColor } };
            GUILayout.Label($"FPS: {fps:F1}", fpsStyle);

            // 显示地图配置
            if (_initialized)
            {
                GUILayout.Label($"地图: {_rowCount}行×{_columnCount}列 (格子:{_cellSize})", new GUIStyle(GUI.skin.label) { fontSize = 10 });
            }

            // 显示生成状态
            GUILayout.Label($"状态: {(enableAutoSpawn ? "运行中" : "已暂停")}", new GUIStyle(GUI.skin.label) { fontSize = 10 });
            GUILayout.Label($"关卡: Level {testLevelId}", new GUIStyle(GUI.skin.label) { fontSize = 10 });
            if (enableAutoSpawn)
            {
                if (enableAutoPlantSpawn)
                    GUILayout.Label($"→ 自动植物: {plantSpawnInterval:F2}s间隔, {plantsPerBatch}个/批", new GUIStyle(GUI.skin.label) { fontSize = 10 });
                if (useDefaultZombieSpawn)
                    GUILayout.Label($"→ 关卡僵尸: 使用关卡配置", new GUIStyle(GUI.skin.label) { fontSize = 10 });
                if (enableExtraZombieSpawn)
                    GUILayout.Label($"→ 额外僵尸: {zombieSpawnInterval:F2}s间隔, {zombiesPerBatch}个/批", new GUIStyle(GUI.skin.label) { fontSize = 10 });
            }

            GUILayout.Space(10);

            // 控制按钮
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(enableAutoSpawn ? "暂停生成" : "开始生成", GUILayout.Height(30)))
            {
                enableAutoSpawn = !enableAutoSpawn;
                GameLogger.Log("PerformanceTest", enableAutoSpawn ? "开始生成" : "暂停生成");
            }

            if (GUILayout.Button("清空所有", GUILayout.Height(30)))
            {
                ClearAllEntities();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 生成速率调整
            GUILayout.Label("植物生成间隔:", new GUIStyle(GUI.skin.label) { fontSize = 12 });
            float newPlantInterval = GUILayout.HorizontalSlider(plantSpawnInterval, 0.01f, 2f);
            if (Mathf.Abs(newPlantInterval - plantSpawnInterval) > 0.001f)
            {
                plantSpawnInterval = newPlantInterval;
            }
            GUILayout.Label($"{plantSpawnInterval:F2}秒", new GUIStyle(GUI.skin.label) { fontSize = 10 });

            GUILayout.Space(5);

            GUILayout.Label("每批次植物数量:", new GUIStyle(GUI.skin.label) { fontSize = 12 });
            float newBatchSize = GUILayout.HorizontalSlider(plantsPerBatch, 1, 50);
            if (Mathf.Abs(newBatchSize - plantsPerBatch) > 0.1f)
            {
                plantsPerBatch = (int)newBatchSize;
            }
            GUILayout.Label($"{plantsPerBatch}个", new GUIStyle(GUI.skin.label) { fontSize = 10 });

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        /// <summary>
        /// 清空所有生成的实体
        /// </summary>
        public void ClearAllEntities()
        {
            if (_entityManager == null)
                return;

            // 清除所有植物
            var plantQuery = _entityManager.CreateEntityQuery(typeof(PlantComponent));
            _entityManager.DestroyEntity(plantQuery);
            plantQuery.Dispose();

            // 清除所有僵尸
            var zombieQuery = _entityManager.CreateEntityQuery(typeof(ZombieComponent));
            _entityManager.DestroyEntity(zombieQuery);
            zombieQuery.Dispose();

            totalPlantsSpawned = 0;
            totalZombiesSpawned = 0;
            currentPlantCount = 0;
            currentZombieCount = 0;

            GameLogger.Log("PerformanceTest", "已清空所有实体");
        }

        /// <summary>
        /// 批量生成指定数量的植物
        /// </summary>
        public void SpawnPlantsBatch(int count)
        {
            int originalBatch = plantsPerBatch;
            plantsPerBatch = count;
            SpawnPlants();
            plantsPerBatch = originalBatch;
            GameLogger.Log("PerformanceTest", $"批量生成 {count} 个植物");
        }

        /// <summary>
        /// 批量生成指定数量的僵尸
        /// </summary>
        public void SpawnZombiesBatch(int count)
        {
            int originalBatch = zombiesPerBatch;
            zombiesPerBatch = count;
            SpawnZombies();
            zombiesPerBatch = originalBatch;
            GameLogger.Log("PerformanceTest", $"批量生成 {count} 个僵尸");
        }

#if UNITY_EDITOR
        [ContextMenu("开始性能测试")]
        private void StartPerformanceTest()
        {
            enableAutoSpawn = true;
            GameLogger.Log("PerformanceTest", "通过菜单启动性能测试");
        }

        [ContextMenu("停止性能测试")]
        private void StopPerformanceTest()
        {
            enableAutoSpawn = false;
            GameLogger.Log("PerformanceTest", "停止性能测试");
        }

        [ContextMenu("生成100个植物")]
        private void Spawn100Plants()
        {
            if (!_initialized)
                InitializeMapConfig();
            SpawnPlantsBatch(100);
        }

        [ContextMenu("生成1000个植物")]
        private void Spawn1000Plants()
        {
            if (!_initialized)
                InitializeMapConfig();
            SpawnPlantsBatch(1000);
        }

        [ContextMenu("清空所有实体")]
        private void ClearAll()
        {
            ClearAllEntities();
        }
#endif
    }
}
