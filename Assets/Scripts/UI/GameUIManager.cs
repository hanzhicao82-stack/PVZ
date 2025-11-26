using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using PVZ.DOTS.Components;
using PVZ.DOTS.Utils;

namespace PVZ.DOTS
{
    public class GameUIManager : MonoBehaviour
    {
        [Header("UI References")]
        public Text timerText;
        public Text waveText;
        public Text stateText;
        public Text zombiesKilledText;
        public GameObject victoryPanel;
        public GameObject defeatPanel;

        private EntityManager _entityManager;

        void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
        }

        private bool _hasLoggedUIStart;

        void Update()
        {
            if (World.DefaultGameObjectInjectionWorld == null)
                return;

            var query = _entityManager.CreateEntityQuery(typeof(GameStateComponent));
            if (query.IsEmpty)
            {
                if (!_hasLoggedUIStart)
                {
                    GameLogger.LogWarning("GameUIManager", "找不到GameStateComponent");
                    _hasLoggedUIStart = true;
                }
                return;
            }

            var gameState = query.GetSingleton<GameStateComponent>();

            if (!_hasLoggedUIStart)
            {
                GameLogger.Log("GameUIManager", $"开始更新UI，状态={gameState.CurrentState}, 剩余时间={gameState.RemainingTime}");
                _hasLoggedUIStart = true;
            }

            // 更新倒计时
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(gameState.RemainingTime / 60f);
                int seconds = Mathf.FloorToInt(gameState.RemainingTime % 60f);
                timerText.text = $"时间: {minutes:00}:{seconds:00}";
                
                // 时间不足10秒时变红
                if (gameState.RemainingTime < 10f)
                    timerText.color = Color.red;
                else
                    timerText.color = Color.white;
            }

            // 更新波次信息
            if (waveText != null)
            {
                waveText.text = $"波次: {gameState.CurrentWave}/{gameState.TotalWaves}";
            }

            // 更新击杀数
            if (zombiesKilledText != null)
            {
                zombiesKilledText.text = $"击杀: {gameState.ZombiesKilled}";
            }

            // 更新游戏状态
            if (stateText != null)
            {
                switch (gameState.CurrentState)
                {
                    case GameState.Preparing:
                        stateText.text = "准备中...";
                        stateText.color = Color.yellow;
                        break;
                    case GameState.Playing:
                        stateText.text = "战斗中";
                        stateText.color = Color.green;
                        break;
                    case GameState.Victory:
                        stateText.text = "胜利！";
                        stateText.color = Color.cyan;
                        if (victoryPanel != null) victoryPanel.SetActive(true);
                        break;
                    case GameState.Defeat:
                        stateText.text = "失败";
                        stateText.color = Color.red;
                        if (defeatPanel != null) defeatPanel.SetActive(true);
                        break;
                }
            }

            query.Dispose();
        }

        // UI按钮调用
        public void OnStartGame()
        {
            if (_entityManager == null || World.DefaultGameObjectInjectionWorld == null)
                return;

            var query = _entityManager.CreateEntityQuery(typeof(GameStateComponent));
            if (!query.IsEmpty)
            {
                var gameState = query.GetSingleton<GameStateComponent>();
                gameState.CurrentState = GameState.Playing;
                query.SetSingleton(gameState);
            }
            query.Dispose();
        }

        public void OnRestartGame()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        public void OnQuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
