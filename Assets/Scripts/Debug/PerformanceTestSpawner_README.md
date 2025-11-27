# PerformanceTestSpawner 使用说明

## 新增功能：视图模型加载和显示

### 配置项说明

#### 视图模型配置 (View Model Configuration)

1. **Enable View Loading** (启用视图加载)
   - 类型：`bool`
   - 默认值：`true`
   - 说明：控制是否为生成的实体加载和显示视图模型
   - 用途：性能测试时可以关闭视图加载，只测试纯ECS逻辑性能

2. **Mesh Prefab Path** (Mesh预制体路径)
   - 类型：`string`
   - 默认值：`"Prefabs/TestMesh"`
   - 说明：MeshRenderer类型预制体在Resources文件夹下的相对路径
   - 要求：预制体必须包含 `MeshRenderer` 或 `SpriteRenderer` 组件

3. **Spine Prefab Path** (Spine预制体路径)
   - 类型：`string`
   - 默认值：`"Prefabs/TestSpine"`
   - 说明：Spine动画预制体在Resources文件夹下的相对路径
   - 要求：预制体必须包含 `Spine.Unity.SkeletonAnimation` 组件

4. **Use Spine Model** (使用Spine模型)
   - 类型：`bool`
   - 默认值：`true`
   - 说明：选择使用哪种类型的模型
   - `true` = 使用 Spine 预制体
   - `false` = 使用 Mesh 预制体

### 使用步骤

#### 1. 准备预制体

在 Unity 项目中创建 Resources 文件夹结构：

```
Assets/
  Resources/
    Prefabs/
      TestMesh.prefab      (MeshRenderer模型)
      TestSpine.prefab     (Spine骨骼动画)
```

**Mesh 预制体要求**：
- 包含 `MeshRenderer` 组件，或
- 包含 `SpriteRenderer` 组件

**Spine 预制体要求**：
- 包含 `Spine.Unity.SkeletonAnimation` 组件
- 已配置好骨骼动画

#### 2. 配置 PerformanceTestSpawner

在 Unity Inspector 中配置：

```
视图模型配置:
  ✓ Enable View Loading        (勾选以启用)
  Mesh Prefab Path:  "Prefabs/TestMesh"
  Spine Prefab Path: "Prefabs/TestSpine"
  ✓ Use Spine Model            (勾选使用Spine，取消使用Mesh)
```

#### 3. 运行测试

- **测试 Spine 模型**：
  1. 勾选 `Use Spine Model`
  2. 确保 `Spine Prefab Path` 正确
  3. 勾选 `Enable View Loading`
  4. 运行场景

- **测试 Mesh 模型**：
  1. 取消勾选 `Use Spine Model`
  2. 确保 `Mesh Prefab Path` 正确
  3. 勾选 `Enable View Loading`
  4. 运行场景

- **测试纯逻辑性能（不加载模型）**：
  1. 取消勾选 `Enable View Loading`
  2. 运行场景

### 工作原理

当 `Enable View Loading` 启用时：

1. **实体创建**：PerformanceTestSpawner 创建植物或僵尸实体
2. **添加组件**：自动添加 `ViewPrefabComponent` 到实体
3. **系统加载**：`ViewLoaderSystem` 自动检测并加载预制体
4. **类型识别**：系统根据预制体上的组件自动识别渲染类型
5. **标记添加**：自动添加 `SpineRenderComponent` 或 `MeshRenderComponent`
6. **视图渲染**：相应的渲染系统（SpineViewSystem 或 MeshRendererViewSystem）处理显示

### 代码示例

生成的实体会包含以下组件：

```csharp
// 植物实体
Entity plantEntity = entityManager.CreateEntity();

// 游戏逻辑组件
entityManager.AddComponentData(plantEntity, new PlantComponent { ... });
entityManager.AddComponentData(plantEntity, new HealthComponent { ... });
entityManager.AddComponentData(plantEntity, new GridPositionComponent { ... });
entityManager.AddComponentData(plantEntity, LocalTransform.FromPosition(...));

// 视图组件（当 enableViewLoading = true 时自动添加）
entityManager.AddComponentData(plantEntity, new ViewPrefabComponent
{
    PrefabPath = "Prefabs/TestSpine",  // 或 "Prefabs/TestMesh"
    IsViewLoaded = false
});
```

### 性能对比测试建议

#### 测试1：纯ECS逻辑性能
```
Enable Auto Spawn: ✓
Enable View Loading: ✗
Max Plants: 1000
Max Zombies: 500
```

#### 测试2：Mesh渲染性能
```
Enable Auto Spawn: ✓
Enable View Loading: ✓
Use Spine Model: ✗
Max Plants: 1000
Max Zombies: 500
```

#### 测试3：Spine渲染性能
```
Enable Auto Spawn: ✓
Enable View Loading: ✓
Use Spine Model: ✓
Max Plants: 500
Max Zombies: 250
```

### 注意事项

1. **预制体路径**：必须是相对于 Resources 文件夹的路径，不需要加 `.prefab` 扩展名
2. **预制体检查**：确保预制体包含必需的渲染组件（SkeletonAnimation/MeshRenderer/SpriteRenderer）
3. **性能影响**：大量实体加载视图会显著影响性能，建议根据设备调整数量
4. **系统启用**：确保 `ViewLoaderSystem`、`SpineViewSystem` 或 `MeshRendererViewSystem` 已启用
5. **配置文件**：可以在 `ViewSystemConfig` 中全局控制渲染系统的启用状态

### 调试信息

系统会在 Console 中输出加载日志：

```
ViewLoaderSystem: 加载 Spine 视图: Prefabs/TestSpine
ViewLoaderSystem: 加载 MeshRenderer 视图: Prefabs/TestMesh
SpineViewSystem is enabled.
MeshRendererViewSystem is enabled.
```

如果出现警告：
```
ViewLoaderSystem: 预制体 Prefabs/TestXXX 缺少任何可识别的渲染组件
```
请检查预制体是否包含正确的组件。

### 常见问题

**Q: 预制体加载失败？**
A: 检查路径是否正确，确保预制体在 `Assets/Resources/` 下

**Q: 模型不显示？**
A: 检查 `Enable View Loading` 是否勾选，查看 Console 是否有错误日志

**Q: 性能太低？**
A: 减少 `Max Plants` 和 `Max Zombies` 数量，或关闭 `Enable View Loading` 只测试逻辑

**Q: 如何切换模型类型？**
A: 直接勾选或取消勾选 `Use Spine Model` 即可，无需重启

### 扩展用途

此功能不仅用于性能测试，还可以用于：

1. **视图系统测试**：验证 ViewLoaderSystem 的正确性
2. **渲染对比**：对比 Spine 和 MeshRenderer 的视觉效果
3. **资源管理测试**：测试大量预制体加载和销毁的内存管理
4. **多样化显示**：可以为不同类型的实体配置不同的预制体路径
