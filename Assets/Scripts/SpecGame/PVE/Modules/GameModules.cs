using Unity.Entities;
using UnityEngine;
using Framework;
using Common;
using Debug;

namespace PVZ
{
    /// <summary>
    /// 核心ECS模块 - 管理ECS系统的创建和生命周期
    /// 所有使用ECS的模块都依赖此模�?
    /// </summary>
    public class CoreECSModule : GameModuleBase
    {
        public override string ModuleId => "core.ecs";
        public override string DisplayName => "核心ECS系统";
        public override int Priority => 0; // 最高优先级
        public override string[] Dependencies => System.Array.Empty<string>();

        private World _world;
        private EntityManager _entityManager;

        protected override void OnInitialize()
        {
            _world = Context.GetWorld();
            _entityManager = _world.EntityManager;

            UnityEngine.Debug.Log($"ECS World: {_world.Name}, Systems Count: {_world.Systems.Count}");
            
            // 注册ECS服务供其他模块使�?
            Context.RegisterService<World>(_world);
            // EntityManager是值类型，其他模块可以通过World.EntityManager获取
        }

        protected override void OnShutdown()
        {
            // ECS World通常由Unity管理，不需要手动销�?
        }
    }

    /// <summary>
    /// 战斗-投射物模�?- 管理子弹、投射物系统
    /// </summary>
    public class CombatProjectileModule : GameModuleBase
    {
        public override string ModuleId => "combat.projectile";
        public override string DisplayName => "投射物战斗系统";
        public override string[] Dependencies => new[] { "core.ecs", "render.view" };
        public override int Priority => 110;

        protected override void OnInitialize()
        {
            var world = Context.GetWorld();
            
            // 确保投射物相关系统已创建
            // ProjectileMovementSystem, ProjectileHitSystem 等会自动创建
            UnityEngine.Debug.Log("投射物系统已就绪");
        }
    }

    /// <summary>
    /// 渲染-视图模块 - 管理ECS实体与Unity GameObject的同�?
    /// </summary>
    public class RenderViewModule : GameModuleBase
    {
        public override string ModuleId => "render.view";
        public override string DisplayName => "视图渲染系统";
        public override string[] Dependencies => new[] { "core.ecs" };
        public override int Priority => 50;

        protected override void OnInitialize()
        {
            var world = Context.GetWorld();
            
            // ViewLoaderSystem, ViewCleanupSystem, SpineViewSystem 等会自动创建
            UnityEngine.Debug.Log("视图系统已初始化 (ViewLoader, Spine, MeshRenderer)");
        }
    }

    /// <summary>
    /// UI-血条模�?- 管理实体血条显�?
    /// </summary>
    public class UIHealthBarModule : GameModuleBase
    {
        public override string ModuleId => "ui.health-bar";
        public override string DisplayName => "血条UI系统";
        public override string[] Dependencies => new[] { "core.ecs", "render.view" };
        public override int Priority => 120;

        protected override void OnInitialize()
        {
            // HealthBarSystem 会自动创�?
            UnityEngine.Debug.Log("血条系统已初始化");
        }
    }

    /// <summary>
    /// 动画-Spine优化模块 - 管理Spine动画性能优化
    /// </summary>
    public class AnimationSpineOptimizationModule : GameModuleBase
    {
        public override string ModuleId => "animation.spine-optimization";
        public override string DisplayName => "Spine动画优化";
        public override string[] Dependencies => new[] { "render.view" };
        public override int Priority => 130;

        private bool _enableCulling;
        private bool _enableLOD;

        protected override void OnInitialize()
        {
            _enableCulling = Context.GetConfigParameter("spine.culling.enabled", true);
            _enableLOD = Context.GetConfigParameter("spine.lod.enabled", true);

            UnityEngine.Debug.Log($"Spine优化已启�?- 视锥剔除: {_enableCulling}, LOD: {_enableLOD}");
            
            // ViewCullingSystem, LODSystem 会自动创�?
        }
    }

    /// <summary>
    /// PVZ-关卡管理模块 - 管理关卡配置和波次推�?
    /// </summary>
    public class PVZLevelManagementModule : GameModuleBase
    {
        public override string ModuleId => "pvz.level-management";
        public override string DisplayName => "关卡管理系统";
        public override string[] Dependencies => new[] { "core.ecs" };
        public override int Priority => 80;

        protected override void OnInitialize()
        {
            // LevelManagementSystem 会自动创�?
            UnityEngine.Debug.Log("关卡管理系统已初始化");
        }
    }

    /// <summary>
    /// PVZ-植物系统模块 - 管理植物相关逻辑
    /// </summary>
    public class PVZPlantSystemModule : GameModuleBase
    {
        public override string ModuleId => "pvz.plant-system";
        public override string DisplayName => "植物系统";
        public override string[] Dependencies => new[] { "core.ecs", "combat.projectile", "render.view" };
        public override int Priority => 100;

        protected override void OnInitialize()
        {
            // PlantAttackSystem, SunProductionSystem 等会自动创建
            UnityEngine.Debug.Log("植物系统已初始化 (攻击、阳光生�?");
        }
    }

    /// <summary>
    /// PVZ-僵尸系统模块 - 管理僵尸相关逻辑
    /// </summary>
    public class PVZZombieSystemModule : GameModuleBase
    {
        public override string ModuleId => "pvz.zombie-system";
        public override string DisplayName => "僵尸系统";
        public override string[] Dependencies => new[] { "core.ecs", "render.view" };
        public override int Priority => 100;

        protected override void OnInitialize()
        {
            // ZombieSpawnSystem, ZombieMovementSystem, ZombieAttackSystem 等会自动创建
            UnityEngine.Debug.Log("僵尸系统已初始化 (生成、移动、攻�?");
        }
    }

    /// <summary>
    /// PVZ-游戏循环模块 - 管理游戏状态和主循�?
    /// </summary>
    public class PVZGameLoopModule : GameModuleBase
    {
        public override string ModuleId => "pvz.game-loop";
        public override string DisplayName => "游戏主循环";
        public override string[] Dependencies => new[] { "core.ecs", "pvz.level-management" };
        public override int Priority => 90;

        protected override void OnInitialize()
        {
            // GameLoopSystem 会自动创建
            UnityEngine.Debug.Log("游戏主循环系统已初始化");
        }
    }

    /// <summary>
    /// 调试-网格可视化模�?- 显示地图网格
    /// </summary>
    public class DebugMapGridModule : GameModuleBase
    {
        public override string ModuleId => "debug.map-grid";
        public override string DisplayName => "地图网格调试";
        public override string[] Dependencies => new[] { "core.ecs" };
        public override int Priority => 200;

        private GameObject _debugDrawerObject;

        protected override void OnInitialize()
        {
            // 创建MapGridDebugDrawer GameObject
            _debugDrawerObject = new GameObject("MapGridDebugDrawer");
            var drawer = _debugDrawerObject.AddComponent<Debug.MapGridDebugDrawer>();
            
            drawer.enableGridDrawing = Context.GetConfigParameter("debug.grid.enabled", true);
            
            GameObject.DontDestroyOnLoad(_debugDrawerObject);
            UnityEngine.Debug.Log("地图网格调试器已创建");
        }

        protected override void OnShutdown()
        {
            if (_debugDrawerObject != null)
            {
                GameObject.Destroy(_debugDrawerObject);
            }
        }
    }
}
