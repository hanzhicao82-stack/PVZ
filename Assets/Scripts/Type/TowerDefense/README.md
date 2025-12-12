# Type/TowerDefense - 塔防类型玩法层

## 概述
TowerDefense 层包含塔防类型游戏的通用代码，这些代码可以在不同的塔防游戏中复用。

## 目录结构

### Components/
- **ProjectileComponent.cs** - 子弹组件
- **ProjectileViewComponent.cs** - 子弹视图组件
- **LevelConfigComponent.cs** - 关卡配置组件

### Systems/
- **ProjectileMovementSystem.cs** - 子弹移动系统
- **ProjectileHitSystem.cs** - 子弹碰撞系统
- **ProjectileViewSystem.cs** - 子弹视图系统
- **ProjectileViewCleanupSystem.cs** - 子弹视图清理系统
- **AttackSystemBase.cs** - 攻击系统基类
- **GameLoopSystem.cs** - 游戏循环系统
- **LevelManagementSystem.cs** - 关卡管理系统

### Modules/
（待添加塔防特定模块）

## 命名空间
- `PVZ.Type.TowerDefense.Components`
- `PVZ.Type.TowerDefense.Systems`
- `PVZ.Type.TowerDefense.Modules`

## 依赖关系
- 依赖：Framework 层、Common 层
- 被依赖：SpecGame 层（特定塔防游戏）

## 设计原则
- 包含塔防游戏通用机制（子弹系统、攻击系统、关卡管理）
- 可配置化设计，允许不同塔防游戏定制参数
- 不包含特定游戏的美术资源或具体数值
