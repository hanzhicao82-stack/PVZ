using System;
using System.Collections;
using UnityEngine;
using Unity.Entities;
using PVZ.DOTS.Utils;

namespace PVZ.DOTS
{
    /// <summary>
    /// 游戏加载器 - 统一管理配置和组件加载
    /// </summary>
    public class GameLoader : MonoBehaviour
    {
        [Header("配置文件")]
        [Tooltip("游戏全局配置")]
        public TextAsset gameConfigJson;
        
        [Tooltip("关卡配置文件")]
        public TextAsset levelConfigJson;
        
        [Header("加载选项")]
        [Tooltip("要加载的关卡ID")]
        public int levelToLoad = 1;
        
        [Tooltip("是否自动设置游戏状态为Playing")]
        public bool autoSetGamePlaying = true;
        
        [Tooltip("加载后延迟多少秒设置Playing状态")]
        public float playingStateDelay = 0.5f;

        private EntityManager _entityManager;
        private bool _isLoading = false;
        private bool _loadComplete = false;

        /// <summary>
        /// 加载完成回调
        /// </summary>
        public event Action OnLoadComplete;

        /// <summary>
        /// 关卡配置加载完成回调
        /// </summary>
        public event Action<Components.LevelConfigComponent> OnLevelConfigLoaded;

        /// <summary>
        /// 游戏配置加载完成回调
        /// </summary>
        public event Action<Components.GameConfigComponent> OnGameConfigLoaded;

        public bool IsLoading => _isLoading;
        public bool IsLoadComplete => _loadComplete;

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                _entityManager = world.EntityManager;
            }
        }

        /// <summary>
        /// 开始加载游戏配置和关卡
        /// </summary>
        public void StartLoad()
        {
            UnityEngine.Debug.Log("=== GameLoader: StartLoad 被调用 ===");
            
            if (_isLoading)
            {
                GameLogger.LogWarning("GameLoader", "加载已在进行中");
                UnityEngine.Debug.LogWarning("GameLoader: 已经在加载中，跳过");
                return;
            }

            UnityEngine.Debug.Log("GameLoader: 启动协程 LoadSequence");
            StartCoroutine(LoadSequence());
        }

        /// <summary>
        /// 加载指定关卡
        /// </summary>
        public void LoadLevel(int levelId)
        {
            levelToLoad = levelId;
            StartLoad();
        }

        private IEnumerator LoadSequence()
        {
            _isLoading = true;
            _loadComplete = false;

            UnityEngine.Debug.Log("=== GameLoader: LoadSequence 协程开始 ===");
            GameLogger.Log("GameLoader", "开始加载流程...");

            // 等待EntityManager初始化
            while (_entityManager == null)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world != null)
                {
                    _entityManager = world.EntityManager;
                    UnityEngine.Debug.Log("GameLoader: EntityManager 已获取");
                }
                yield return null;
            }

            // 1. 加载游戏配置
            if (gameConfigJson != null)
            {
                UnityEngine.Debug.Log("GameLoader: 开始加载游戏配置...");
                yield return LoadGameConfig();
            }
            else
            {
                UnityEngine.Debug.LogWarning("GameLoader: gameConfigJson 为 null，跳过游戏配置加载");
            }

            // 2. 加载关卡配置
            if (levelConfigJson != null)
            {
                UnityEngine.Debug.Log("GameLoader: 开始加载关卡配置...");
                yield return LoadLevelConfig();
            }
            else
            {
                UnityEngine.Debug.LogWarning("GameLoader: levelConfigJson 为 null，跳过关卡配置加载");
            }

            // 3. 等待配置实体创建完成
            UnityEngine.Debug.Log("GameLoader: 等待配置实体创建...");
            yield return new WaitForSeconds(0.1f);

            // 4. 触发回调
            UnityEngine.Debug.Log("GameLoader: 准备触发回调...");
            if (OnLevelConfigLoaded != null)
            {
                var query = _entityManager.CreateEntityQuery(typeof(Components.LevelConfigComponent));
                if (query.TryGetSingleton<Components.LevelConfigComponent>(out var levelConfig))
                {
                    UnityEngine.Debug.Log("GameLoader: 触发 OnLevelConfigLoaded 回调");
                    OnLevelConfigLoaded?.Invoke(levelConfig);
                }
                else
                {
                    UnityEngine.Debug.LogWarning("GameLoader: 未找到 LevelConfigComponent");
                }
                query.Dispose();
            }

            if (OnGameConfigLoaded != null)
            {
                var query = _entityManager.CreateEntityQuery(typeof(Components.GameConfigComponent));
                if (query.TryGetSingleton<Components.GameConfigComponent>(out var gameConfig))
                {
                    UnityEngine.Debug.Log("GameLoader: 触发 OnGameConfigLoaded 回调");
                    OnGameConfigLoaded?.Invoke(gameConfig);
                }
                else
                {
                    UnityEngine.Debug.LogWarning("GameLoader: 未找到 GameConfigComponent");
                }
                query.Dispose();
            }

            // 5. 设置游戏状态
            if (autoSetGamePlaying)
            {
                UnityEngine.Debug.Log($"GameLoader: 等待 {playingStateDelay} 秒后设置 Playing 状态");
                yield return new WaitForSeconds(playingStateDelay);
                GameStateManager.Instance.SetGameStatePlaying();
            }

            _isLoading = false;
            _loadComplete = true;

            UnityEngine.Debug.Log("GameLoader: 触发 OnLoadComplete 回调");
            GameLogger.Log("GameLoader", "加载流程完成！");
            OnLoadComplete?.Invoke();
            UnityEngine.Debug.Log("=== GameLoader: LoadSequence 协程结束 ===");
        }

        private IEnumerator LoadGameConfig()
        {
            GameLogger.Log("GameLoader", "加载游戏配置...");

            var loader = FindObjectOfType<Config.GameConfigLoader>();
            if (loader == null)
            {
                var loaderObj = new GameObject("GameConfigLoader");
                loader = loaderObj.AddComponent<Config.GameConfigLoader>();
            }

            loader.configJson = gameConfigJson;
            // GameConfigLoader会在Start时自动加载

            yield return new WaitForSeconds(0.1f);
            GameLogger.Log("GameLoader", "游戏配置加载完成");
        }

        private IEnumerator LoadLevelConfig()
        {
            GameLogger.Log("GameLoader", $"加载关卡配置 (Level {levelToLoad})...");

            var loader = FindObjectOfType<Config.LevelConfigLoader>();
            if (loader == null)
            {
                var loaderObj = new GameObject("LevelConfigLoader");
                loader = loaderObj.AddComponent<Config.LevelConfigLoader>();
            }

            loader.levelConfigJson = levelConfigJson;
            loader.levelToLoad = levelToLoad;
            loader.loadOnStart = false; // 手动控制加载
            loader.LoadLevel(levelToLoad);

            yield return new WaitForSeconds(0.1f);
            GameLogger.Log("GameLoader", "关卡配置加载完成");
        }

        /// <summary>
        /// 获取当前加载的关卡配置
        /// </summary>
        public bool TryGetLevelConfig(out Components.LevelConfigComponent config)
        {
            config = default;
            if (_entityManager == null)
                return false;

            var query = _entityManager.CreateEntityQuery(typeof(Components.LevelConfigComponent));
            bool result = query.TryGetSingleton<Components.LevelConfigComponent>(out config);
            query.Dispose();
            return result;
        }

        /// <summary>
        /// 获取当前游戏配置
        /// </summary>
        public bool TryGetGameConfig(out Components.GameConfigComponent config)
        {
            config = default;
            if (_entityManager == null)
                return false;

            var query = _entityManager.CreateEntityQuery(typeof(Components.GameConfigComponent));
            bool result = query.TryGetSingleton<Components.GameConfigComponent>(out config);
            query.Dispose();
            return result;
        }
    }
}
