using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using PVZ.DOTS.Utils;
using Debug = UnityEngine.Debug;

namespace PVZ.DOTS
{
    /// <summary>
    /// 游戏主入口 - 自动创建游戏所需的运行时组件
    /// 在Unity编辑器中：右键场景中的Hierarchy > Create Empty，命名为"GameManager"，添加此脚本
    /// </summary>
    public class GameMain : MonoBehaviour
    {
        [Header("自动创建运行时组件")]
        [Tooltip("是否在场景加载时自动创建UI")]
        public bool autoCreateUI = true;

        [Tooltip("是否使用GameLoader加载配置")]
        public bool useGameLoader = true;

        [Header("配置引用")]
        public TextAsset gameConfigJson;

        [Header("关卡设置")]
        public TextAsset levelConfigJson;
        public int startLevelId = 1;

        [Header("调试工具")]
        [Tooltip("是否自动创建地图网格调试工具")]
        public bool autoCreateMapGridDebugger = true;

        [Tooltip("是否自动创建实体位置调试工具")]
        public bool autoCreateEntityDebugger = true;

        [Tooltip("是否在加载关卡后自动调整地图偏移")]
        public bool autoAdjustMapOffset = true;

        private Vector3 mapOffset = Vector3.zero;
        private GameLoader _gameLoader;

        void Awake()
        {
            UnityEngine.Debug.Log("=== GameMain: Awake 开始 ===");
            GameLogger.Log("GameMain", "开始初始化场景...");

            // 使用GameLoader加载配置
            if (useGameLoader)
            {
                CreateGameLoader();
            }

            // 创建UI系统
            if (autoCreateUI)
            {
                CreateGameUI();
            }

            // 创建调试工具
            if (autoCreateMapGridDebugger)
            {
                CreateMapGridDebugger();
            }

            if (autoCreateEntityDebugger)
            {
                CreateEntityDebugger();
            }

            GameLogger.Log("GameMain", "场景初始化完成！");
            UnityEngine.Debug.Log("=== GameMain: Awake 结束 ===");
        }

        private void CreateGameLoader()
        {
            var loaderObj = GameObject.Find("GameLoader");
            if (loaderObj == null)
            {
                loaderObj = new GameObject("GameLoader");
                _gameLoader = loaderObj.AddComponent<GameLoader>();
            }
            else
            {
                _gameLoader = loaderObj.GetComponent<GameLoader>();
            }

            if (_gameLoader != null)
            {
                _gameLoader.gameConfigJson = gameConfigJson;
                _gameLoader.levelConfigJson = levelConfigJson;
                _gameLoader.levelToLoad = startLevelId;

                // 注册回调
                _gameLoader.OnLoadComplete += OnLoadComplete;
                _gameLoader.OnLevelConfigLoaded += OnLevelConfigLoaded;

                // 开始加载
                _gameLoader.StartLoad();

                GameLogger.Log("GameMain", $"创建 GameLoader (关卡ID={startLevelId})");
            }
        }

        private void OnLoadComplete()
        {
            GameLogger.Log("GameMain", "GameLoader 加载完成");

            // 调整地图偏移
            if (autoAdjustMapOffset)
            {
                AdjustMapOffset();
            }

            // 设置游戏状态为 Playing
            GameStateManager.Instance.SetGameStatePlaying();
            GameLogger.Log("GameMain", "游戏状态已设置为 Playing");
        }

        private void OnLevelConfigLoaded(Components.LevelConfigComponent levelConfig)
        {
            GameLogger.Log("GameMain", $"关卡配置已加载: {levelConfig.RowCount}行 × {levelConfig.ColumnCount}列");
        }

        private void CreateGameUI()
        {
            // 检查是否已有Canvas
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                // 创建Canvas
                var canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();

                GameLogger.Log("GameMain", "创建 Canvas");
            }

            // 创建GameUI管理器
            var gameUIObj = GameObject.Find("GameUI");
            if (gameUIObj == null)
            {
                gameUIObj = new GameObject("GameUI");
                gameUIObj.transform.SetParent(canvas.transform, false);

                var uiManager = gameUIObj.AddComponent<GameUIManager>();

                // 创建顶部面板
                var topPanel = CreateTopPanel(gameUIObj.transform);
                uiManager.timerText = topPanel.Find("TimerText")?.GetComponent<Text>();
                uiManager.waveText = topPanel.Find("WaveText")?.GetComponent<Text>();
                uiManager.zombiesKilledText = topPanel.Find("ZombiesKilledText")?.GetComponent<Text>();

                // 创建中心状态文本
                var stateTextObj = CreateCenteredText("StateText", gameUIObj.transform, "准备中...", 48);
                uiManager.stateText = stateTextObj.GetComponent<Text>();

                // 创建胜利面板
                uiManager.victoryPanel = CreateResultPanel("VictoryPanel", gameUIObj.transform, "胜利！", Color.cyan, uiManager);

                // 创建失败面板
                uiManager.defeatPanel = CreateResultPanel("DefeatPanel", gameUIObj.transform, "失败", Color.red, uiManager);

                GameLogger.Log("GameMain", "创建 GameUI 完成");
            }
        }

        private Transform CreateTopPanel(Transform parent)
        {
            var topPanelObj = new GameObject("TopPanel");
            var rectTransform = topPanelObj.AddComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            rectTransform.sizeDelta = new Vector2(0, 80);
            rectTransform.anchoredPosition = Vector2.zero;

            // Timer Text (右上)
            var timerObj = CreateText("TimerText", topPanelObj.transform, "时间: 03:00", 32);
            var timerRect = timerObj.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(1, 0.5f);
            timerRect.anchorMax = new Vector2(1, 0.5f);
            timerRect.pivot = new Vector2(1, 0.5f);
            timerRect.anchoredPosition = new Vector2(-20, 0);
            timerRect.sizeDelta = new Vector2(200, 50);
            timerObj.GetComponent<Text>().alignment = TextAnchor.MiddleRight;

            // Wave Text (左上)
            var waveObj = CreateText("WaveText", topPanelObj.transform, "波次: 0/5", 28);
            var waveRect = waveObj.GetComponent<RectTransform>();
            waveRect.anchorMin = new Vector2(0, 0.5f);
            waveRect.anchorMax = new Vector2(0, 0.5f);
            waveRect.pivot = new Vector2(0, 0.5f);
            waveRect.anchoredPosition = new Vector2(20, 0);
            waveRect.sizeDelta = new Vector2(200, 50);

            // Zombies Killed Text (中上)
            var killedObj = CreateText("ZombiesKilledText", topPanelObj.transform, "击杀: 0", 28);
            var killedRect = killedObj.GetComponent<RectTransform>();
            killedRect.anchorMin = new Vector2(0.5f, 0.5f);
            killedRect.anchorMax = new Vector2(0.5f, 0.5f);
            killedRect.pivot = new Vector2(0.5f, 0.5f);
            killedRect.anchoredPosition = Vector2.zero;
            killedRect.sizeDelta = new Vector2(200, 50);
            killedObj.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

            return topPanelObj.transform;
        }

        private GameObject CreateCenteredText(string name, Transform parent, string text, int fontSize)
        {
            var textObj = CreateText(name, parent, text, fontSize);
            var rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(400, 100);
            textObj.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            return textObj;
        }

        private GameObject CreateResultPanel(string name, Transform parent, string resultText, Color textColor, GameUIManager uiManager)
        {
            var panelObj = new GameObject(name);
            var panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.SetParent(parent, false);
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            var panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);

            // 结果文本
            var textObj = CreateCenteredText("ResultText", panelObj.transform, resultText, 72);
            textObj.GetComponent<Text>().color = textColor;
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(0, 100);

            // 重新开始按钮
            var restartBtn = CreateButton("RestartButton", panelObj.transform, "重新开始", new Vector2(0, -50));
            restartBtn.GetComponent<Button>().onClick.AddListener(uiManager.OnRestartGame);

            // 退出按钮
            var quitBtn = CreateButton("QuitButton", panelObj.transform, "退出游戏", new Vector2(0, -150));
            quitBtn.GetComponent<Button>().onClick.AddListener(uiManager.OnQuitGame);

            panelObj.SetActive(false);
            return panelObj;
        }

        private GameObject CreateText(string name, Transform parent, string text, int fontSize)
        {
            var textObj = new GameObject(name);
            var rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.SetParent(parent, false);

            var textComponent = textObj.AddComponent<Text>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.alignment = TextAnchor.MiddleLeft;

            return textObj;
        }

        private GameObject CreateButton(string name, Transform parent, string text, Vector2 position)
        {
            var buttonObj = new GameObject(name);
            var rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(200, 60);
            rectTransform.anchoredPosition = position;

            var image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.6f, 0.2f);

            var button = buttonObj.AddComponent<Button>();

            // 按钮文本
            var textObj = new GameObject("Text");
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.SetParent(buttonObj.transform, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var textComponent = textObj.AddComponent<Text>();
            textComponent.text = text;
            textComponent.fontSize = 24;
            textComponent.color = Color.white;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.alignment = TextAnchor.MiddleCenter;

            return buttonObj;
        }

        private void CreateMapGridDebugger()
        {
            var debuggerObj = GameObject.Find("MapGridDebugger");
            if (debuggerObj == null)
            {
                debuggerObj = new GameObject("MapGridDebugger");
                var debugger = debuggerObj.AddComponent<PVZ.DOTS.Debug.MapGridDebugDrawer>();
                debugger.enableGridDrawing = true;
                debugger.showCellFill = true;
                debugger.showRowColumnIndex = true;
                GameLogger.Log("GameMain", "创建 MapGridDebugger");
            }
        }

        private void AdjustMapOffset()
        {
            if (_gameLoader == null || !_gameLoader.TryGetLevelConfig(out var levelConfig))
            {
                GameLogger.LogWarning("GameMain", "无法获取关卡配置，跳过地图偏移调整");
                return;
            }

            // 根据关卡配置自动计算地图偏移（居中显示）
            float totalWidth = levelConfig.ColumnCount * levelConfig.CellWidth;
            mapOffset = new Vector3(-totalWidth * 0.5f, 0, 0);

            GameLogger.Log("GameMain", $"自动调整地图偏移为 {mapOffset}（列数={levelConfig.ColumnCount}, 格子宽度={levelConfig.CellWidth}）");
        }

        private void CreateEntityDebugger()
        {
            var debuggerObj = GameObject.Find("EntityDebugger");
            if (debuggerObj == null)
            {
                debuggerObj = new GameObject("EntityDebugger");
                var debugger = debuggerObj.AddComponent<PVZ.DOTS.Debug.EntityPositionDebugDrawer>();
                debugger.showPlants = true;
                debugger.showZombies = true;
                debugger.showProjectiles = true;
                GameLogger.Log("GameMain", "创建 EntityDebugger");
            }
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("PVZ/Setup Main Scene")]
        private static void SetupMainScene()
        {
            var gameManagerObj = GameObject.Find("GameManager");
            if (gameManagerObj == null)
            {
                gameManagerObj = new GameObject("GameManager");
                var initializer = gameManagerObj.AddComponent<GameMain>();

                // 尝试加载配置文件
                var configPath = "Assets/Configs/GameConfig.json";
                var levelConfigPath = "Assets/Configs/LevelConfig.json";
                initializer.gameConfigJson = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(configPath);
                initializer.levelConfigJson = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(levelConfigPath);
                initializer.useGameLoader = true;
                initializer.startLevelId = 1;
                initializer.autoAdjustMapOffset = true;

                UnityEditor.Selection.activeGameObject = gameManagerObj;
                GameLogger.Log("GameMain", "已创建GameManager并添加GameMain。已自动配置游戏和关卡配置文件。");
            }
            else
            {
                GameLogger.Log("GameMain", "GameManager已存在。");
            }
        }
#endif
    }
}
