# 视图系统模块化集成指南

## 概述

本文档说明如何将现有的视图系统（ViewLoaderSystem 和 SpineViewSystem）集成到新的模块化架构中。

## 架构设计

### 模块层次结构

```
RenderingModule (render.view)
├── 依赖
│   ├── core.ecs (ECS World)
│   ├── service.resource (资源加载)
│   └── service.pool (对象池管理)
├── 管理的系统
│   ├── ViewLoaderSystem (视图加载系统)
│   └── SpineViewSystem (Spine 渲染系统)
└── 提供的功能
    ├── 自动视图加载
    ├── Spine 动画管理
    ├── LOD 性能优化
    └── 视图清理
```

## 核心组件

### 1. RenderingModule

渲染模块是视图系统的入口，负责管理 ECS 系统的生命周期。

```csharp
public class RenderingModule : GameModuleBase
{
    public override string ModuleId => "render.view";
    public override int Priority => 50;
    public override string[] Dependencies => new[] 
    { 
        "core.ecs",           // ECS World 依赖
        "service.resource",   // 资源服务依赖
        "service.pool"        // 对象池服务依赖
    };

    private ViewLoaderSystem _viewLoaderSystem;
    private SpineViewSystem _spineViewSystem;

    public override void Initialize(IModuleContext context)
    {
        // 1. 获取 ECS World
        var world = context.GetService<World>();

        // 2. 创建系统
        _viewLoaderSystem = world.GetOrCreateSystemManaged<ViewLoaderSystem>();
        _spineViewSystem = world.GetOrCreateSystemManaged<SpineViewSystem>();

        // 3. 注入服务
        _viewLoaderSystem.SetResourceService(context.GetService<IResourceService>());
        _viewLoaderSystem.SetPoolService(context.GetService<IPoolService>());
        _spineViewSystem.SetResourceService(context.GetService<IResourceService>());
    }

    public override void Shutdown()
    {
        // 清理所有视图
        _viewLoaderSystem?.CleanupAllViews();
    }
}
```

### 2. ViewLoaderSystem（已改造）

视图加载系统现在支持服务注入：

**改造要点**：
1. 添加服务字段
2. 提供服务注入方法
3. 优先使用资源服务加载预制体
4. 提供公共清理方法

```csharp
public partial class ViewLoaderSystem : SystemBase
{
    private IResourceService _resourceService;
    private IPoolService _poolService;

    // 由 RenderingModule 调用
    public void SetResourceService(IResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    public void SetPoolService(IPoolService poolService)
    {
        _poolService = poolService;
    }

    private void LoadViewForEntity(...)
    {
        // 优先使用资源服务
        GameObject prefab = null;
        if (_resourceService != null)
        {
            prefab = _resourceService.Load<GameObject>(prefabPath);
        }
        else
        {
            prefab = Resources.Load<GameObject>(prefabPath); // 降级方案
        }
        
        // ... 实例化逻辑
    }

    // 提供给模块的清理方法
    public void CleanupAllViews()
    {
        foreach (var entity in QueryBuilder().WithAll<ViewInstanceComponent>()...)
        {
            // 清理逻辑
        }
    }
}
```

### 3. SpineViewSystem（已改造）

Spine 视图系统添加了资源服务注入：

```csharp
public partial class SpineViewSystem : ViewSystemBase
{
    private IResourceService _resourceService;

    public void SetResourceService(IResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    // 渲染逻辑保持不变
}
```

## 配置文件集成

在 `GameModuleConfig.json` 中配置渲染模块：

```json
{
  "modules": [
    {
      "moduleId": "core.ecs",
      "enabled": true,
      "parametersJson": "{}",
      "orderOverride": -1
    },
    {
      "moduleId": "service.resource",
      "enabled": true,
      "parametersJson": "{\"cacheEnabled\": true}",
      "orderOverride": -1
    },
    {
      "moduleId": "service.pool",
      "enabled": true,
      "parametersJson": "{\"defaultPoolSize\": 50, \"maxPoolSize\": 200}",
      "orderOverride": -1
    },
    {
      "moduleId": "render.view",
      "enabled": true,
      "parametersJson": "{}",
      "orderOverride": -1
    }
  ]
}
```

## 使用方式

### 方式1：通过 ECS 自动加载视图

创建实体时添加 `ViewPrefabComponent`，系统会自动加载视图：

```csharp
var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

// 创建实体
var zombie = entityManager.CreateEntity();

// 添加 Transform
entityManager.AddComponentData(zombie, new LocalTransform
{
    Position = new float3(10, 0, 3),
    Rotation = quaternion.identity,
    Scale = 1f
});

// 添加视图预制体组件（会自动触发加载）
entityManager.AddComponentData(zombie, new ViewPrefabComponent
{
    PrefabPath = "Res/Spine/Zombie_Normal",
    IsViewLoaded = false
});

// 添加视图状态组件（控制动画）
entityManager.AddComponentData(zombie, new ViewStateComponent
{
    CurrentAnimationState = AnimationState.Walk,
    NeedsAnimationUpdate = true,
    ColorTint = 1.0f
});
```

**流程**：
1. `ViewLoaderSystem` 检测到新实体有 `ViewPrefabComponent` 但没有 `ViewInstanceComponent`
2. 通过资源服务加载预制体：`_resourceService.Load<GameObject>(path)`
3. 实例化 GameObject 并绑定到实体
4. 自动检测渲染类型（Spine/MeshRenderer/SpriteRenderer）
5. 添加对应的渲染组件标记（`SpineRenderComponent` 或 `MeshRenderComponent`）
6. `SpineViewSystem` 开始更新 Spine 动画

### 方式2：通过模块服务创建

```csharp
// 在任何系统或模块中
var context = GameBootstrap.Instance.Context;
var world = context.GetService<World>();
var resourceService = context.GetService<IResourceService>();

// 创建带视图的实体
Entity CreateEntityWithView(float3 position, string prefabPath)
{
    var entityManager = world.EntityManager;
    var entity = entityManager.CreateEntity();

    entityManager.AddComponentData(entity, new LocalTransform
    {
        Position = position,
        Rotation = quaternion.identity,
        Scale = 1f
    });

    entityManager.AddComponentData(entity, new ViewPrefabComponent
    {
        PrefabPath = prefabPath,
        IsViewLoaded = false
    });

    entityManager.AddComponentData(entity, new ViewStateComponent
    {
        CurrentAnimationState = AnimationState.Idle,
        NeedsAnimationUpdate = true
    });

    return entity;
}
```

## 与事件总线配合使用

### 示例：僵尸死亡处理

```csharp
public class ZombieDeathHandler
{
    private IEventBus _eventBus;
    private World _world;

    public void Initialize(IModuleContext context)
    {
        _eventBus = context.GetService<IEventBus>();
        _world = context.GetService<World>();

        // 订阅僵尸死亡事件
        _eventBus.Subscribe<ZombieDeathEvent>(OnZombieDeath);
    }

    private void OnZombieDeath(ZombieDeathEvent evt)
    {
        var entityManager = _world.EntityManager;

        // 查找僵尸实体（假设事件包含 Entity）
        Entity zombieEntity = evt.Entity;

        // 更改动画为死亡
        if (entityManager.HasComponent<ViewStateComponent>(zombieEntity))
        {
            var viewState = entityManager.GetComponentData<ViewStateComponent>(zombieEntity);
            viewState.CurrentAnimationState = AnimationState.Death;
            viewState.NeedsAnimationUpdate = true;
            entityManager.SetComponentData(zombieEntity, viewState);
        }

        // SpineViewSystem 会在下一帧应用死亡动画
    }
}
```

## 数据流向

```
┌────────────────────────────────────────────────────┐
│                 模块化架构                          │
├────────────────────────────────────────────────────┤
│                                                    │
│  GameBootstrap                                     │
│       ↓                                            │
│  ModuleRegistry                                    │
│       ↓                                            │
│  RenderingModule.Initialize()                      │
│       ↓                                            │
│  ┌─────────────────────────────────────────┐      │
│  │ 1. 获取 World (从 ECSModule)            │      │
│  │ 2. 创建 ViewLoaderSystem                │      │
│  │ 3. 创建 SpineViewSystem                 │      │
│  │ 4. 注入 ResourceService                 │      │
│  │ 5. 注入 PoolService                     │      │
│  └─────────────────────────────────────────┘      │
│       ↓                                            │
│  ECS Update Loop                                   │
│       ↓                                            │
│  ┌────────────────────┐  ┌──────────────────┐     │
│  │ ViewLoaderSystem   │  │ SpineViewSystem  │     │
│  │ (InitializationGrp)│  │ (PresentationGrp)│     │
│  └────────────────────┘  └──────────────────┘     │
│       ↓                         ↓                  │
│  加载视图预制体          更新 Spine 动画           │
│  实例化 GameObject       应用 LOD 优化             │
│  绑定到 Entity           视锥剔除                  │
│                                                    │
└────────────────────────────────────────────────────┘
```

## 性能优化

### 1. 资源缓存

通过资源服务自动缓存：

```csharp
var resourceService = context.GetService<IResourceService>();

// 首次加载（从磁盘）
var prefab1 = resourceService.Load<GameObject>("Res/Spine/Zombie_Normal");

// 再次加载（从缓存）
var prefab2 = resourceService.Load<GameObject>("Res/Spine/Zombie_Normal");
```

### 2. 预加载

关卡开始前预加载所有视图资源：

```csharp
public void PreloadLevelAssets(int levelId)
{
    var resourceService = context.GetService<IResourceService>();

    string[] assets = new[]
    {
        "Res/Spine/Zombie_Normal",
        "Res/Spine/Zombie_Conehead",
        "Res/Spine/Plant_Peashooter"
    };

    resourceService.PreloadAssets(assets, () =>
    {
        Debug.Log("关卡资源预加载完成");
        // 开始关卡
    });
}
```

### 3. LOD 自动优化

`SpineViewSystem` 内置 LOD 优化：

- **近距离（< 15 单位）**：每帧更新
- **中距离（15-30 单位）**：每 2 帧更新
- **远距离（> 30 单位）**：每 4 帧更新
- **屏幕外**：禁用渲染

无需额外配置，自动生效。

## 调试和监控

### 查看加载的视图数量

```csharp
var world = context.GetService<World>();
var query = world.EntityManager.CreateEntityQuery(typeof(ViewInstanceComponent));
int count = query.CalculateEntityCount();
Debug.Log($"当前加载视图数量: {count}");
query.Dispose();
```

### 检查系统状态

```csharp
var world = context.GetService<World>();
var viewLoaderSystem = world.GetExistingSystemManaged<ViewLoaderSystem>();
var spineViewSystem = world.GetExistingSystemManaged<SpineViewSystem>();

Debug.Log($"ViewLoaderSystem 启用: {viewLoaderSystem.Enabled}");
Debug.Log($"SpineViewSystem 启用: {spineViewSystem.Enabled}");
```

## 常见问题

### Q1: 视图没有加载？

**检查清单**：
1. `render.view` 模块是否在配置中启用
2. `service.resource` 模块是否已启动
3. `ViewPrefabComponent.PrefabPath` 路径是否正确
4. 预制体是否放在 `Resources` 目录下

### Q2: 动画没有播放？

**检查清单**：
1. `ViewStateComponent.NeedsAnimationUpdate` 是否为 `true`
2. `SpineViewSystem` 是否启用（检查配置）
3. Spine 预制体是否包含 `SkeletonAnimation` 组件
4. 动画名称是否正确（`GetAnimationName()` 映射）

### Q3: 性能问题？

**优化建议**：
1. 启用资源缓存：`"cacheEnabled": true`
2. 预加载常用资源
3. 使用对象池管理 GameObject
4. 检查 LOD 距离设置是否合理
5. 确保屏幕外剔除生效

## 迁移指南

### 从旧代码迁移

**旧方式**（直接使用系统）：
```csharp
var world = World.DefaultGameObjectInjectionWorld;
var viewLoaderSystem = world.GetOrCreateSystemManaged<ViewLoaderSystem>();
```

**新方式**（通过模块）：
```csharp
var context = GameBootstrap.Instance.Context;
var world = context.GetService<World>();
var viewLoaderSystem = world.GetExistingSystemManaged<ViewLoaderSystem>();
// 或者通过 RenderingModule 访问
```

## 最佳实践

1. **始终通过模块启动**：不要手动创建 ECS 系统
2. **使用服务注入**：让模块管理依赖关系
3. **通过事件通信**：视图变化发布事件，其他系统订阅响应
4. **预加载资源**：关卡启动前预加载所有视图资源
5. **及时清理**：关卡结束时调用 `CleanupAllViews()`

## 总结

通过模块化改造，视图系统现在：

✅ **解耦合**：通过服务注入，不直接依赖 Resources.Load  
✅ **可配置**：通过 JSON 配置启用/禁用  
✅ **可测试**：可以 Mock 资源服务进行单元测试  
✅ **易维护**：清晰的依赖关系和生命周期管理  
✅ **高性能**：资源缓存 + LOD 优化 + 视锥剔除

参考 `ViewModuleExamples.cs` 了解更多使用场景。
