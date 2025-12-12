using System;
using System.Collections.Generic;
using Unity.Entities;
using Common;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 游戏启动�?- 模块化启动入�?
    /// 负责加载配置、初始化模块、创建ECS World
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrap : MonoBehaviour
    {
        public static event Action OnGameStateSingletonCreated;
        [Header("配置")]
        [Tooltip("游戏模块配置文件")]
        public TextAsset gameConfigJson;

        [Tooltip("是否在启动时自动初始化模块系统")]
        public bool autoInitialize = true;

        [Header("调试")]
        [Tooltip("显示详细日志")]
        public bool verboseLogging = true;

        private ModuleRegistry _moduleRegistry;
        private GameConfiguration _gameConfig;
        private World _gameWorld;

        /// <summary>
        /// 全局单例实例
        /// </summary>
        public static GameBootstrap Instance { get; private set; }

        /// <summary>
        /// 全局模块上下文（供System获取服务�?
        /// </summary>
        public IModuleContext Context => _moduleRegistry;

        private void Awake()
        {
            // 设置单例
            if (Instance != null && Instance != this)
            {
                UnityEngine.Debug.LogWarning("GameBootstrap 单例已存在，销毁重复实例");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            UnityEngine.Debug.Log("====== GameBootstrap 启动 ======");

            if (autoInitialize)
            {
                Initialize();
            }
        }

        /// <summary>
        /// 初始化游�?
        /// </summary>
        public void Initialize()
        {
            try
            {
                // 1. 加载配置
                LoadConfiguration();

                // 2. 创建模块注册�?
                CreateModuleRegistry();

                // 3. 创建ECS World
                CreateECSWorld();

                // 4. 注册所有模�?
                RegisterAllModules();

                // 4.5 确保 GameStateComponent 单例由 Bootstrap 创建，供系统依赖
                EnsureGameStateSingleton();

                // 5. 初始化模�?
                _moduleRegistry.InitializeAllModules();

                UnityEngine.Debug.Log("====== 游戏初始化完�?======");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"游戏初始化失�? {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void EnsureGameStateSingleton()
        {
            try
            {
                if (_gameWorld == null)
                    return;

                var em = _gameWorld.EntityManager;
                var query = em.CreateEntityQuery(typeof(GameStateComponent));
                if (query.CalculateEntityCount() == 0)
                {
                    var e = em.CreateEntity();
                    var data = new GameStateComponent
                    {
                        CurrentState = GameState.Playing,
                        RemainingTime = 0f,
                        TotalGameTime = 0f,
                        CurrentWave = 0,
                        TotalWaves = 0,
                        ZombiesKilled = 0,
                        ZombiesReachedEnd = 0
                    };
                    em.AddComponentData(e, data);

                    UnityEngine.Debug.Log("GameBootstrap: 已创建 GameStateComponent 单例 (Preparing)");
                }
                query.Dispose();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"EnsureGameStateSingleton 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载游戏配置
        /// </summary>
        private void LoadConfiguration()
        {
            if (gameConfigJson == null)
            {
                UnityEngine.Debug.LogWarning("未指定配置文件，使用默认配置");
                _gameConfig = CreateDefaultConfiguration();
            }
            else
            {
                try
                {
                    _gameConfig = JsonUtility.FromJson<GameConfiguration>(gameConfigJson.text);
                    UnityEngine.Debug.Log($"加载配置: {_gameConfig.projectName} v{_gameConfig.version}");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"配置文件解析失败: {ex.Message}");
                    _gameConfig = CreateDefaultConfiguration();
                }
            }
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        private GameConfiguration CreateDefaultConfiguration()
        {
            return new GameConfiguration
            {
                projectName = "PVZ Game",
                projectType = "tower-defense",
                version = "1.0.0",
                modules = new List<ModuleConfig>
                {
                    new ModuleConfig { moduleId = "core.ecs", enabled = true },
                    new ModuleConfig { moduleId = "core.input", enabled = true },
                }
            };
        }

        /// <summary>
        /// 创建模块注册�?
        /// </summary>
        private void CreateModuleRegistry()
        {
            _moduleRegistry = new ModuleRegistry();

            // 设置全局配置参数
            foreach (var param in _gameConfig.globalParameters)
            {
                _moduleRegistry.SetConfigParameter(param.key, param.value);
            }
        }

        /// <summary>
        /// 创建ECS World
        /// </summary>
        private void CreateECSWorld()
        {
            // 使用默认World或创建自定义World
            _gameWorld = World.DefaultGameObjectInjectionWorld;

            if (_gameWorld == null)
            {
                UnityEngine.Debug.Log("创建新的ECS World");
                _gameWorld = new World("GameWorld");
            }

            // 始终设置默认 World，确保 MonoBehaviour 与系统使用同一个 World
            World.DefaultGameObjectInjectionWorld = _gameWorld;

            _moduleRegistry.SetWorld(_gameWorld);
            UnityEngine.Debug.Log($"ECS World 已设定为 {_gameWorld.Name}");
        }

        /// <summary>
        /// 注册所有模�?
        /// </summary>
        private void RegisterAllModules()
        {
                UnityEngine.Debug.Log("====== 开始注册模�?======");

                // 注册所有启用的模块
            foreach (var moduleConfig in _gameConfig.modules)
            {
                if (!moduleConfig.enabled)
                {
                    UnityEngine.Debug.Log($"跳过已禁用的模块: {moduleConfig.moduleId}");
                    continue;
                }

                var module = CreateModule(moduleConfig);
                if (module != null)
                {
                    _moduleRegistry.RegisterModule(module);
                }
            }
        }

        /// <summary>
        /// 根据配置创建模块实例
        /// </summary>
        private IGameModule CreateModule(ModuleConfig config)
        {
            try
            {
                // 通过反射或工厂模式创建模�?
                var moduleType = ModuleFactory.GetModuleType(config.moduleId);
                if (moduleType == null)
                {
                    UnityEngine.Debug.LogError($"未找到模块类�? {config.moduleId}");
                    return null;
                }

                var module = Activator.CreateInstance(moduleType) as IGameModule;

                // TODO: 应用自定义参�?

                return module;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"创建模块失败 {config.moduleId}: {ex.Message}");
                return null;
            }
        }

        private void Update()
        {
            if (_moduleRegistry != null)
            {
                _moduleRegistry.UpdateAllModules(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            UnityEngine.Debug.Log("====== GameBootstrap 关闭 ======");
            _moduleRegistry?.ShutdownAllModules();

            // 清理单例
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            _moduleRegistry?.ShutdownAllModules();
        }
    }

    /// <summary>
    /// 游戏配置数据结构
    /// </summary>
    [Serializable]
    public class GameConfiguration
    {
        public string projectName;
        public string projectType;
        public string version;
        public List<ModuleConfig> modules = new List<ModuleConfig>();
        public List<ConfigParameter> globalParameters = new List<ConfigParameter>();
    }

    [Serializable]
    public class ConfigParameter
    {
        public string key;
        public string value;
    }
}
