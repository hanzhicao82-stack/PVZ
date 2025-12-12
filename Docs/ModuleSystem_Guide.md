# 模块化架构使用指南

## 概述

PVZ游戏采用模块化架构设计，允许通过配置文件动态组合游戏功能模块，实现一套框架支持多种游戏类型和测试场景。

## 核心概念

### 1. 模块（Module）
- **定义**: 独立的功能单元，封装特定领域的逻辑
- **特性**: 可插拔、可配置、有明确的依赖关系
- **示例**: 投射物系统、血条UI、Spine优化

### 2. 模块注册表（ModuleRegistry）
- 管理所有模块的生命周期
- 解析模块依赖关系
- 按正确顺序初始化和关闭模块

### 3. 游戏启动器（GameBootstrap）
- 加载配置文件
- 创建ECS World
- 初始化模块系统

## 快速开始

### 步骤1: 创建GameBootstrap

在场景中创建GameObject，添加`GameBootstrap`组件：

```csharp
1. 创建空GameObject，命名为"GameBootstrap"
2. 添加组件: GameBootstrap
3. 配置参数:
   - Game Config Json: 拖入 GameModuleConfig.json
   - Auto Initialize: 勾选
   - Verbose Logging: 勾选（调试时）
```

### 步骤2: 配置模块

编辑`Assets/Configs/GameModuleConfig.json`：

```json
{
  "projectName": "Your Game",
  "projectType": "tower-defense",
  "modules": [
    {
      "moduleId": "core.ecs",
      "enabled": true
    },
    {
      "moduleId": "pvz.plant-system",
      "enabled": true
    }
  ]
}
```

### 步骤3: 运行游戏

点击Play，模块系统会自动：
1. 加载配置
2. 解析依赖
3. 按顺序初始化模块
4. 启动游戏

## 可用模块列表

### 核心模块

| 模块ID | 名称 | 优先级 | 依赖 | 说明 |
|--------|------|--------|------|------|
| `core.ecs` | 核心ECS系统 | 0 | 无 | 必需，管理ECS World |
| `render.view` | 视图渲染系统 | 50 | core.ecs | 实体与GameObject同步 |

### PVZ游戏模块

| 模块ID | 名称 | 优先级 | 依赖 | 说明 |
|--------|------|--------|------|------|
| `pvz.level-management` | 关卡管理 | 80 | core.ecs | 关卡配置、波次推进 |
| `pvz.game-loop` | 游戏主循环 | 90 | core.ecs, level-management | 游戏状态管理 |
| `pvz.plant-system` | 植物系统 | 100 | core.ecs, combat, view | 植物攻击、生产阳光 |
| `pvz.zombie-system` | 僵尸系统 | 100 | core.ecs, view | 僵尸生成、移动、攻击 |

### 战斗模块

| 模块ID | 名称 | 优先级 | 依赖 | 说明 |
|--------|------|--------|------|------|
| `combat.projectile` | 投射物系统 | 110 | core.ecs, view | 子弹移动、碰撞检测 |

### UI模块

| 模块ID | 名称 | 优先级 | 依赖 | 说明 |
|--------|------|--------|------|------|
| `ui.health-bar` | 血条UI | 120 | core.ecs, view | 实体血条显示 |

### 优化模块

| 模块ID | 名称 | 优先级 | 依赖 | 说明 |
|--------|------|--------|------|------|
| `animation.spine-optimization` | Spine优化 | 130 | view | 视锥剔除、LOD |

### 调试模块

| 模块ID | 名称 | 优先级 | 依赖 | 说明 |
|--------|------|--------|------|------|
| `debug.map-grid` | 地图网格 | 200 | core.ecs | 网格可视化 |

## 配置文件结构

```json
{
  "projectName": "项目名称",
  "projectType": "项目类型标识",
  "version": "版本号",
  "modules": [
    {
      "moduleId": "模块ID",
      "enabled": true/false,
      "parametersJson": "{JSON格式的自定义参数}",
      "orderOverride": -1
    }
  ],
  "globalParameters": [
    {
      "key": "参数键",
      "value": "参数值"
    }
  ]
}
```

### 参数说明

- **moduleId**: 模块唯一标识符（必需）
- **enabled**: 是否启用该模块（必需）
- **parametersJson**: 模块自定义参数（可选）
- **orderOverride**: 覆盖默认初始化顺序（可选，-1表示使用默认）
- **globalParameters**: 全局配置参数，所有模块都可访问

## 预设配置

### 完整游戏配置
文件: `GameModuleConfig.json`
- 包含所有PVZ游戏模块
- 启用血条、调试工具
- 适用于正常游戏开发

### 性能测试配置
文件: `GameModuleConfig_PerformanceTest.json`
- 只加载核心和僵尸模块
- 禁用UI和调试工具
- 适用于性能压力测试

## 创建自定义模块

### 1. 定义模块类

```csharp
using PVZ.Framework.ModuleSystem;

public class MyCustomModule : GameModuleBase
{
    public override string ModuleId => "custom.my-module";
    public override string DisplayName => "我的自定义模块";
    public override string[] Dependencies => new[] { "core.ecs" };
    public override int Priority => 150;

    protected override void OnInitialize()
    {
        // 初始化逻辑
        var world = Context.GetWorld();
        Debug.Log("自定义模块已初始化");
    }

    protected override void OnShutdown()
    {
        // 清理逻辑
    }

    public override void Update(float deltaTime)
    {
        // 每帧更新逻辑（可选）
    }
}
```

### 2. 注册到配置文件

```json
{
  "modules": [
    {
      "moduleId": "custom.my-module",
      "enabled": true
    }
  ]
}
```

### 3. 自动发现

模块工厂会自动扫描所有实现`IGameModule`的类型，无需手动注册。

## 模块间通信

### 获取其他模块

```csharp
protected override void OnInitialize()
{
    // 通过类型获取
    var plantModule = Context.GetModule<PVZPlantSystemModule>();
    
    // 通过ID获取
    var zombieModule = Context.GetModule("pvz.zombie-system");
}
```

### 注册和获取服务

```csharp
// 注册服务
Context.RegisterService<IMyService>(new MyServiceImpl());

// 获取服务
var service = Context.GetService<IMyService>();
```

### 访问配置参数

```csharp
// 获取全局参数
bool enableCulling = Context.GetConfigParameter("spine.culling.enabled", true);
int maxZombies = Context.GetConfigParameter<int>("game.max-zombies", 100);
```

## 最佳实践

### 1. 模块职责单一
- 每个模块只负责一个明确的功能领域
- 避免模块过于庞大或职责混乱

### 2. 明确依赖关系
- 在`Dependencies`中声明所有依赖模块
- 避免循环依赖

### 3. 合理设置优先级
- 核心基础模块: 0-50
- 游戏逻辑模块: 100-150
- UI和优化模块: 150-200
- 调试工具模块: 200+

### 4. 使用配置参数
- 将可变的设置提取为配置参数
- 便于不同项目使用不同配置

### 5. 错误处理
- 在Initialize中捕获异常
- 提供有意义的错误日志

## 调试技巧

### 查看模块初始化顺序

启用`Verbose Logging`，控制台会输出：
```
====== 开始注册模块 ======
注册模块: 核心ECS系统 (core.ecs)
注册模块: 视图渲染系统 (render.view)
...
====== 开始初始化模块系统 ======
✓ 模块 [核心ECS系统] (core.ecs) 初始化成功
✓ 模块 [视图渲染系统] (render.view) 初始化成功
...
====== 模块系统初始化完成 (10个模块) ======
```

### 禁用特定模块

在配置文件中设置`enabled: false`：
```json
{
  "moduleId": "debug.map-grid",
  "enabled": false
}
```

### 检查依赖错误

如果模块依赖未满足，会抛出异常：
```
模块 pvz.plant-system 依赖的模块 combat.projectile 未注册
```

## 扩展场景

### 场景1: 多人对战模式
创建`GameModuleConfig_PVP.json`：
- 禁用关卡管理
- 启用网络同步模块
- 启用双方种植模块

### 场景2: 关卡编辑器
创建`GameModuleConfig_Editor.json`：
- 启用所有调试模块
- 启用网格编辑器
- 禁用游戏循环

### 场景3: 移动端优化
创建`GameModuleConfig_Mobile.json`：
- 强制启用Spine优化
- 禁用高消耗特效
- 降低视图更新频率

## 常见问题

**Q: 模块初始化顺序不对？**
A: 检查`Priority`和`Dependencies`设置，模块系统会根据依赖自动排序。

**Q: 模块找不到？**
A: 确保模块类实现了`IGameModule`接口，并且不是抽象类。模块工厂会自动扫描。

**Q: 如何在运行时切换模块？**
A: 当前不支持运行时热切换，需要重启游戏加载新配置。

**Q: 配置文件加载失败？**
A: 检查JSON格式是否正确，使用在线JSON验证工具检查语法。

## 总结

模块化架构提供了：
- ✅ 灵活的功能组合
- ✅ 清晰的代码边界
- ✅ 便于团队协作
- ✅ 支持多项目复用
- ✅ 简化测试和调试

通过配置文件，你可以快速搭建不同类型的游戏项目，而无需修改代码。
