using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Common
{
    /// <summary>
    /// 血条管理器 - 管理所有实体的血�?UI
    /// </summary>
    public class HealthBarManager : MonoBehaviour
    {
        private static HealthBarManager _instance;
        public static HealthBarManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 先查找场景中是否已存�?
                    _instance = FindObjectOfType<HealthBarManager>();

                    if (_instance == null)
                    {
                        var go = new GameObject("HealthBarManager");
                        _instance = go.AddComponent<HealthBarManager>();
                        DontDestroyOnLoad(go);
                        UnityEngine.Debug.Log("[HealthBarManager] Created new instance via singleton");
                    }
                    else
                    {
                        UnityEngine.Debug.Log("[HealthBarManager] Found existing instance in scene");
                    }
                }
                return _instance;
            }
        }

        [Header("血条预制体")]
        public GameObject healthBarPrefab;

        [Header("血条配")]
        public float defaultWidth = 50f;
        public float defaultHeight = 5f;
        public float defaultYOffset = 2f;
        public bool alwaysShowHealthBar = false;
        public float lowHealthThreshold = 0.3f;

        [Header("颜色配置")]
        public Color fullHealthColor = Color.green;
        public Color lowHealthColor = Color.red;
        public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        private Canvas _canvas;
        private Camera _mainCamera;
        public Dictionary<int, GameObject> _healthBars = new Dictionary<int, GameObject>(); // Public for HealthBarSystem access
        private Transform _healthBarContainer;
        private bool _initialized = false;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            UnityEngine.Debug.Log("[HealthBarManager] Awake - Waiting for external Canvas setup");
        }

        void Start()
        {
            _mainCamera = Camera.main;
            UnityEngine.Debug.Log($"[HealthBarManager] Start - Camera: {(_mainCamera != null ? "Found" : "NULL")}");
        }

        /// <summary>
        /// 设置 Canvas 和血条容器（由外部调用，�?PerformanceTestSpawner�?
        /// </summary>
        public void SetCanvas(Canvas canvas, Transform healthBarContainer)
        {
            if (canvas == null || healthBarContainer == null)
            {
                UnityEngine.Debug.LogError("[HealthBarManager] SetCanvas - Canvas or Container is null!");
                return;
            }

            _canvas = canvas;
            _healthBarContainer = healthBarContainer;
            _initialized = true;
            
            CreateHealthBarPrefab();
            UnityEngine.Debug.Log($"[HealthBarManager] Canvas set externally - Container: {_healthBarContainer.name}");
        }


        private void CreateHealthBarPrefab()
        {
            if (healthBarPrefab != null)
                return;

            // 创建血条预制体
            healthBarPrefab = new GameObject("HealthBar");
            healthBarPrefab.layer = LayerMask.NameToLayer("UI");
            var rectTransform = healthBarPrefab.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(defaultWidth, defaultHeight);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

            // 背景
            var background = new GameObject("Background");
            background.layer = LayerMask.NameToLayer("UI");
            background.transform.SetParent(healthBarPrefab.transform, false);
            var bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.localScale = Vector3.one;
            var bgImage = background.AddComponent<Image>();
            bgImage.color = backgroundColor;

            // 前景（血量条�?
            var fill = new GameObject("Fill");
            fill.layer = LayerMask.NameToLayer("UI");
            fill.transform.SetParent(healthBarPrefab.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.localScale = Vector3.one;
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = fullHealthColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;

            healthBarPrefab.SetActive(false);
            UnityEngine.Debug.Log("[HealthBarManager] Health bar prefab created");
        }

        /// <summary>
        /// 创建血�?UI
        /// </summary>
        public GameObject CreateHealthBar(Entity entity, float yOffset = 0f)
        {
            if (!IsTransformValid(_healthBarContainer))
            {
                UnityEngine.Debug.LogError($"[HealthBarManager] Health bar container not initialized! Please call SetCanvas() first.");
                return null;
            }

            if (healthBarPrefab == null)
            {
                UnityEngine.Debug.LogWarning("[HealthBarManager] Health bar prefab is null, creating...");
                CreateHealthBarPrefab();
            }

            var healthBar = Instantiate(healthBarPrefab, _healthBarContainer, false);
            healthBar.layer = LayerMask.NameToLayer("UI");
            healthBar.name = $"HealthBar_{entity.Index}";
            
            var rectTransform = healthBar.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
            }
            
            healthBar.SetActive(true);
            int instanceID = healthBar.GetInstanceID();
            _healthBars[instanceID] = healthBar;

            UnityEngine.Debug.Log($"[HealthBarManager] Created health bar for entity {entity.Index}, siblings: {_healthBarContainer.childCount}");

            return healthBar;
        }        /// <summary>
        /// 更新血条位置和数值（优化版：减少 GetComponent 调用�?
        /// </summary>
        public void UpdateHealthBar(GameObject healthBar, float3 worldPosition, float yOffset, float currentHealth, float maxHealth)
        {
            if (healthBar == null || _mainCamera == null)
                return;

            // 转换世界坐标到屏幕坐�?
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(new Vector3(worldPosition.x, worldPosition.y + yOffset, worldPosition.z));

            // 检查是否在屏幕后方
            if (screenPos.z < 0)
            {
                healthBar.SetActive(false);
                return;
            }

            // 确保激�?
            if (!healthBar.activeSelf)
            {
                healthBar.SetActive(true);
            }

            // 缓存组件引用（减少重�?GetComponent 调用�?
            var rectTransform = healthBar.GetComponent<RectTransform>();
            if (rectTransform != null && rectTransform.position != screenPos)
            {
                rectTransform.position = screenPos;
            }

            // 更新血量显�?
            float healthPercentage = math.clamp(currentHealth / maxHealth, 0f, 1f);
            var fillImage = healthBar.transform.Find("Fill")?.GetComponent<Image>();
            if (fillImage != null)
            {
                // 只在值变化时更新
                if (math.abs(fillImage.fillAmount - healthPercentage) > 0.001f)
                {
                    fillImage.fillAmount = healthPercentage;
                }

                // 根据血量百分比改变颜色
                Color targetColor;
                if (healthPercentage <= lowHealthThreshold)
                {
                    targetColor = lowHealthColor;
                }
                else
                {
                    targetColor = Color.Lerp(lowHealthColor, fullHealthColor, (healthPercentage - lowHealthThreshold) / (1f - lowHealthThreshold));
                }

                if (fillImage.color != targetColor)
                {
                    fillImage.color = targetColor;
                }
            }

            // 如果不是始终显示，则根据血量决定是否显�?
            if (!alwaysShowHealthBar && healthPercentage >= 1f)
            {
                healthBar.SetActive(false);
            }
        }

        /// <summary>
        /// 销毁血�?UI
        /// </summary>
        public void DestroyHealthBar(int instanceID)
        {
            if (_healthBars.TryGetValue(instanceID, out var healthBar))
            {
                if (healthBar != null)
                {
                    Destroy(healthBar);
                }
                _healthBars.Remove(instanceID);
            }
        }

        /// <summary>
        /// 通过 GameObject 引用销毁血�?
        /// </summary>
        public void DestroyHealthBar(GameObject healthBar)
        {
            if (healthBar != null)
            {
                int instanceID = healthBar.GetInstanceID();
                DestroyHealthBar(instanceID);
            }
        }

        /// <summary>
        /// 清理所有血�?
        /// </summary>
        public void ClearAllHealthBars()
        {
            foreach (var healthBar in _healthBars.Values)
            {
                if (healthBar != null)
                {
                    Destroy(healthBar);
                }
            }
            _healthBars.Clear();
        }

        void OnDestroy()
        {
            ClearAllHealthBars();
        }

        /// <summary>
        /// 确保管理器已初始�?
        /// </summary>
        public void EnsureInitialized()
        {
            UnityEngine.Debug.Log($"[HealthBarManager] EnsureInitialized - Initialized: {_initialized}, Container valid: {IsTransformValid(_healthBarContainer)}");
            
            // 安全检�?Container 是否有效
            if (!IsTransformValid(_healthBarContainer))
            {
                UnityEngine.Debug.LogError("[HealthBarManager] EnsureInitialized - Container not set! Please call SetCanvas() first.");
            }

            if (healthBarPrefab == null)
            {
                UnityEngine.Debug.LogWarning("[HealthBarManager] EnsureInitialized - Prefab was null, recreating");
                CreateHealthBarPrefab();
            }

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
        }

        /// <summary>
        /// 安全检�?Transform 是否有效
        /// </summary>
        private bool IsTransformValid(Transform t)
        {
            if (t == null)
            {
                return false;
            }
            
            try
            {
                // 尝试访问 gameObject，如果已销毁会抛出异常
                var go = t.gameObject;
                return go != null;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}
