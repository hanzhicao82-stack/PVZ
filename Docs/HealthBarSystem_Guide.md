# 血条 HUD UI 系统使用指南

## 概述

血条 HUD UI 系统为所有具有 `HealthComponent` 的实体自动显示头顶血条，包括植物和僵尸。

## 核心组件

### 1. HealthBarComponent
```csharp
public struct HealthBarComponent : IComponentData
{
    public int HealthBarInstanceID;  // 血条 UI 的实例 ID
    public bool IsCreated;           // 是否已创建
    public float YOffset;            // Y 轴偏移
}
```

### 2. HealthBarConfigComponent (可选)
用于自定义单个实体的血条样式（预留接口）。

## 核心系统

### HealthBarManager (MonoBehaviour)
- **单例模式**：自动创建和管理
- **Canvas 管理**：创建 Screen Space Overlay Canvas
- **血条创建**：动态创建血条 UI 预制体
- **血条更新**：实时更新位置和数值

### HealthBarSystem (ECS System)
- **自动检测**：为所有带 `HealthComponent` 的实体创建血条
- **实时更新**：每帧更新血条位置和血量显示
- **自动清理**：实体死亡时销毁血条 UI

## 使用方法

### 1. 自动模式（推荐）

系统会自动为所有带 `HealthComponent` 的实体创建血条：

```csharp
// 创建实体并添加 HealthComponent
Entity entity = entityManager.CreateEntity();
entityManager.AddComponentData(entity, new HealthComponent
{
    CurrentHealth = 100f,
    MaxHealth = 100f,
    IsDead = false
});
entityManager.AddComponentData(entity, LocalTransform.FromPosition(worldPos));

// 血条会自动创建和更新！
```

### 2. 配置血条样式

在场景中添加 `HealthBarManager` 组件并配置：

```csharp
// 通过代码配置
var manager = HealthBarManager.Instance;
manager.defaultWidth = 60f;           // 血条宽度
manager.defaultHeight = 6f;           // 血条高度
manager.defaultYOffset = 2.5f;        // Y 轴偏移
manager.alwaysShowHealthBar = false;  // 满血时隐藏
manager.lowHealthThreshold = 0.3f;    // 低血量阈值（30%）
manager.fullHealthColor = Color.green;
manager.lowHealthColor = Color.red;
```

### 3. 实体类型特定偏移

系统会根据实体类型自动调整血条高度：

- **僵尸**：Y 偏移 2.5
- **植物**：Y 偏移 1.5
- **其他**：使用默认偏移

## 功能特性

### ✅ 已实现功能

1. **自动创建**：检测到新实体时自动创建血条
2. **世界空间转屏幕空间**：血条跟随实体移动
3. **动态颜色**：根据血量百分比改变颜色（绿→黄→红）
4. **视锥剔除**：屏幕外的血条自动隐藏
5. **满血隐藏**：满血时可选隐藏血条
6. **自动清理**：实体死亡时自动销毁血条

### 🎨 颜色系统

- **100% - 30% 血量**：绿色渐变到黄色
- **30% - 0% 血量**：黄色渐变到红色
- **背景色**：半透明深灰色

### 🚫 自动排除

系统会自动排除以下实体：
- `ProjectileComponent`（子弹不显示血条）

## 性能优化

1. **对象池**：血条 UI 复用（未来优化）
2. **批量更新**：使用 ECS Query 批量处理
3. **懒加载**：仅在需要时创建 Canvas
4. **视锥剔除**：屏幕外的血条自动隐藏

## 示例场景

### 植物血条
```csharp
Entity plant = entityManager.CreateEntity();
entityManager.AddComponentData(plant, new PlantComponent { Type = PlantType.Peashooter });
entityManager.AddComponentData(plant, new HealthComponent 
{ 
    CurrentHealth = 300f, 
    MaxHealth = 300f 
});
entityManager.AddComponentData(plant, LocalTransform.FromPosition(new float3(5, 0, 2)));
// 血条自动显示在植物头顶 1.5 单位高度
```

### 僵尸血条
```csharp
Entity zombie = entityManager.CreateEntity();
entityManager.AddComponentData(zombie, new ZombieComponent { Type = ZombieType.Normal });
entityManager.AddComponentData(zombie, new HealthComponent 
{ 
    CurrentHealth = 200f, 
    MaxHealth = 200f 
});
entityManager.AddComponentData(zombie, LocalTransform.FromPosition(new float3(10, 0, 2)));
// 血条自动显示在僵尸头顶 2.5 单位高度
```

## 调试

### 查看血条数量
```csharp
Debug.Log($"当前血条数量: {HealthBarManager.Instance._healthBars.Count}");
```

### 清空所有血条
```csharp
HealthBarManager.Instance.ClearAllHealthBars();
```

### 手动销毁血条
```csharp
// 通过实例 ID
HealthBarManager.Instance.DestroyHealthBar(instanceID);

// 通过 GameObject
HealthBarManager.Instance.DestroyHealthBar(healthBarGameObject);
```

## 已知限制

1. **Canvas 模式**：目前只支持 Screen Space Overlay
2. **血条样式**：统一样式，无法为单个实体自定义（可通过 HealthBarConfigComponent 扩展）
3. **Z 排序**：所有血条在同一层级，可能有重叠

## 未来优化方向

1. ✨ **对象池**：血条 UI 复用
2. ✨ **World Space Canvas**：支持世界空间血条
3. ✨ **自定义样式**：通过 HealthBarConfigComponent 配置
4. ✨ **数字显示**：显示具体血量数值
5. ✨ **动画效果**：受伤时血条抖动/闪烁

## 故障排除

### 问题：血条不显示
**解决方案**：
1. 检查实体是否有 `HealthComponent` 和 `LocalTransform`
2. 确认 Camera.main 存在
3. 检查血条是否在屏幕内

### 问题：血条位置不正确
**解决方案**：
1. 调整 `YOffset` 值
2. 确认实体的 `LocalTransform.Position` 正确
3. 检查相机设置

### 问题：性能下降
**解决方案**：
1. 减少同时显示的实体数量
2. 启用 `alwaysShowHealthBar = false`（满血隐藏）
3. 使用视锥剔除系统（`ViewCullingComponent`）

## 与其他系统集成

### 与 Spine 优化系统配合
```csharp
// 视锥剔除会自动影响血条显示
entityManager.AddComponentData(entity, new ViewCullingComponent
{
    IsVisible = true,
    CullingRadius = 2f
});
```

### 与 PerformanceTestSpawner 配合
血条系统会自动为所有生成的植物和僵尸创建血条。

## 代码示例：完整流程

```csharp
// 1. 创建实体
Entity entity = entityManager.CreateEntity();

// 2. 添加必需组件
entityManager.AddComponentData(entity, new HealthComponent
{
    CurrentHealth = 150f,
    MaxHealth = 200f,
    IsDead = false
});

entityManager.AddComponentData(entity, LocalTransform.FromPosition(new float3(5, 0, 3)));

// 3. 血条自动创建和更新（无需额外代码）

// 4. 实体受伤时，血条自动更新
var health = entityManager.GetComponentData<HealthComponent>(entity);
health.CurrentHealth -= 50f; // 受到 50 点伤害
entityManager.SetComponentData(entity, health);
// 血条会在下一帧自动更新显示

// 5. 实体死亡时，血条自动销毁
health.IsDead = true;
entityManager.SetComponentData(entity, health);
// HealthBarSystem 会自动清理血条 UI
```

## 总结

血条 HUD UI 系统提供了开箱即用的头顶血条功能，无需手动管理 UI 生命周期。系统会自动检测、创建、更新和清理血条，与 ECS 架构完美集成。
