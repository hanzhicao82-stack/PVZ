# 视图系统架构说明

## 架构概览

新的视图系统采用了分层架构，将视图状态更新和具体渲染分离：

```
┌─────────────────────────────────────────┐
│  ZombieViewSystem / PlantViewSystem     │  ← 更新视图状态（动画、颜色）
│  (负责游戏逻辑到视图状态的转换)          │
└────────────────┬────────────────────────┘
                 │
                 ▼
┌────────────────────────────────────────┐
│       ViewSystemBase (基类)            │  ← 通用视图更新逻辑
└────────────┬───────────────────────────┘
             │
     ┌───────┴───────┐
     │               │
     ▼               ▼
┌──────────┐  ┌────────────────┐
│  Spine   │  │ MeshRenderer   │  ← 具体渲染实现
│  View    │  │ View System    │
│  System  │  │                │
└──────────┘  └────────────────┘
```

## 系统组件

### 1. ViewSystemConfig (配置)
**位置**: `Assets/Scripts/Config/ViewSystemConfig.cs`

**功能**:
- 控制启用哪些渲染系统
- 设置默认渲染类型
- 性能配置

**使用**:
```csharp
ViewSystemConfig.Instance.enableSpineSystem = true;
ViewSystemConfig.Instance.enableMeshRendererSystem = true;
ViewSystemConfig.Instance.defaultZombieRenderType = ViewRenderType.Spine;
```

### 2. ViewSystemBase (基类)
**位置**: `Assets/Scripts/Systems/ViewSystemBase.cs`

**功能**:
- 提供通用的视图更新框架
- 定义抽象方法 `UpdateViews()`
- 提供辅助方法（动画名称获取等）

**特点**:
- 抽象基类，不能直接实例化
- 子类必须实现 `UpdateViews()` 方法

### 3. SpineViewSystem (Spine 渲染)
**位置**: `Assets/Scripts/Systems/SpineViewSystem.cs`

**功能**:
- 处理所有使用 Spine 动画的实体
- 更新 Spine 动画状态
- 更新 Spine 骨骼颜色

**查询条件**:
```csharp
.WithAll<SpineRenderComponent, ViewInstanceComponent>()
```

**根据配置启用/禁用**:
```csharp
if (!ViewSystemConfig.Instance.enableSpineSystem)
{
    Enabled = false;
}
```

### 4. MeshRendererViewSystem (MeshRenderer 渲染)
**位置**: `Assets/Scripts/Systems/MeshRendererViewSystem.cs`

**功能**:
- 处理所有使用 MeshRenderer/SpriteRenderer 的实体
- 更新材质颜色
- 支持基于纹理偏移的帧动画

**查询条件**:
```csharp
.WithAll<MeshRenderComponent, ViewInstanceComponent>()
```

### 5. ZombieViewSystem (僵尸视图状态)
**位置**: `Assets/Scripts/Systems/ZombieViewSystem.cs`

**功能**:
- 更新僵尸的 ViewStateComponent
- 根据游戏逻辑决定动画状态（行走/攻击/死亡）
- 计算血量相关的视觉效果（闪烁）

**不负责**: 具体的渲染操作（由 SpineViewSystem 或 MeshRendererViewSystem 处理）

### 6. PlantViewSystem (植物视图状态)
**位置**: `Assets/Scripts/Systems/PlantViewSystem.cs`

**功能**:
- 更新植物的 ViewStateComponent
- 根据游戏逻辑决定动画状态（待机/攻击/生产）
- 计算血量相关的视觉效果

## 工作流程

### 执行顺序
```
1. ZombieViewSystem / PlantViewSystem
   ↓ 更新 ViewStateComponent（动画状态、颜色调制）
   
2. SpineViewSystem (如果启用)
   ↓ 读取 ViewStateComponent，更新 Spine 动画
   
3. MeshRendererViewSystem (如果启用)
   ↓ 读取 ViewStateComponent，更新材质颜色
```

### 数据流
```
GameLogic (ZombieComponent, HealthComponent)
    ↓
ZombieViewSystem 计算
    ↓
ViewStateComponent (CurrentAnimationState, ColorTint)
    ↓
SpineViewSystem / MeshRendererViewSystem 渲染
    ↓
实际视觉呈现 (GameObject)
```

## 使用示例

### 创建带 Spine 视图的僵尸
```csharp
Entity zombie = entityManager.CreateEntity();

// 添加游戏逻辑组件
entityManager.AddComponentData(zombie, new ZombieComponent { ... });
entityManager.AddComponentData(zombie, new HealthComponent { ... });

// 添加视图配置
entityManager.AddComponentData(zombie, new ViewPrefabComponent
{
    PrefabPath = "Prefabs/Zombies/NormalZombie_Spine",
    RenderType = ViewRenderType.Spine
});

// ViewLoaderSystem 会自动：
// 1. 加载预制体
// 2. 添加 SpineRenderComponent
// 3. 添加 ViewInstanceComponent
// 4. 添加 ViewStateComponent

// ZombieViewSystem 会每帧更新 ViewStateComponent
// SpineViewSystem 会读取并应用到 Spine 动画
```

### 创建带 MeshRenderer 视图的植物
```csharp
Entity plant = entityManager.CreateEntity();

// 添加游戏逻辑组件
entityManager.AddComponentData(plant, new PlantComponent { ... });

// 添加视图配置
entityManager.AddComponentData(plant, new ViewPrefabComponent
{
    PrefabPath = "Prefabs/Plants/Peashooter_Mesh",
    RenderType = ViewRenderType.MeshRenderer
});

// 系统会自动处理后续流程
```

### 在运行时切换渲染系统
```csharp
// 禁用 Spine 系统
var spineSystem = World.DefaultGameObjectInjectionWorld
    .GetExistingSystemManaged<SpineViewSystem>();
if (spineSystem != null)
{
    spineSystem.Enabled = false;
}

// 启用 MeshRenderer 系统
var meshSystem = World.DefaultGameObjectInjectionWorld
    .GetExistingSystemManaged<MeshRendererViewSystem>();
if (meshSystem != null)
{
    meshSystem.Enabled = true;
}
```

## 优势

### 1. 关注点分离
- **游戏逻辑** ← ZombieViewSystem/PlantViewSystem → **视图状态**
- **视图状态** ← SpineViewSystem/MeshRendererViewSystem → **实际渲染**

### 2. 灵活性
- 可以独立启用/禁用不同的渲染系统
- 支持混合使用多种渲染方式
- 易于添加新的渲染方式（如 VFX Graph）

### 3. 可维护性
- 代码职责清晰
- 修改渲染逻辑不影响游戏逻辑
- 便于单元测试

### 4. 性能
- 可以根据设备性能选择渲染方式
- 支持按需启用系统
- 查询条件明确，避免无效遍历

## 配置建议

### 高端设备
```csharp
config.enableSpineSystem = true;
config.enableMeshRendererSystem = false;
config.defaultZombieRenderType = ViewRenderType.Spine;
config.defaultPlantRenderType = ViewRenderType.Spine;
```

### 中低端设备
```csharp
config.enableSpineSystem = false;
config.enableMeshRendererSystem = true;
config.defaultZombieRenderType = ViewRenderType.MeshRenderer;
config.defaultPlantRenderType = ViewRenderType.MeshRenderer;
```

### 混合模式
```csharp
config.enableSpineSystem = true;
config.enableMeshRendererSystem = true;
config.defaultZombieRenderType = ViewRenderType.Spine;
config.defaultPlantRenderType = ViewRenderType.MeshRenderer; // 植物用简单渲染
```

## 扩展指南

### 添加新的渲染系统
1. 创建新类继承 `ViewSystemBase`
2. 实现 `UpdateViews()` 方法
3. 添加相应的组件标记（如 `VFXRenderComponent`）
4. 在 `ViewSystemConfig` 中添加控制开关

### 添加新的动画状态
1. 在 `Components/RenderComponents.cs` 中的 `AnimationState` 枚举添加新状态
2. 在对应的 ViewSystem 中处理新状态
3. 在 `GetAnimationName()` 中映射动画名称

## 注意事项

1. **执行顺序**: ZombieViewSystem/PlantViewSystem 应在渲染系统之前执行
2. **组件管理**: ViewInstanceComponent 是托管类型，访问时需要通过 EntityManager
3. **性能**: 避免在每帧创建/销毁 GameObject，使用对象池
4. **配置**: 在游戏启动时加载 ViewSystemConfig，避免运行时频繁访问
