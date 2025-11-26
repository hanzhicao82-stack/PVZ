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
        public bool autoSetGamePlaying = false;
        
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
            if (_isLoading)
            {
                GameLogger.LogWarning("GameLoader", "加载已在进行中");
                return;
            }

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

            GameLogger.Log("GameLoader", "开始加载流程...");

            // 等待EntityManager初始化
            while (_entityManager == null)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world != null)
                {
                    _entityManager = world.EntityManager;
                }
                yield return null;
            }

            // 1. 加载游戏配置
            if (gameConfigJson != null)
            {
                yield return LoadGameConfig();
            }

            // 2. 加载关卡配置
            if (levelConfigJson != null)
            {
                yield return LoadLevelConfig();
            }

            // 3. 等待配置实体创建完成
            yield return new WaitForSeconds(0.1f);

            // 4. 触发回调
            if (OnLevelConfigLoaded != null)
            {
                var query = _entityManager.CreateEntityQuery(typeof(Components.LevelConfigComponent));
                if (query.TryGetSingleton<Components.LevelConfigComponent>(out var levelConfig))
                {
                    OnLevelConfigLoaded?.Invoke(levelConfig);
                }
                query.Dispose();
            }

            if (OnGameConfigLoaded != null)
            {
                var query = _entityManager.CreateEntityQuery(typeof(Components.GameConfigComponent));
                if (query.TryGetSingleton<Components.GameConfigComponent>(out var gameConfig))
                {
                    OnGameConfigLoaded?.Invoke(gameConfig);
                }
                query.Dispose();
            }

            // 5. 设置游戏状态
            if (autoSetGamePlaying)
            {
                yield return new WaitForSeconds(playingStateDelay);
                SetGameStatePlaying();
            }

            _isLoading = false;
            _loadComplete = true;

            GameLogger.Log("GameLoader", "加载流程完成！");
            OnLoadComplete?.Invoke();
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

        private void SetGameStatePlaying()
        {
            if (_entityManager == null)
                return;

            var query = _entityManager.CreateEntityQuery(typeof(Components.GameStateComponent));
            if (query.TryGetSingleton<Components.GameStateComponent>(out var gameState))
            {
                var entity = query.GetSingletonEntity();
                gameState.CurrentState = Components.GameState.Playing;
                _entityManager.SetComponentData(entity, gameState);
                GameLogger.Log("GameLoader", "游戏状态设置为Playing");
            }
            else
            {
                GameLogger.LogWarning("GameLoader", "未找到GameStateComponent");
            }
            query.Dispose();
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
