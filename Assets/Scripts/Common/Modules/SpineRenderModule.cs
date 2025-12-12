using UnityEngine;
using Unity.Entities;
using Framework;

namespace Common
{
    /// <summary>
    /// Spine 渲染模块 - 管理 Spine 动画渲染系统
    /// </summary>
    public class SpineRenderModule : GameModuleBase
    {
        public override string ModuleId => "render.spine";
        public override string DisplayName => "Spine 渲染系统";
        public override int Priority => 51;
        public override string[] Dependencies => new[] { "render.core" };

        private SpineViewSystem _spineSystem;
        private SpineRenderConfig _config;
        private string _parametersJson;

        /// <summary>
        /// 设置模块参数（由 ModuleRegistry 调用�?
        /// </summary>
        public void SetParametersJson(string parametersJson)
        {
            _parametersJson = parametersJson;
        }

        protected override void OnInitialize()
        {
            // 解析配置参数
            if (!string.IsNullOrEmpty(_parametersJson))
            {
                try
                {
                    _config = JsonUtility.FromJson<SpineRenderConfig>(_parametersJson);
                    UnityEngine.Debug.Log($"[SpineRenderModule] Loaded config from JSON: LOD={_config.lodEnabled}, Near={_config.lodNearDistance}");
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[SpineRenderModule] Failed to parse config JSON: {ex.Message}, using default config");
                    _config = SpineRenderConfig.Default();
                }
            }
            else
            {
                _config = SpineRenderConfig.Default();
                UnityEngine.Debug.Log("[SpineRenderModule] Using default config");
            }

            // 如果配置禁用，不创建系统
            if (!_config.enabled)
            {
                UnityEngine.Debug.Log("[SpineRenderModule] Disabled by config");
                return;
            }

            // 获取 ECS World
            var world = Context.GetService<World>();
            if (world == null)
            {
                UnityEngine.Debug.LogError("[SpineRenderModule] World not found in module context!");
                return;
            }

            // 创建 Spine 视图系统
            _spineSystem = world.GetOrCreateSystemManaged<SpineViewSystem>();

            // 应用配置到系�?
            if (_spineSystem != null)
            {
                _spineSystem.SetConfig(_config);
                UnityEngine.Debug.Log($"[SpineRenderModule] Initialized - SpineViewSystem created with config");
            }
        }

        protected override void OnShutdown()
        {
            _spineSystem = null;
            UnityEngine.Debug.Log("[SpineRenderModule] Shutdown complete");
        }

        /// <summary>
        /// 获取 Spine 视图系统
        /// </summary>
        public SpineViewSystem GetSpineViewSystem() => _spineSystem;

        /// <summary>
        /// 获取当前配置
        /// </summary>
        public SpineRenderConfig GetConfig() => _config;

        /// <summary>
        /// 运行时更新配�?
        /// </summary>
        public void UpdateConfig(SpineRenderConfig newConfig)
        {
            _config = newConfig;
            if (_spineSystem != null)
            {
                _spineSystem.SetConfig(_config);
                UnityEngine.Debug.Log("[SpineRenderModule] Config updated at runtime");
            }
        }
    }
}
