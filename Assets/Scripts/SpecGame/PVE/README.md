# SpecGame/PVE - 植物大战僵尸特定实现

## 概述
PVE (Plants vs Zombies/Enemies) 层包含植物大战僵尸游戏的特定实现代码，包括植物、僵尸的具体逻辑和数据。

## 目录结构

### Components/
- **PlantComponent.cs** - 植物组件
- **PlantConfigElement.cs** - 植物配置元素
- **ZombieComponent.cs** - 僵尸组件
- **ZombieConfigElement.cs** - 僵尸配置元素
- **SunProducerComponent.cs** - 阳光生产组件

### Systems/
- **PlantAttackSystem.cs** - 植物攻击系统
- **PlantViewSystem.cs** - 植物视图系统
- **ZombieMovementSystem.cs** - 僵尸移动系统
- **ZombieAttackSystem.cs** - 僵尸攻击系统
- **ZombieViewSystem.cs** - 僵尸视图系统
- **ZombieSpawnSystem.cs** - 僵尸生成系统
- **SunProductionSystem.cs** - 阳光生产系统

### Modules/
- **GameModules.cs** - PVZ 游戏模块集合
  - CoreECSModule
  - CombatProjectileModule
  - RenderViewModule
  - UIHealthBarModule
  - AnimationSpineOptimizationModule
  - PVZLevelManagementModule
  - PVZPlantSystemModule
  - PVZZombieSystemModule
  - PVZGameLoopModule
  - DebugMapGridModule

### Config/
- **ViewSystemConfig.cs** - 视图系统配置
- **LevelConfigLoader.cs** - 关卡配置加载器
- **GameConfigLoader.cs** - 游戏配置加载器

### Data/
- **GameConfig.cs** - 游戏配置数据

### UI/
（UI 相关脚本）

### Examples/
- **ServiceExamples.cs** - 服务使用示例
- **ViewLoaderExample.cs** - 视图加载示例
- **ViewModuleExamples.cs** - 视图模块示例
- **EventBusExamples.cs** - 事件总线示例

## 命名空间
- `PVZ.SpecGame.PVE.Components`
- `PVZ.SpecGame.PVE.Systems`
- `PVZ.SpecGame.PVE.Modules`
- `PVZ.SpecGame.PVE.Config`
- `PVZ.SpecGame.PVE.Data`
- `PVZ.SpecGame.PVE.UI`
- `PVZ.SpecGame.PVE.Examples`

## 依赖关系
- 依赖：Framework 层、Common 层、Type/TowerDefense 层
- 被依赖：无（顶层实现）

## 设计原则
- 包含 PVZ 游戏的具体实现
- 复用 TowerDefense 层的通用机制（子弹系统、攻击系统）
- 复用 Common 层的通用组件（视图系统、血条系统）
- 包含游戏特定的数值配置和美术资源引用
