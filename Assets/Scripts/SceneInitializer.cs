using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using Debug = UnityEngine.Debug;

namespace PVZ.DOTS
{
    /// <summary>
    /// 场景初始化脚本 - 自动创建游戏所需的运行时组件
    /// 在Unity编辑器中：右键场景中的Hierarchy > Create Empty，命名为"GameManager"，添加此脚本
    /// </summary>
    public class SceneInitializer : MonoBehaviour
    {
        [Header("自动创建运行时组件")]
        [Tooltip("是否在场景加载时自动创建UI")]
        public bool autoCreateUI = true;

        [Tooltip("是否在场景加载时自动创建配置加载器")]
        public bool autoCreateConfigLoader = true;

        [Tooltip("是否自动加载关卡配置")]
        public bool autoLoadLevel = true;

        [Header("配置引用")]
        public TextAsset gameConfigJson;

        [Header("关卡设置1")]
        public TextAsset levelConfigJson;

        [Header("关卡设置2")]
        public int startLevelId = 1;

        [Header("调试工具")]
        [Tooltip("是否自动创建地图网格调试工具")]
        public bool autoCreateMapGridDebugger = true;

        [Tooltip("是否自动创建实体位置调试工具")]
        public bool autoCreateEntityDebugger = true;

        [Header("地图配置")]
        [Tooltip("地图偏移位置（用于居中显示）")]
        public Vector3 mapOffset = new Vector3(-4.5f, 0, 0);

        [Tooltip("是否在加载关卡后自动调整地图偏移")]
        public bool autoAdjustMapOffset = true;

        void Awake()
        {
            UnityEngine.Debug.Log("SceneInitializer: 开始初始化场景...");

            // 创建配置加载器
            if (autoCreateConfigLoader)
            {
                CreateConfigLoader();
            }

            // 创建关卡配置加载器
            if (autoLoadLevel && levelConfigJson != null)
            {
                CreateLevelConfigLoader();
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

            // 延迟调整地图偏移（等待关卡加载完成）
            if (autoLoadLevel && autoAdjustMapOffset && levelConfigJson != null)
            {
                StartCoroutine(AdjustMapOffsetAfterLevelLoad());
            }

            UnityEngine.Debug.Log("SceneInitializer: 场景初始化完成！");
        }

        private void CreateConfigLoader()
        {
            var configLoaderObj = GameObject.Find("GameConfigLoader");
            if (configLoaderObj == null)
            {
                configLoaderObj = new GameObject("GameConfigLoader");
                var loader = configLoaderObj.AddComponent<Config.GameConfigLoader>();
                loader.configJson = gameConfigJson;
                UnityEngine.Debug.Log("SceneInitializer: 创建 GameConfigLoader");
            }
        }

        private void CreateLevelConfigLoader()
        {
            var levelLoaderObj = GameObject.Find("LevelConfigLoader");
            if (levelLoaderObj == null)
            {
                levelLoaderObj = new GameObject("LevelConfigLoader");
                var levelLoader = levelLoaderObj.AddComponent<Config.LevelConfigLoader>();
                levelLoader.levelConfigJson = levelConfigJson;
                levelLoader.loadOnStart = true;
                levelLoader.levelToLoad = startLevelId;
                UnityEngine.Debug.Log($"SceneInitializer: 创建 LevelConfigLoader (关卡ID={startLevelId})");
            }
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

                UnityEngine.Debug.Log("SceneInitializer: 创建 Canvas");
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

                UnityEngine.Debug.Log("SceneInitializer: 创建 GameUI 完成");
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
                debugger.mapOffset = mapOffset;
                UnityEngine.Debug.Log("SceneInitializer: 创建 MapGridDebugger");
            }
        }

        private System.Collections.IEnumerator AdjustMapOffsetAfterLevelLoad()
        {
            // 等待1秒确保关卡加载完成
            yield return new WaitForSeconds(1f);

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                UnityEngine.Debug.LogWarning("SceneInitializer: World未创建，无法调整地图偏移");
                yield break;
            }

            var entityManager = world.EntityManager;
            var query = entityManager.CreateEntityQuery(typeof(Components.LevelConfigComponent));

            if (!query.IsEmptyIgnoreFilter)
            {
                var levelEntity = query.GetSingletonEntity();
                var levelConfig = entityManager.GetComponentData<Components.LevelConfigComponent>(levelEntity);

                // 根据关卡配置自动计算地图偏移（居中显示）
                float totalWidth = levelConfig.ColumnCount * levelConfig.CellWidth;
                mapOffset = new Vector3(-totalWidth * 0.5f, 0, 0);

                // 更新MapGridDebugger的偏移
                var debuggerObj = GameObject.Find("MapGridDebugger");
                if (debuggerObj != null)
                {
                    var debugger = debuggerObj.GetComponent<PVZ.DOTS.Debug.MapGridDebugDrawer>();
                    if (debugger != null)
                    {
                        debugger.mapOffset = mapOffset;
                        UnityEngine.Debug.Log($"SceneInitializer: 自动调整地图偏移为 {mapOffset}（列数={levelConfig.ColumnCount}, 格子宽度={levelConfig.CellWidth}）");
                    }
                }
            }

            query.Dispose();
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
                UnityEngine.Debug.Log("SceneInitializer: 创建 EntityDebugger");
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
                var initializer = gameManagerObj.AddComponent<SceneInitializer>();

                // 尝试加载配置文件
                var configPath = "Assets/Configs/GameConfig.json";
                var levelConfigPath = "Assets/Configs/LevelConfig.json";
                initializer.gameConfigJson = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(configPath);
                initializer.levelConfigJson = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(levelConfigPath);
                initializer.autoLoadLevel = true;
                initializer.startLevelId = 1;
                initializer.autoAdjustMapOffset = true;

                UnityEditor.Selection.activeGameObject = gameManagerObj;
                UnityEngine.Debug.Log("已创建GameManager并添加SceneInitializer。已自动配置游戏和关卡配置文件。");
            }
            else
            {
                UnityEngine.Debug.Log("GameManager已存在。");
            }
        }
#endif
    }
}
