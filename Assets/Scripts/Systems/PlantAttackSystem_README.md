# 植物攻击系统重构说明

## 重构目标

将攻击逻辑与视图显示逻辑解耦，通过攻击状态组件建立明确的数据驱动关系。

## 架构设计

### 1. 核心组件

#### AttackStateComponent（新增）
```csharp
public struct AttackStateComponent : IComponentData
{
    public float AttackStartTime;           // 攻击开始时间
    public float AttackAnimationDuration;   // 攻击动画持续时间
    public bool HasDealtDamage;            // 是否已发射子弹/造成伤害
}
```

**作用**：
- 标记实体正在执行攻击动作
- 携带攻击状态的关键时间信息
- 作为攻击系统和视图系统之间的桥梁

### 2. 系统流程

```
┌──────────────────────────────────────────────────────────┐
│              PlantAttackSystem (攻击逻辑)                  │
├──────────────────────────────────────────────────────────┤
│                                                            │
│  第一阶段：检测攻击条件                                      │
│  ┌───────────────────────────────────────────┐            │
│  │ 1. 检查冷却时间                             │            │
│  │ 2. 检查是否有目标（同行僵尸）                 │            │
│  │ 3. 添加 AttackStateComponent               │            │
│  │ 4. 记录攻击开始时间                          │            │
│  └───────────────────────────────────────────┘            │
│                                                            │
│  第二阶段：执行攻击动作                                      │
│  ┌───────────────────────────────────────────┐            │
│  │ 1. 在动画特定时刻（40%）发射子弹             │            │
│  │ 2. 标记已造成伤害                            │            │
│  │ 3. 动画结束后移除 AttackStateComponent      │            │
│  └───────────────────────────────────────────┘            │
│                                                            │
└──────────────────────────────────────────────────────────┘
                            │
                            │ AttackStateComponent
                            ▼
┌──────────────────────────────────────────────────────────┐
│              PlantViewSystem (视图逻辑)                     │
├──────────────────────────────────────────────────────────┤
│                                                            │
│  根据组件状态决定动画                                        │
│  ┌───────────────────────────────────────────┐            │
│  │ if (HasComponent<AttackStateComponent>)   │            │
│  │     显示攻击动画                             │            │
│  │ else if (Type == Sunflower)               │            │
│  │     显示生产动画                             │            │
│  │ else                                       │            │
│  │     显示待机动画                             │            │
│  └───────────────────────────────────────────┘            │
│                                                            │
└──────────────────────────────────────────────────────────┘
```

## 关键改进

### 1. 明确的状态驱动
**之前**：
```csharp
// PlantViewSystem 需要推断攻击状态
float timeSinceLastAttack = currentTime - plant.LastAttackTime;
if (timeSinceLastAttack < 0.3f)  // 硬编码的时间窗口
    targetAnimationState = AnimationState.Attack;
```

**之后**：
```csharp
// 直接检查攻击状态组件
bool isAttacking = SystemAPI.HasComponent<AttackStateComponent>(entity);
if (isAttacking)
    targetAnimationState = AnimationState.Attack;
```

**优势**：
- ✅ 视图系统不需要了解攻击逻辑的细节
- ✅ 不依赖硬编码的时间窗口
- ✅ 攻击动画持续时间由攻击系统控制

### 2. 精确的动画时序控制
```csharp
// 攻击动画的 40% 时刻发射子弹
float damageTimingPercent = 0.4f;
float damageTime = attackState.ValueRO.AttackAnimationDuration * damageTimingPercent;

if (!attackState.ValueRO.HasDealtDamage && timeSinceAttackStart >= damageTime)
{
    SpawnProjectile(...);
    attackState.ValueRW.HasDealtDamage = true;
}
```

**优势**：
- ✅ 子弹发射时机与动画同步
- ✅ 避免重复发射子弹
- ✅ 支持不同植物有不同的攻击时序

### 3. 可配置的动画持续时间
```csharp
private float GetAttackAnimationDuration(PlantType plantType)
{
    return plantType switch
    {
        PlantType.Peashooter => 0.5f,   // 豌豆射手
        PlantType.SnowPea => 0.5f,      // 寒冰射手
        PlantType.Repeater => 0.8f,     // 双发射手（发射两次）
        PlantType.CherryBomb => 1.0f,   // 樱桃炸弹
        _ => 0.5f
    };
}
```

**优势**：
- ✅ 每种植物可以有独特的攻击节奏
- ✅ 易于调整和平衡游戏性
- ✅ 支持复杂的多段攻击（如双发射手）

## 系统职责

### PlantAttackSystem
- ✅ 检测攻击条件（目标、冷却）
- ✅ 管理攻击状态（添加/移除 AttackStateComponent）
- ✅ 控制攻击时序（何时发射子弹）
- ✅ 生成子弹实体
- ❌ 不处理任何视图相关的逻辑

### PlantViewSystem
- ✅ 根据 AttackStateComponent 决定显示的动画
- ✅ 更新视图状态组件（ViewStateComponent）
- ✅ 处理血量相关的视觉效果
- ❌ 不了解攻击逻辑的实现细节
- ❌ 不直接触发攻击行为

## 数据流示例

### 豌豆射手攻击流程

```
时间线：
─────────────────────────────────────────────────────────
t=0.0s  │ 检测到僵尸
        │ → 添加 AttackStateComponent
        │   { StartTime=0.0, Duration=0.5, HasDealtDamage=false }
        │ → PlantViewSystem 检测到攻击状态
        │ → 切换到攻击动画
        │
t=0.2s  │ 攻击动画进行到 40% (0.5s × 0.4 = 0.2s)
        │ → 发射豌豆子弹
        │ → 标记 HasDealtDamage = true
        │
t=0.5s  │ 攻击动画结束
        │ → 移除 AttackStateComponent
        │ → PlantViewSystem 检测到攻击状态消失
        │ → 切换回待机动画
        │
t=1.5s  │ 冷却结束（AttackInterval = 1.5s）
        │ 如果仍有僵尸，重新开始攻击循环
─────────────────────────────────────────────────────────
```

## 扩展性

### 支持多段攻击
```csharp
// 双发射手：在 30% 和 70% 时各发射一次
if (plant.Type == PlantType.Repeater)
{
    if (!attackState.FirstShotFired && progress >= 0.3f)
    {
        SpawnProjectile(...);
        attackState.FirstShotFired = true;
    }
    if (!attackState.SecondShotFired && progress >= 0.7f)
    {
        SpawnProjectile(...);
        attackState.SecondShotFired = true;
    }
}
```

### 支持蓄力攻击
```csharp
// 玉米加农炮：需要蓄力
public struct ChargingAttackComponent : IComponentData
{
    public float ChargeStartTime;
    public float ChargeRequiredDuration;
    public float ChargeProgress;  // 0.0 ~ 1.0
}
```

### 支持攻击中断
```csharp
// 如果植物受到攻击，可以中断当前攻击
if (SystemAPI.HasComponent<StunnedComponent>(entity))
{
    ecb.RemoveComponent<AttackStateComponent>(entity);
}
```

## 性能优化

### 查询分离
```csharp
// 未攻击的植物（检测条件）
.WithNone<AttackStateComponent>()

// 正在攻击的植物（执行攻击）
.WithAll<AttackStateComponent>()
```

**优势**：
- 减少不必要的条件检查
- 每个植物只处理相关的逻辑分支
- ECS 查询优化更高效

## 总结

通过引入 **AttackStateComponent**，实现了：

1. **逻辑解耦**：攻击系统和视图系统职责明确，互不干扰
2. **状态清晰**：攻击状态由明确的组件表示，而非时间推断
3. **时序精确**：攻击动画、子弹发射、状态清理时机可控
4. **易于扩展**：支持复杂的攻击模式和特殊机制
5. **性能优化**：基于组件的查询更高效

这是一个标准的 **数据驱动设计** 模式，符合 ECS 架构的最佳实践。
