using UnityEngine;
using Unity.Entities;
using Framework;

namespace Common
{
    /// <summary>
    /// 渲染核心模块 - 管理视图加载系统
    /// </summary>
    public class RenderingCoreModule : GameModuleBase
    {
        public override string ModuleId => "render.core";
        public override string DisplayName => "渲染核心系统";
        public override int Priority => 50;
        public override string[] Dependencies => new[] { "core.ecs", "service.resource", "service.pool" };

        private ViewLoaderSystem _viewLoaderSystem;

        protected override void OnInitialize()
        {
            // 获取 ECS World
            var world = Context.GetService<World>();
            if (world == null)
            {
                UnityEngine.Debug.LogError("[RenderingCoreModule] World not found in module context!");
                return;
            }

            // 创建视图加载系统（系统会自己通过单例 Context 获取服务�?
            _viewLoaderSystem = world.GetOrCreateSystemManaged<ViewLoaderSystem>();

            UnityEngine.Debug.Log("[RenderingCoreModule] Initialized - ViewLoader system created");
        }

        protected override void OnShutdown()
        {
            // 清理资源
            if (_viewLoaderSystem != null)
            {
                _viewLoaderSystem.CleanupAllViews();
            }

            _viewLoaderSystem = null;

            UnityEngine.Debug.Log("[RenderingCoreModule] Shutdown complete");
        }

        /// <summary>
        /// 获取视图加载系统
        /// </summary>
        public ViewLoaderSystem GetViewLoaderSystem() => _viewLoaderSystem;
    }
}
