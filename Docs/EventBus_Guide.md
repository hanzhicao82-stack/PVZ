# 事件总线系统使用指南

## 📖 概述

事件总线是模块间通信的核心机制，提供松耦合的发布/订阅模式，让不同模块可以通过事件进行通信而无需直接依赖。

## 🎯 设计理念

### 为什么需要事件总线？

**问题**：没有事件总线时
```csharp
// UI模块需要直接依赖游戏逻辑模块
public class ScoreUI
{
    private ZombieSystem _zombieSystem; // 强耦合
    
    void Update()
    {
        // 轮询检查僵尸状态
        if (_zombieSystem.HasZombieDied())
        {
            UpdateScore();
        }
    }
}
```

**解决方案**：使用事件总线
```csharp
// UI模块只需监听事件，无需依赖具体实现
public class ScoreUI
{
    void Initialize()
    {
        _eventBus.Subscribe<ZombieDeathEvent>(OnZombieDeath);
    }
    
    void OnZombieDeath(ZombieDeathEvent evt)
    {
        UpdateScore(evt.ScoreValue);
    }
}
```

## 🚀 快速开始

### 1. 启用事件总线模块

在配置文件中添加：
```json
{
  "modules": [
    {
      "moduleId": "core.event-bus",
      "enabled": true
    }
  ]
}
```

### 2. 在模块中获取事件总线

```csharp
public class MyModule : GameModuleBase
{
    private IEventBus _eventBus;
    
    protected override void OnInitialize()
    {
        _eventBus = Context.GetService<IEventBus>();
    }
}
```

### 3. 订阅事件

```csharp
_eventBus.Subscribe<ZombieDeathEvent>(OnZombieDeath);

void OnZombieDeath(ZombieDeathEvent evt)
{
    Debug.Log($"僵尸死亡: {evt.ZombieType}");
}
```

### 4. 发布事件

```csharp
_eventBus.Publish(new ZombieDeathEvent
{
    ZombieEntity = entity,
    ZombieType = "Normal",
    Position = position,
    ScoreValue = 100
});
```

## 📋 预定义事件列表

### 游戏生命周期
- `GameStartedEvent` - 游戏开始
- `GamePausedEvent` - 游戏暂停
- `GameEndedEvent` - 游戏结束
- `LevelCompletedEvent` - 关卡完成

### 波次相关
- `WaveStartedEvent` - 波次开始
- `WaveCompletedEvent` - 波次完成
- `HugeWaveIncomingEvent` - 大波僵尸来袭

### 僵尸相关
- `ZombieSpawnedEvent` - 僵尸生成
- `ZombieDamagedEvent` - 僵尸受伤
- `ZombieDeathEvent` - 僵尸死亡
- `ZombieReachedEndEvent` - 僵尸到达终点
- `ZombieEnterAttackRangeEvent` - 进入攻击范围

### 植物相关
- `PlantPlacedEvent` - 植物种植
- `PlantRemovedEvent` - 植物移除
- `PlantAttackEvent` - 植物攻击
- `PlantDamagedEvent` - 植物受伤
- `PlantDeathEvent` - 植物死亡

### 投射物相关
- `ProjectileFiredEvent` - 投射物发射
- `ProjectileHitEvent` - 投射物命中

### 资源相关
- `SunProducedEvent` - 阳光生产
- `SunCollectedEvent` - 阳光收集
- `SunSpentEvent` - 阳光消耗

### UI相关
- `PlantCardSelectedEvent` - 卡片选择
- `ShowMessageEvent` - 显示消息

### 音效相关
- `PlaySoundEvent` - 播放音效
- `PlayMusicEvent` - 播放音乐

### 特效相关
- `PlayEffectEvent` - 播放特效

## 💡 使用模式

### 模式1: 一对多通知

```csharp
// 僵尸死亡时，通知多个系统
_eventBus.Publish(new ZombieDeathEvent { ... });

// UI系统监听 → 更新分数
// 音效系统监听 → 播放音效
// 成就系统监听 → 检查成就
// 统计系统监听 → 更新数据
```

### 模式2: 链式反应

```csharp
// 订阅波次完成事件
_eventBus.Subscribe<WaveCompletedEvent>(evt =>
{
    if (evt.IsFinalWave)
    {
        // 触发关卡完成事件
        _eventBus.Publish(new LevelCompletedEvent { ... });
    }
});

// 订阅关卡完成事件
_eventBus.Subscribe<LevelCompletedEvent>(evt =>
{
    // 触发显示胜利界面事件
    _eventBus.Publish(new ShowMessageEvent { ... });
});
```

### 模式3: 延迟处理

```csharp
// 在敏感操作中使用延迟发布
foreach (var zombie in zombies)
{
    if (zombie.ShouldDie())
    {
        // 延迟到下一帧处理，避免在迭代中修改集合
        _eventBus.PublishDeferred(new ZombieDeathEvent { ... });
    }
}
```

### 模式4: 条件订阅

```csharp
void OnGameStarted(GameStartedEvent evt)
{
    if (evt.LevelId > 10)
    {
        // 只在高级关卡中启用特殊机制
        _eventBus.Subscribe<SpecialEvent>(OnSpecialEvent);
    }
}
```

## 🏗️ 实际应用场景

### 场景1: 僵尸死亡处理

```csharp
// ZombieSystem 发布事件
public partial class ZombieDeathSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (health, entity) in 
            SystemAPI.Query<RefRO<HealthComponent>>()
                .WithEntityAccess())
        {
            if (health.ValueRO.CurrentHealth <= 0)
            {
                _eventBus.Publish(new ZombieDeathEvent
                {
                    ZombieEntity = entity,
                    ZombieType = "Normal",
                    ScoreValue = 100
                });
                
                EntityManager.DestroyEntity(entity);
            }
        }
    }
}

// ScoreUI 订阅事件
public class ScoreUI : MonoBehaviour
{
    private int _totalScore;
    
    void Start()
    {
        var eventBus = GetEventBus();
        eventBus.Subscribe<ZombieDeathEvent>(OnZombieDeath);
    }
    
    void OnZombieDeath(ZombieDeathEvent evt)
    {
        _totalScore += evt.ScoreValue;
        UpdateScoreDisplay();
    }
}

// AudioSystem 订阅事件
public class AudioSystem
{
    void Initialize()
    {
        _eventBus.Subscribe<ZombieDeathEvent>(evt =>
        {
            PlaySound("zombie_death", evt.Position);
        });
    }
}
```

### 场景2: 阳光收集流程

```csharp
// 1. 阳光生产
_eventBus.Publish(new SunProducedEvent
{
    SourceEntity = sunflowerEntity,
    SunAmount = 25,
    Position = position,
    IsFromSky = false
});

// 2. 玩家点击收集
_eventBus.Publish(new SunCollectedEvent
{
    SunAmount = 25,
    TotalSun = 175,
    CollectionPosition = clickPosition
});

// 3. UI更新监听
_eventBus.Subscribe<SunCollectedEvent>(evt =>
{
    sunText.text = evt.TotalSun.ToString();
    PlayCollectionAnimation(evt.CollectionPosition);
});

// 4. 种植消耗
_eventBus.Publish(new SunSpentEvent
{
    SunAmount = 100,
    RemainingSun = 75,
    SpentOn = "Peashooter"
});
```

### 场景3: 波次推进

```csharp
// 游戏开始
_eventBus.Publish(new GameStartedEvent
{
    LevelId = 1,
    LevelName = "白天1-1"
});

// 第一波开始
_eventBus.Publish(new WaveStartedEvent
{
    WaveNumber = 1,
    TotalWaves = 5,
    ZombieCount = 10
});

// 波次完成
_eventBus.Publish(new WaveCompletedEvent
{
    WaveNumber = 1,
    TotalWaves = 5,
    IsFinalWave = false
});

// 最终波
_eventBus.Publish(new HugeWaveIncomingEvent
{
    WaveNumber = 5,
    PrepareTime = 3f
});
```

## ⚠️ 注意事项

### 1. 避免内存泄漏

```csharp
❌ 错误：忘记取消订阅
public class MyComponent : MonoBehaviour
{
    void Start()
    {
        _eventBus.Subscribe<GameEvent>(OnGameEvent);
    }
    // 组件销毁时没有取消订阅 → 内存泄漏！
}

✅ 正确：及时取消订阅
public class MyComponent : MonoBehaviour
{
    void Start()
    {
        _eventBus.Subscribe<GameEvent>(OnGameEvent);
    }
    
    void OnDestroy()
    {
        _eventBus?.Unsubscribe<GameEvent>(OnGameEvent);
    }
}
```

### 2. 避免事件风暴

```csharp
❌ 错误：在Update中频繁发布事件
void Update()
{
    _eventBus.Publish(new PositionUpdateEvent { ... }); // 每帧触发！
}

✅ 正确：只在变化时发布
void Update()
{
    if (HasPositionChanged())
    {
        _eventBus.Publish(new PositionUpdateEvent { ... });
    }
}
```

### 3. 避免循环依赖

```csharp
❌ 错误：事件触发事件形成循环
_eventBus.Subscribe<EventA>(evt =>
{
    _eventBus.Publish(new EventB());
});

_eventBus.Subscribe<EventB>(evt =>
{
    _eventBus.Publish(new EventA()); // 无限循环！
});
```

### 4. 异常处理

```csharp
// 事件处理器中的异常不会影响其他订阅者
_eventBus.Subscribe<GameEvent>(evt =>
{
    throw new Exception("错误"); // 会被捕获并记录
});

_eventBus.Subscribe<GameEvent>(evt =>
{
    Debug.Log("仍然会执行"); // 正常执行
});
```

## 🔧 高级用法

### 创建自定义事件

```csharp
public class CustomBossEvent : GameEventBase
{
    public string BossName;
    public float BossHealth;
    public int Phase;
}

// 使用
_eventBus.Subscribe<CustomBossEvent>(evt =>
{
    Debug.Log($"Boss: {evt.BossName}, 阶段: {evt.Phase}");
});
```

### 事件过滤

```csharp
_eventBus.Subscribe<ZombieDeathEvent>(evt =>
{
    // 只处理特定类型的僵尸
    if (evt.ZombieType == "BossZombie")
    {
        TriggerSpecialEffect();
    }
});
```

### 事件统计

```csharp
// 启用详细日志查看事件流
// 在配置文件中设置: "eventbus.verbose": "true"

// 或在代码中
var eventBus = Context.GetService<EventBusService>();
eventBus.PrintStatistics();
```

## 📊 性能考虑

- **立即发布** (`Publish`): 零开销，直接调用订阅者
- **延迟发布** (`PublishDeferred`): 入队操作，下一帧批处理
- **订阅**: O(1) 操作
- **取消订阅**: O(n) 操作（n为订阅者数量）

**建议**：
- 高频事件（每帧多次）→ 考虑直接调用或ECS组件
- 中频事件（秒级）→ 使用事件总线 ✅
- 低频事件（分钟级）→ 使用事件总线 ✅

## 📚 完整示例

参考文件: `Assets/Scripts/Game/Examples/EventBusExamples.cs`

---

**版本**: 1.0.0  
**更新日期**: 2025-12-10
