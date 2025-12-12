using UnityEngine;
using UnityEngine.UI;
using Framework;
using PVZ;
using Common;

namespace Game
{
    /// <summary>
    /// 娓告垙涓诲叆鍙?- 鑷姩鍒涘缓娓告垙鎵€闇€鐨勮繍琛屾椂缁勪欢
    /// 鍦║nity缂栬緫鍣ㄤ腑锛氬彸閿満鏅腑鐨凥ierarchy > Create Empty锛屽懡鍚嶄负"GameManager"锛屾坊鍔犳鑴氭湰
    /// </summary>
    public class GameMain : MonoBehaviour
    {
        [Header("UI 设置")]
        [Tooltip("是否在场景中添加时自动创建UI")]
        public bool autoCreateUI = true;

        [Tooltip("是否使用GameLoader加载配置")]
        public bool useGameLoader = true;

        [Header("配置资源")]
        public TextAsset gameConfigJson;

        [Header("加载配置")]
        public TextAsset levelConfigJson;
        public int startLevelId = 1;

        [Header("调试器")]
        [Tooltip("是否自动创建地图格子调试器")]
        public bool autoCreateMapGridDebugger = true;

        [Tooltip("是否自动创建实体调试器")]
        public bool autoCreateEntityDebugger = true;

        [Tooltip("是否在加载相关配置后自动调整地图偏移")]
        public bool autoAdjustMapOffset = true;

        private Vector3 mapOffset = Vector3.zero;
        private GameLoader _gameLoader;

        void Awake()
        {
            UnityEngine.Debug.Log("=== GameMain: Awake 寮€锟?===");
            GameLogger.Log("GameMain", "寮€濮嬪垵濮嬪寲鍦烘櫙...");

            // 浣跨敤GameLoader鍔犺浇閰嶇疆
            if (useGameLoader)
            {
                CreateGameLoader();
            }

            // 鍒涘缓UI绯荤粺
            if (autoCreateUI)
            {
                CreateGameUI();
            }

            // 鍒涘缓璋冭瘯宸ュ叿
            if (autoCreateMapGridDebugger)
            {
                CreateMapGridDebugger();
            }

            if (autoCreateEntityDebugger)
            {
                CreateEntityDebugger();
            }

            GameLogger.Log("GameMain", "场景初始化完成");
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

                // 娉ㄥ唽鍥炶皟
                _gameLoader.OnLoadComplete += OnLoadComplete;
                _gameLoader.OnLevelConfigLoaded += OnLevelConfigLoaded;

                // 寮€濮嬪姞锟?
                _gameLoader.StartLoad();

                GameLogger.Log("GameMain", $"鍒涘缓 GameLoader (鍏冲崱ID={startLevelId})");
            }
        }

        private void OnLoadComplete()
        {
            GameLogger.Log("GameMain", "GameLoader 鍔犺浇瀹屾垚");

            // 璋冩暣鍦板浘鍋忕Щ
            if (autoAdjustMapOffset)
            {
                AdjustMapOffset();
            }

            // 璁剧疆娓告垙鐘舵€佷负 Playing
            GameStateManager.Instance.SetGameStatePlaying();
            GameLogger.Log("GameMain", "娓告垙鐘舵€佸凡璁剧疆锟?Playing");
        }

        private void OnLevelConfigLoaded(LevelConfigComponent levelConfig)
        {
            GameLogger.Log("GameMain", $"关卡配置已加载：{levelConfig.RowCount}行 × {levelConfig.ColumnCount}列");
        }

        private void CreateGameUI()
        {
            // 妫€鏌ユ槸鍚﹀凡鏈塁anvas
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                // 鍒涘缓Canvas
                var canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();

                GameLogger.Log("GameMain", "鍒涘缓 Canvas");
            }

            // 鍒涘缓GameUI绠＄悊锟?
            var gameUIObj = GameObject.Find("GameUI");
            if (gameUIObj == null)
            {
                gameUIObj = new GameObject("GameUI");
                gameUIObj.transform.SetParent(canvas.transform, false);

                var uiManager = gameUIObj.AddComponent<GameUIManager>();

                // 鍒涘缓椤堕儴闈㈡澘
                var topPanel = CreateTopPanel(gameUIObj.transform);
                uiManager.timerText = topPanel.Find("TimerText")?.GetComponent<Text>();
                uiManager.waveText = topPanel.Find("WaveText")?.GetComponent<Text>();
                uiManager.zombiesKilledText = topPanel.Find("ZombiesKilledText")?.GetComponent<Text>();

                // 鍒涘缓涓績鐘舵€佹枃锟?
                var stateTextObj = CreateCenteredText("StateText", gameUIObj.transform, "鍑嗗锟?..", 48);
                uiManager.stateText = stateTextObj.GetComponent<Text>();

                // 鍒涘缓鑳滃埄闈㈡澘
                uiManager.victoryPanel = CreateResultPanel("VictoryPanel", gameUIObj.transform, "胜利！", Color.cyan, uiManager);

                // 鍒涘缓澶辫触闈㈡澘
                uiManager.defeatPanel = CreateResultPanel("DefeatPanel", gameUIObj.transform, "失败", Color.red, uiManager);

                GameLogger.Log("GameMain", "鍒涘缓 GameUI 瀹屾垚");
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

            // Timer Text (鍙充笂)
            var timerObj = CreateText("TimerText", topPanelObj.transform, "鏃堕棿: 03:00", 32);
            var timerRect = timerObj.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(1, 0.5f);
            timerRect.anchorMax = new Vector2(1, 0.5f);
            timerRect.pivot = new Vector2(1, 0.5f);
            timerRect.anchoredPosition = new Vector2(-20, 0);
            timerRect.sizeDelta = new Vector2(200, 50);
            timerObj.GetComponent<Text>().alignment = TextAnchor.MiddleRight;

            // Wave Text (宸︿笂)
            var waveObj = CreateText("WaveText", topPanelObj.transform, "娉㈡: 0/5", 28);
            var waveRect = waveObj.GetComponent<RectTransform>();
            waveRect.anchorMin = new Vector2(0, 0.5f);
            waveRect.anchorMax = new Vector2(0, 0.5f);
            waveRect.pivot = new Vector2(0, 0.5f);
            waveRect.anchoredPosition = new Vector2(20, 0);
            waveRect.sizeDelta = new Vector2(200, 50);

            // Zombies Killed Text (涓笂)
            var killedObj = CreateText("ZombiesKilledText", topPanelObj.transform, "鍑绘潃: 0", 28);
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

            // 缁撴灉鏂囨湰
            var textObj = CreateCenteredText("ResultText", panelObj.transform, resultText, 72);
            textObj.GetComponent<Text>().color = textColor;
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(0, 100);

            // 閲嶆柊寮€濮嬫寜锟?
            var restartBtn = CreateButton("RestartButton", panelObj.transform, "重新开始", new Vector2(0, -50));
            restartBtn.GetComponent<Button>().onClick.AddListener(uiManager.OnRestartGame);

            // 閫€鍑烘寜閽?
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

            // 鎸夐挳鏂囨湰
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
                var debugger = debuggerObj.AddComponent<global::Debug.MapGridDebugDrawer>();
                debugger.enableGridDrawing = true;
                debugger.showCellFill = true;
                debugger.showRowColumnIndex = true;
                GameLogger.Log("GameMain", "创建MapGridDebugger");
            }
        }

        private void AdjustMapOffset()
        {
            if (_gameLoader == null || !_gameLoader.TryGetLevelConfig(out var levelConfig))
            {
                GameLogger.LogWarning("GameMain", "无法获取关卡配置，跳过地图偏移调整");        
                return;
            }   

            // 鏍规嵁鍏冲崱閰嶇疆鑷姩璁＄畻鍦板浘鍋忕Щ锛堝眳涓樉绀猴級
            float totalWidth = levelConfig.ColumnCount * levelConfig.CellWidth;
            mapOffset = new Vector3(-totalWidth * 0.5f, 0, 0);
        }

        private void CreateEntityDebugger()
        {
            var debuggerObj = GameObject.Find("EntityDebugger");
            if (debuggerObj == null)
            {
                debuggerObj = new GameObject("EntityDebugger");
                var debugger = debuggerObj.AddComponent<global::Debug.EntityPositionDebugDrawer>();
                debugger.showPlants = true;
                debugger.showZombies = true;
                debugger.showProjectiles = true;
                GameLogger.Log("GameMain", "创建EntityDebugger");
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

                // 灏濊瘯鍔犺浇閰嶇疆鏂囦欢
                var configPath = "Assets/Configs/GameConfig.json";
                var levelConfigPath = "Assets/Configs/LevelConfig.json";
                initializer.gameConfigJson = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(configPath);
                initializer.levelConfigJson = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(levelConfigPath);
                initializer.useGameLoader = true;
                initializer.startLevelId = 1;
                initializer.autoAdjustMapOffset = true;

                UnityEditor.Selection.activeGameObject = gameManagerObj;
                GameLogger.Log("GameMain", "创建了GameManager对象并添加了GameMain组件。自动加载配置文件并设置了相关参数。");
            }
            else
            {
                GameLogger.Log("GameMain", "GameManager对象已存在。");
            }
        }
#endif
    }
}
