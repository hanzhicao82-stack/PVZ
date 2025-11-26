using UnityEngine;
using Unity.Entities;
using PVZ.DOTS.Components;
using PVZ.DOTS.Utils;

namespace PVZ.DOTS
{
    /// <summary>
    /// 游戏状态管理器 - 管理游戏状态切换
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        private EntityManager _entityManager;
        private static GameStateManager _instance;

        public static GameStateManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameStateManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("GameStateManager");
                        _instance = go.AddComponent<GameStateManager>();
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            InitializeEntityManager();
        }

        private void InitializeEntityManager()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                _entityManager = world.EntityManager;
            }
        }

        /// <summary>
        /// 设置游戏状态为Playing
        /// </summary>
        public void SetGameStatePlaying()
        {
            SetGameState(GameState.Playing);
        }

        /// <summary>
        /// 设置游戏状态为Preparing
        /// </summary>
        public void SetGameStatePreparing()
        {
            SetGameState(GameState.Preparing);
        }

        /// <summary>
        /// 设置游戏状态为Victory
        /// </summary>
        public void SetGameStateVictory()
        {
            SetGameState(GameState.Victory);
        }

        /// <summary>
        /// 设置游戏状态为Defeat
        /// </summary>
        public void SetGameStateDefeat()
        {
            SetGameState(GameState.Defeat);
        }

        /// <summary>
        /// 设置游戏状态
        /// </summary>
        public void SetGameState(GameState newState)
        {
              GameLogger.LogWarning("GameStateManager", "SetState " + newState);
            if (_entityManager == null)
            {
                InitializeEntityManager();
                if (_entityManager == null)
                {
                    GameLogger.LogWarning("GameStateManager", "EntityManager未初始化");
                    return;
                }
            }

            var query = _entityManager.CreateEntityQuery(typeof(GameStateComponent));
            if (query.TryGetSingleton<GameStateComponent>(out var gameState))
            {
                var entity = query.GetSingletonEntity();
                var oldState = gameState.CurrentState;
                gameState.CurrentState = newState;
                _entityManager.SetComponentData(entity, gameState);
                GameLogger.Log("GameStateManager", $"游戏状态切换: {oldState} -> {newState}");
            }
            else
            {
                GameLogger.LogWarning("GameStateManager", "未找到GameStateComponent");
            }
            query.Dispose();
        }

        /// <summary>
        /// 获取当前游戏状态
        /// </summary>
        public bool TryGetGameState(out GameStateComponent gameState)
        {
            gameState = default;
            
            if (_entityManager == null)
            {
                InitializeEntityManager();
                if (_entityManager == null)
                    return false;
            }

            var query = _entityManager.CreateEntityQuery(typeof(GameStateComponent));
            bool result = query.TryGetSingleton<GameStateComponent>(out gameState);
            query.Dispose();
            return result;
        }

        /// <summary>
        /// 获取当前游戏状态（简化版本）
        /// </summary>
        public GameState GetCurrentState()
        {
            if (TryGetGameState(out var gameState))
            {
                return gameState.CurrentState;
            }
            return GameState.Preparing;
        }

        /// <summary>
        /// 重置游戏状态到初始状态
        /// </summary>
        public void ResetGameState()
        {
            if (_entityManager == null)
            {
                InitializeEntityManager();
                if (_entityManager == null)
                {
                    GameLogger.LogWarning("GameStateManager", "EntityManager未初始化");
                    return;
                }
            }

            var query = _entityManager.CreateEntityQuery(typeof(GameStateComponent));
            if (query.TryGetSingleton<GameStateComponent>(out var gameState))
            {
                var entity = query.GetSingletonEntity();
                gameState.CurrentState = GameState.Preparing;
                gameState.ZombiesReachedEnd = 0;
                gameState.ZombiesKilled = 0;
                gameState.CurrentWave = 0;
                _entityManager.SetComponentData(entity, gameState);
                GameLogger.Log("GameStateManager", "游戏状态已重置");
            }
            query.Dispose();
        }
    }
}
