# Plants vs Zombies - Unity DOTS

基于 Unity DOTS（Data-Oriented Technology Stack）架构实现的植物大战僵尸游戏。

## 项目特点

- **ECS 架构**：使用 Unity Entities 实现高性能游戏逻辑
- **数据驱动**：通过 JSON 配置文件管理游戏参数
- **可视化调试**：Gizmos 实时显示游戏对象和状态
- **模块化设计**：组件、系统、配置分离

## 项目结构

```
Assets/
├── Configs/
│   └── GameConfig.json          # 游戏配置文件
├── Scripts/
│   ├── Components/              # ECS 组件
│   │   ├── PlantComponent.cs
│   │   ├── ZombieComponent.cs
│   │   ├── ProjectileComponent.cs
│   │   ├── HealthComponent.cs
│   │   ├── GridPositionComponent.cs
│   │   ├── SunProducerComponent.cs
│   │   ├── GameConfigComponent.cs
│   │   ├── PlantConfigElement.cs
│   │   └── ZombieConfigElement.cs
│   ├── Systems/                 # ECS 系统
│   │   ├── ZombieMovementSystem.cs
│   │   ├── ProjectileMovementSystem.cs
│   │   ├── PlantAttackSystem.cs
│   │   ├── CombatSystem.cs
│   │   ├── SunProductionSystem.cs
│   │   └── ZombieSpawnSystem.cs
│   ├── Authoring/               # GameObject 到 ECS 转换
│   │   ├── PlantAuthoring.cs
│   │   ├── ZombieAuthoring.cs
│   │   └── GameManagerAuthoring.cs
│   ├── Config/                  # 配置加载
│   │   └── GameConfigLoader.cs
│   ├── Debug/                   # 调试工具
│   │   ├── GameDebugSystem.cs
│   │   ├── GameDebugDrawer.cs
│   │   └── GameTestSetup.cs
│   └── Data/
│       └── GameConfig.cs
```

## 核心功能

### 已实现
- ✅ 植物系统（豌豆射手、向日葵、坚果墙、寒冰射手、双发射手）
- ✅ 僵尸系统（普通僵尸、路障僵尸、铁桶僵尸、旗帜僵尸）
- ✅ 战斗系统（子弹发射、碰撞检测、伤害计算）
- ✅ 僵尸自动生成系统
- ✅ JSON 配置系统
- ✅ Gizmos 可视化调试
- ✅ 快速测试工具

### 待实现
- ⏳ UI 系统
- ⏳ 阳光收集系统
- ⏳ 植物种植系统
- ⏳ 关卡系统
- ⏳ 音效和视觉效果

## 快速开始

### 环境要求
- Unity 2022.3 或更高版本
- Unity Entities 包

### 测试运行

1. **创建测试场景**
   - 在场景中创建空 GameObject，命名为 "GameDebug"
   - 添加 `GameDebugDrawer` 组件
   - 添加 `GameTestSetup` 组件

2. **加载配置**
   - 创建另一个空 GameObject，命名为 "GameConfig"
   - 添加 `GameConfigLoader` 组件
   - 可选：在 Inspector 中引用 `GameConfig.json`

3. **运行游戏**
   - 按 Play 运行
   - 在 Scene 视图中查看 Gizmos 绘制的对象

### 快捷键
- **F1** - 设置测试场景（生成植物和僵尸）
- **F2** - 清除所有实体
- **F3** - 只生成测试植物
- **F4** - 只生成测试僵尸

## 配置文件

编辑 `Assets/Configs/GameConfig.json` 可调整游戏参数：

```json
{
  "zombieSpawn": {
    "interval": 5.0,
    "startDelay": 0.0,
    "laneCount": 5,
    "spawnX": 15.0,
    "laneZSpacing": 2.0,
    "laneZOffset": -4.0
  },
  "plants": [
    {
      "type": "Peashooter",
      "sunCost": 100,
      "attackDamage": 20.0,
      "attackInterval": 1.5,
      "attackRange": 10.0,
      "health": 100.0
    }
  ],
  "zombies": [
    {
      "type": "Normal",
      "movementSpeed": 1.0,
      "attackDamage": 10.0,
      "attackInterval": 1.0,
      "health": 100.0
    }
  ]
}
```

## 开发说明

### ECS 架构
- 所有游戏逻辑基于组件数据
- 系统自动并行处理
- 使用 Burst Compiler 获得最佳性能

### 扩展指南
1. 添加新植物类型：在 `PlantType` 枚举中添加，并在 JSON 中配置
2. 添加新僵尸类型：在 `ZombieType` 枚举中添加，并在 JSON 中配置
3. 添加新系统：继承 `ISystem` 并实现 `OnUpdate` 方法

## 许可证

MIT License

## 贡献

欢迎提交 Issue 和 Pull Request！
