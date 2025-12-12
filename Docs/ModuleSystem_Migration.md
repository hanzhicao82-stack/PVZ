# 从传统启动迁移到模块化启动指南

## 📋 迁移概述

本指南帮助你将现有的PVZ游戏从传统启动方式迁移到模块化架构。

## 🔄 迁移方案

### 方案A: 完全替换（推荐用于新场景）

**适用场景**: 新建关卡、测试场景

**步骤**:

1. **删除旧的启动脚本**
   - 删除或禁用 `GameManagerAuthoring`
   - 删除或禁用其他初始化脚本

2. **添加GameBootstrap**
   ```
   1. 场景中创建GameObject: "GameBootstrap"
   2. 添加组件: GameBootstrap
   3. 拖入配置: Assets/Configs/GameModuleConfig.json
   4. 勾选: Auto Initialize
   ```

3. **运行测试**
   - 点击Play
   - 查看Console确认模块加载成功

### 方案B: 渐进式迁移（推荐用于现有场景）

**适用场景**: 已有复杂配置的游戏场景

**步骤**:

1. **保留原有启动逻辑**
   - 不删除现有GameManager等脚本

2. **添加SceneModuleInitializer**
   ```csharp
   1. 创建GameObject: "ModuleInitializer"
   2. 添加组件: SceneModuleInitializer
   3. 配置:
      - Full Game Mode: 勾选
      - Enable Debug Tools: 勾选
      - Auto Initialize: 勾选
   ```

3. **逐步迁移功能**
   - 先让两套系统共存
   - 逐个验证模块功能
   - 确认无误后禁用旧脚本

### 方案C: 混合模式（用于过渡期）

**适用场景**: 需要同时维护旧新代码的团队

**步骤**:

1. **创建开关控制**
   ```csharp
   public bool useModularSystem = false; // 控制开关
   
   void Awake() {
       if (useModularSystem) {
           // 使用模块化启动
           InitializeModuleSystem();
       } else {
           // 使用传统启动
           InitializeLegacySystem();
       }
   }
   ```

2. **在Inspector中切换**
   - 测试时勾选 `useModularSystem`
   - 生产环境保持旧系统

## 📊 迁移对照表

### 原有系统 → 对应模块

| 原有组件/系统 | 对应模块 | 说明 |
|--------------|---------|------|
| GameManagerAuthoring | core.ecs | ECS World管理 |
| ZombieSpawnSystem | pvz.zombie-system | 僵尸生成 |
| PlantAttackSystem | pvz.plant-system | 植物攻击 |
| ProjectileMovementSystem | combat.projectile | 投射物 |
| ViewLoaderSystem | render.view | 视图加载 |
| HealthBarSystem | ui.health-bar | 血条UI |
| LevelManagementSystem | pvz.level-management | 关卡管理 |
| GameLoopSystem | pvz.game-loop | 游戏循环 |
| SpineOptimizationSystems | animation.spine-optimization | Spine优化 |
| MapGridDebugDrawer | debug.map-grid | 调试网格 |

## 🛠️ 常见场景迁移示例

### 示例1: main.unity 场景

**原有配置**:
- GameManagerAuthoring (手动配置参数)
- LevelConfigLoader
- 各种Authoring脚本

**迁移后**:
```
1. 添加 GameBootstrap
2. 配置文件: GameModuleConfig.json
3. 删除手动配置的参数（现在从配置文件读取）
```

**配置文件示例**:
```json
{
  "projectName": "PVZ Main Level",
  "modules": [
    {"moduleId": "core.ecs", "enabled": true},
    {"moduleId": "pvz.level-management", "enabled": true},
    {"moduleId": "pvz.game-loop", "enabled": true},
    {"moduleId": "pvz.plant-system", "enabled": true},
    {"moduleId": "pvz.zombie-system", "enabled": true},
    {"moduleId": "combat.projectile", "enabled": true},
    {"moduleId": "ui.health-bar", "enabled": true},
    {"moduleId": "render.view", "enabled": true}
  ]
}
```

### 示例2: performanceTest.unity 场景

**原有配置**:
- 只有僵尸生成
- 禁用大部分UI
- 最小化系统

**迁移后**:
```
1. 使用 SceneModuleInitializer
2. 勾选 Performance Test Mode
3. 禁用不需要的模块
```

或使用专门的配置文件:
```json
{
  "projectName": "Performance Test",
  "modules": [
    {"moduleId": "core.ecs", "enabled": true},
    {"moduleId": "render.view", "enabled": true},
    {"moduleId": "pvz.zombie-system", "enabled": true},
    {"moduleId": "animation.spine-optimization", "enabled": true},
    {"moduleId": "ui.health-bar", "enabled": false},
    {"moduleId": "debug.map-grid", "enabled": false}
  ]
}
```

### 示例3: testRender.unity 场景

**原有配置**:
- 只测试渲染
- 无游戏逻辑

**迁移后**:
```csharp
// 使用SceneModuleInitializer自定义配置
public class TestRenderInitializer : SceneModuleInitializer
{
    void Awake()
    {
        // 只启用渲染相关模块
        additionalModules = new[] {
            "core.ecs",
            "render.view",
            "animation.spine-optimization"
        };
        
        // 禁用游戏逻辑
        disabledModules = new[] {
            "pvz.game-loop",
            "pvz.zombie-system",
            "pvz.plant-system"
        };
        
        base.InitializeModuleSystem();
    }
}
```

## ⚠️ 注意事项

### 1. ECS World管理

**问题**: 原有代码可能直接访问 `World.DefaultGameObjectInjectionWorld`

**解决**: 模块系统仍使用相同的World，无需修改

### 2. 系统更新顺序

**问题**: 原有系统可能依赖特定的更新顺序

**解决**: 模块的优先级确保正确的初始化顺序，但ECS系统的Update顺序由Unity管理，不受影响

### 3. Singleton组件

**问题**: 游戏可能有Singleton组件（如GameStateComponent）

**解决**: 
- 模块可以在Initialize中创建Singleton实体
- 或者保持原有的创建方式不变

### 4. 配置参数

**问题**: 原有的GameManagerAuthoring中的参数去哪了？

**解决**:
- 参数移到配置文件的 `globalParameters`
- 或者模块自己管理参数
- 也可以保留Authoring脚本，在模块中读取

## 🔍 验证迁移成功

### 检查清单

- [ ] 场景能正常启动
- [ ] Console输出模块加载日志
- [ ] ECS系统正常工作
- [ ] 僵尸能生成和移动
- [ ] 植物能攻击
- [ ] UI显示正常
- [ ] 性能无明显下降

### 日志检查

正确的启动日志应该包含:
```
====== GameBootstrap 启动 ======
加载配置: Plants vs Zombies DOTS v1.0.0
ECS World 已设置: World
====== 开始注册模块 ======
注册模块: 核心ECS系统 (core.ecs)
...
====== 开始初始化模块系统 ======
✓ 模块 [核心ECS系统] (core.ecs) 初始化成功
...
====== 模块系统初始化完成 (10个模块) ======
====== 游戏初始化完成 ======
```

## 🐛 常见问题排查

### 问题1: 系统没有自动创建

**现象**: 某些ECS System没有运行

**原因**: Unity的ECS系统会自动创建System，模块不需要手动创建

**解决**: 确保System类定义正确，有 `[UpdateInGroup]` 等特性

### 问题2: 依赖关系错误

**现象**: 模块初始化失败，提示依赖未满足

**原因**: 配置文件中某个模块的依赖模块未启用

**解决**: 使用编辑器工具"验证依赖"功能检查

### 问题3: 配置文件加载失败

**现象**: 使用默认配置，不是指定的配置文件

**原因**: JSON格式错误或文件路径错误

**解决**: 
- 使用JSON验证工具检查格式
- 确认文件在Assets目录下
- 检查Inspector中是否正确拖入

## 📈 迁移后的优势

### 对比

| 方面 | 迁移前 | 迁移后 |
|------|--------|--------|
| 启动配置 | 手动在Inspector设置 | JSON配置文件 |
| 功能切换 | 注释代码或删除组件 | 修改配置enabled字段 |
| 测试场景 | 复制场景修改配置 | 创建新配置文件 |
| 团队协作 | 场景文件冲突多 | 只改配置文件 |
| 代码复用 | 困难 | 模块可直接复用 |

## 🎯 下一步

迁移完成后，你可以:

1. **创建更多配置** - 针对不同场景创建专门配置
2. **添加自定义模块** - 将新功能封装为模块
3. **优化配置** - 根据需求精简模块加载
4. **文档化** - 为团队编写模块使用规范

## 📞 获取帮助

- 查看详细文档: `Docs/ModuleSystem_Guide.md`
- 使用编辑器工具: `PVZ > 模块系统管理器`
- 示例配置: `Assets/Configs/GameModuleConfig*.json`

---

**祝迁移顺利！** 🚀
