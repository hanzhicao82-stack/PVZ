using UnityEngine;
using Framework;

namespace Game
{
    /// <summary>
    /// Scene Module Initializer - Provides modular support for existing scenes
    /// Can be attached to any GameObject in a scene to enable modular startup
    /// </summary>
    public class SceneModuleInitializer : MonoBehaviour
    {
        [Header("Module Configuration")]
        [Tooltip("Module configuration file (optional, leave empty to use default)")]
        public TextAsset moduleConfigJson;

        [Tooltip("Auto initialize module system on scene start")]
        public bool autoInitialize = true;

        [Header("Quick Configuration")]
        [Tooltip("Enable full game mode (all systems)")]
        public bool fullGameMode = true;

        [Tooltip("Enable performance test mode (minimal systems)")]
        public bool performanceTestMode = false;

        [Tooltip("Enable debug tools")]
        public bool enableDebugTools = true;

        [Header("Custom Modules")]
        [Tooltip("Additional module IDs to enable")]
        public string[] additionalModules;

        [Tooltip("Module IDs to disable")]
        public string[] disabledModules;

        private GameBootstrap _bootstrap;

        private void Awake()
        {
            if (autoInitialize)
            {
                InitializeModuleSystem();
            }
        }

        /// <summary>
        /// Initialize module system
        /// </summary>
        public void InitializeModuleSystem()
        {
            // If GameBootstrap already exists, don't create duplicate
            if (FindObjectOfType<GameBootstrap>() != null)
            {
                UnityEngine.Debug.LogWarning("GameBootstrap already exists, skipping creation");
                return;
            }

            // Create GameBootstrap GameObject
            GameObject bootstrapObj = new GameObject("GameBootstrap (Runtime)");
            _bootstrap = bootstrapObj.AddComponent<GameBootstrap>();

            // Configure GameBootstrap
            if (moduleConfigJson != null)
            {
                // Use specified config file
                _bootstrap.gameConfigJson = moduleConfigJson;
            }
            else
            {
                // Create config based on quick settings
                _bootstrap.gameConfigJson = CreateRuntimeConfig();
            }

            _bootstrap.autoInitialize = true;
            _bootstrap.verboseLogging = enableDebugTools;

            DontDestroyOnLoad(bootstrapObj);

            UnityEngine.Debug.Log("SceneModuleInitializer: Module system created");
        }

        /// <summary>
        /// Create runtime configuration
        /// </summary>
        private TextAsset CreateRuntimeConfig()
        {
            var config = new GameConfiguration
            {
                projectName = "PVZ Runtime Config",
                projectType = "tower-defense",
                version = "1.0.0",
                modules = new System.Collections.Generic.List<ModuleConfig>()
            };

            if (performanceTestMode)
            {
                // Performance test mode: minimal modules
                AddModule(config, "core.ecs", true);
                AddModule(config, "render.view", true);
                AddModule(config, "pvz.zombie-system", true);
                AddModule(config, "animation.spine-optimization", true);
            }
            else if (fullGameMode)
            {
                // Full game mode: all core modules
                AddModule(config, "core.ecs", true);
                AddModule(config, "render.view", true);
                AddModule(config, "pvz.level-management", true);
                AddModule(config, "pvz.game-loop", true);
                AddModule(config, "pvz.plant-system", true);
                AddModule(config, "pvz.zombie-system", true);
                AddModule(config, "combat.projectile", true);
                AddModule(config, "ui.health-bar", true);
                AddModule(config, "animation.spine-optimization", true);

                if (enableDebugTools)
                {
                    AddModule(config, "debug.map-grid", true);
                }
            }

            // Add additional modules
            if (additionalModules != null)
            {
                foreach (var moduleId in additionalModules)
                {
                    if (!string.IsNullOrEmpty(moduleId))
                    {
                        AddModule(config, moduleId, true);
                    }
                }
            }

            // Disable specified modules
            if (disabledModules != null)
            {
                foreach (var moduleId in disabledModules)
                {
                    DisableModule(config, moduleId);
                }
            }

            // Convert to TextAsset (create temporary config)
            string jsonText = JsonUtility.ToJson(config, true);
            
            // Create temporary TextAsset
            var tempAsset = new TextAsset(jsonText);
            tempAsset.name = "RuntimeModuleConfig";
            
            return tempAsset;
        }

        private void AddModule(GameConfiguration config, string moduleId, bool enabled)
        {
            // Check if already exists
            if (config.modules.Exists(m => m.moduleId == moduleId))
                return;

            config.modules.Add(new ModuleConfig
            {
                moduleId = moduleId,
                enabled = enabled,
                parametersJson = "{}",
                orderOverride = -1
            });
        }

        private void DisableModule(GameConfiguration config, string moduleId)
        {
            var module = config.modules.Find(m => m.moduleId == moduleId);
            if (module != null)
            {
                module.enabled = false;
            }
        }

        private void OnDestroy()
        {
            // No need for manual cleanup, GameBootstrap handles it
        }

        #region Editor Utilities
        
#if UNITY_EDITOR
        [ContextMenu("Show Available Modules")]
        private void ShowAvailableModules()
        {
            ModuleFactory.Initialize();
            var moduleIds = ModuleFactory.GetAllModuleIds();
            
            UnityEngine.Debug.Log("====== Available Modules ======");
            foreach (var id in moduleIds)
            {
                var type = ModuleFactory.GetModuleType(id);
                var instance = System.Activator.CreateInstance(type) as IGameModule;
                UnityEngine.Debug.Log($"- {id}: {instance.DisplayName} (Priority: {instance.Priority})");
            }
        }

        [ContextMenu("Generate Config Template")]
        private void GenerateConfigTemplate()
        {
            var tempConfig = CreateRuntimeConfig();
            UnityEngine.Debug.Log("Generated Config:\n" + tempConfig.text);
        }
#endif

        #endregion
    }
}



