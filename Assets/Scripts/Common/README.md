# Common - 游戏通用层

## 概述
Common 层包含所有游戏类型共享的通用代码，这些代码可以在任何游戏项目中复用。

## 目录结构

### Components/
- **HealthComponent.cs** - 生命值组件
- **HealthBarComponent.cs** - 血条组件
- **GridPositionComponent.cs** - 网格位置组件
- **ViewPrefabComponent.cs** - 视图预制体组件
- **RenderComponents.cs** - 渲染组件集合
- **SpineOptimizationComponents.cs** - Spine 优化组件
- **GameStateComponent.cs** - 游戏状态组件
- **GameConfigComponent.cs** - 游戏配置组件
- **AttackStateComponent.cs** - 攻击状态组件

### Systems/
- **ViewLoaderSystem.cs** - 视图加载系统
- **ViewSystemBase.cs** - 视图系统基类
- **SpineViewSystem.cs** - Spine 视图系统
- **MeshRendererViewSystem.cs** - Mesh 渲染视图系统
- **ViewCleanupSystem.cs** - 视图清理系统
- **SpineOptimizationSystems.cs** - Spine 优化系统集合
- **HealthBarSystem.cs** - 血条系统
- **HealthBarManager.cs** - 血条管理器

### Modules/
- **EventBusModule.cs** - 事件总线模块
- **ServiceModules.cs** - 服务模块集合
- **RenderingModule.cs** - 渲染核心模块
- **SpineRenderModule.cs** - Spine 渲染模块

### Utils/
（待添加通用工具类）

## 命名空间
- `PVZ.Common.Components`
- `PVZ.Common.Systems`
- `PVZ.Common.Modules`
- `PVZ.Common.Utils`

## 依赖关系
- 依赖：Framework 层
- 被依赖：Type 层、SpecGame 层
