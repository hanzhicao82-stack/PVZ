# 僵尸攻击系统说明文档

## 概述
`ZombieAttackSystem` 实现了僵尸遇到植物时的攻击逻辑。系统使用空间分组优化，确保高效的碰撞检测。

## 核心功能

### 1. 攻击检测
- **范围检测**: 使用 `ATTACK_RANGE = 0.8f` 作为攻击距离
- **空间优化**: 使用 `NativeParallelMultiHashMap<int, Entity>` 按行分组植物
- **距离计算**: 使用 `math.distancesq()` 避免昂贵的平方根运算

### 2. 攻击行为
僵尸遇到植物时的行为流程：
```
检测范围内植物 → 停止移动 → 攻击间隔检查 → 造成伤害 → 植物死亡判定
```

### 3. 移动控制
- **攻击时**: 将 `MovementSpeed` 设为 0，僵尸停止移动
- **无目标时**: 恢复默认移动速度（基于僵尸类型）

## 系统更新顺序
```csharp
[UpdateBefore(typeof(ZombieMovementSystem))]
```
确保在移动系统之前更新，以便正确控制僵尸移动状态。

## 优化策略

### 空间分组 (O(Z + P × P_lane))
```csharp
// 构建植物的行索引
NativeParallelMultiHashMap<int, Entity> plantsByLane;
foreach (plant) plantsByLane.Add(plant.Row, plantEntity);

// 僵尸只检查同一行的植物
foreach (zombie) {
    foreach (plant in plantsByLane[zombie.Lane]) {
        // 碰撞检测和攻击逻辑
    }
}
```

**性能提升**:
- 无优化: 每个僵尸检查所有植物 O(Z × P)
- 有优化: 每个僵尸只检查同行植物 O(Z + P + Z × P_lane)
- 典型场景 (5行): 检测次数减少 **80%**

### 距离计算优化
```csharp
// 使用平方距离，避免 sqrt() 运算
float distanceSq = math.distancesq(zombiePos.xz, plantPos.xz);
if (distanceSq < ATTACK_RANGE_SQ) {
    // 在攻击范围内
}
```
- `math.distancesq()` 比 `math.distance()` 快 **2-3倍**
- 预计算 `ATTACK_RANGE_SQ = 0.64f` 避免运行时平方运算

## 攻击参数

### 僵尸属性 (ZombieComponent)
- `AttackDamage`: 每次攻击伤害值
- `AttackInterval`: 攻击间隔时间（秒）
- `LastAttackTime`: 上次攻击时间戳

### 默认移动速度 (按类型)
| 僵尸类型 | 移动速度 |
|---------|---------|
| Normal | 1.0 |
| ConeHead | 0.8 |
| BucketHead | 0.6 |
| Flag | 1.5 |
| Newspaper | 1.2 |

## 与其他系统的交互

### ZombieMovementSystem
```csharp
// 移动系统检查速度，攻击时不移动
if (zombie.MovementSpeed > 0f) {
    position.x -= zombie.MovementSpeed * deltaTime;
}
```

### CombatSystem
- CombatSystem: 处理子弹击中僵尸
- ZombieAttackSystem: 处理僵尸攻击植物
- 两个系统互不干扰，各自处理不同的伤害来源

## 使用示例

### 配置僵尸攻击属性
```csharp
ZombieComponent zombie = new ZombieComponent
{
    Type = ZombieType.Normal,
    MovementSpeed = 1.0f,
    AttackDamage = 10f,      // 每次攻击10点伤害
    AttackInterval = 1.5f,   // 每1.5秒攻击一次
    LastAttackTime = 0f,
    Lane = 2
};
```

### 攻击流程
1. 僵尸向左移动
2. 检测到攻击范围内有植物
3. 停止移动（速度设为0）
4. 每隔 `AttackInterval` 秒造成 `AttackDamage` 点伤害
5. 植物死亡后继续移动（恢复速度）

## 性能基准

| 单位数量 | 原始检测 | 优化检测 | 提升 |
|---------|---------|---------|------|
| 100单位 | 5,000次 | ~1,000次 | 5x |
| 500单位 | 125,000次 | ~25,000次 | 5x |
| 1000单位 | 500,000次 | ~100,000次 | **5x** |

*假设僵尸和植物各占一半，5行均匀分布*

## 进一步优化建议

### 1. Burst编译
```csharp
[BurstCompile]
public partial struct ZombieAttackSystem : ISystem
```
预计性能提升: **2-5x**

### 2. Job并行化
```csharp
public partial struct ZombieAttackJob : IJobEntity
{
    // 将攻击检测并行化到多个线程
}
```

### 3. 更精细的空间分组
- 当前: 按行分组
- 优化: 按行+列分组（网格分组）
- 预计检测次数再减少 **50-80%**

### 4. 攻击状态组件
```csharp
public struct AttackingComponent : IComponentData
{
    public Entity Target;
}
```
- 避免每帧重新查找攻击目标
- 目标死亡时移除组件

## 调试日志

系统使用 `GameLogger` 记录关键事件（仅编辑器模式）：
```
ZombieAttackSystem: 僵尸攻击植物 Lane=2 伤害=10
ZombieAttackSystem: 植物被摧毁 Lane=2
```

## 注意事项

1. **系统更新顺序**: 必须在 `ZombieMovementSystem` 之前更新
2. **速度恢复**: 使用 `GetDefaultSpeed()` 确保速度正确恢复
3. **实体验证**: 使用 `EntityManager.Exists()` 防止访问已销毁实体
4. **内存管理**: `NativeParallelMultiHashMap` 使用 `Allocator.Temp`，自动在帧结束时释放

## 配置文件支持

僵尸攻击参数可通过 `GameConfig.json` 配置：
```json
{
  "zombies": [
    {
      "type": "Normal",
      "attackDamage": 10,
      "attackInterval": 1.5,
      "movementSpeed": 1.0
    }
  ]
}
```

## 版本历史
- **v1.0** (2025-11-26): 初始实现，包含空间分组优化
