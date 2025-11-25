# 植物大战僵尸 - Unity DOTS 版本

这是一个使用 Unity DOTS（Data-Oriented Technology Stack）架构实现的植物大战僵尸游戏基础框架。

## 项目结构

### Components（组件）
- **PlantComponent**: 植物基础组件，定义植物类型、攻击力、攻击间隔等属性
- **ZombieComponent**: 僵尸基础组件，定义僵尸类型、移动速度、攻击属性等
- **ProjectileComponent**: 子弹组件，定义子弹的伤害、速度、方向等
- **HealthComponent**: 生命值组件，用于所有具有生命值的实体
- **GridPositionComponent**: 网格位置组件，用于在游戏场地上定位实体
- **SunProducerComponent**: 阳光生产者组件，用于向日葵等生产阳光的植物

### Systems（系统）
- **ZombieMovementSystem**: 处理僵尸向左移动的逻辑
- **ProjectileMovementSystem**: 处理子弹移动和销毁逻辑
- **PlantAttackSystem**: 处理植物发射子弹的逻辑
- **CombatSystem**: 处理子弹碰撞和伤害计算
- **SunProductionSystem**: 处理向日葵生产阳光的逻辑
- **ZombieSpawnSystem**: 定期生成僵尸

### Authoring（创作脚本）
- **PlantAuthoring**: 将 MonoBehaviour 植物转换为 ECS 组件
- **ZombieAuthoring**: 将 MonoBehaviour 僵尸转换为 ECS 组件
- **GameManagerAuthoring**: 游戏管理器，配置全局参数

### Data（数据配置）
- **GameConfig**: 游戏全局配置（网格大小、资源、僵尸生成等）
- **PlantConfig**: 各种植物的预设配置
- **ZombieConfig**: 各种僵尸的预设配置

## 使用方法

1. **创建植物**：在场景中添加一个 GameObject，附加 `PlantAuthoring` 脚本，配置相关属性
2. **创建僵尸**：在场景中添加一个 GameObject，附加 `ZombieAuthoring` 脚本，配置相关属性
3. **游戏管理器**：创建一个空的 GameObject，附加 `GameManagerAuthoring` 脚本，配置游戏参数

## 核心机制

- **ECS 架构**：使用 Unity DOTS 的 ECS（Entity Component System）架构，提高性能
- **数据驱动**：所有游戏逻辑基于组件数据，易于扩展和调整
- **并行处理**：系统自动并行处理，提高大量实体时的性能

## 后续开发

- 添加更多植物类型和特殊能力
- 实现完整的 UI 系统
- 添加关卡系统和波次管理
- 实现阳光收集和资源管理
- 添加音效和视觉效果
- 实现植物种植和拖拽系统

## 注意事项

- 确保项目已安装 Unity Entities 包
- 需要 Unity 2022.3 或更高版本
- 建议使用 Burst Compiler 以获得最佳性能
