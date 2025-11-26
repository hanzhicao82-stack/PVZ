# 地图网格可视化指南

本文档介绍如何使用Gizmo系统在Unity Scene视图中可视化地图网格。

## 功能概述

创建了两个调试工具脚本来可视化游戏世界：

### 1. MapGridDebugDrawer - 地图网格绘制器

**功能：**
- 根据关卡配置（LevelConfigComponent）自动绘制地图网格
- 使用Gizmos在Scene视图中绘制行列网格线
- 显示格子填充、行列索引、地图边界标记
- 实时显示地图配置信息（行数、列数、格子大小等）

**可视化元素：**
- 绿色半透明格子填充：显示每个网格单元
- 绿色网格线：行列分隔线
- 黄色球体：地图四角标记点
- 红色球体：地图中心点标记
- 行列标签：R0, R1...（行） C0, C1...（列）

**参数设置：**
```csharp
enableGridDrawing = true;          // 启用网格绘制
gridColor = 绿色半透明;              // 网格线颜色
cellColor = 绿色极透明;              // 格子填充颜色
showCellFill = true;               // 显示格子填充
showRowColumnIndex = true;         // 显示行列索引
lineWidth = 2f;                    // 网格线宽度
mapOffset = (0f, 0, 0);        // 地图起始位置偏移
```

### 2. EntityPositionDebugDrawer - 实体位置绘制器

**功能：**
- 绘制场景中所有植物、僵尸、抛射物的实时位置
- 显示实体类型、格子坐标、移动方向
- 统计并显示各类实体数量

**可视化元素：**
- 植物（绿色）：
  - 线框球体 + 立方体顶部标记
  - 显示格子坐标 "P(行,列)"
  - 显示植物类型
  
- 僵尸（红色）：
  - 线框立方体（高度为宽度2倍）
  - 移动方向指示线（向左）
  - 显示僵尸类型 "Z(类型)"
  
- 抛射物（黄色）：
  - 实心小球体
  - 移动轨迹指示线

**参数设置：**
```csharp
showPlants = true;                 // 显示植物
showZombies = true;                // 显示僵尸
showProjectiles = true;            // 显示抛射物
plantColor = Color.green;          // 植物颜色
zombieColor = Color.red;           // 僵尸颜色
projectileColor = Color.yellow;    // 抛射物颜色
plantSize = 0.5f;                  // 植物绘制尺寸
zombieSize = 0.5f;                 // 僵尸绘制尺寸
projectileSize = 0.1f;             // 抛射物绘制尺寸
showGridCoordinates = true;        // 显示格子坐标
```

## 使用方法

### 自动创建（推荐）

场景中的 `GameManager` 对象上的 `SceneInitializer` 脚本会自动创建这些调试工具：

1. 打开 main.unity 场景
2. 选择 GameManager 对象
3. 在 Inspector 中找到 SceneInitializer 组件
4. 确保以下选项勾选：
   - `Auto Create Map Grid Debugger = true`
   - `Auto Create Entity Debugger = true`
5. 运行场景，调试工具会自动创建

### 手动创建

如果需要手动添加调试工具：

#### 添加地图网格绘制器：
1. 在 Hierarchy 中右键 > Create Empty
2. 命名为 "MapGridDebugger"
3. 添加组件：`PVZ.DOTS.Debug.MapGridDebugDrawer`
4. 调整参数（可选）

#### 添加实体位置绘制器：
1. 在 Hierarchy 中右键 > Create Empty
2. 命名为 "EntityDebugger"
3. 添加组件：`PVZ.DOTS.Debug.EntityPositionDebugDrawer`
4. 调整参数（可选）

## 查看效果

### Scene视图中查看：
1. 运行游戏（Play Mode）
2. 切换到 Scene 视图
3. 可以看到：
   - 绿色网格线和格子填充
   - 行列索引标签（R0, C0等）
   - 地图边界和中心点标记
   - 植物/僵尸/抛射物的位置可视化

### Game视图中查看：
运行时左侧会显示两个信息面板：
- **地图网格信息**（Y=200）：
  - 行数、列数、格子大小
  - 地图总尺寸
  - 关卡类型、难度
  
- **实体统计**（Y=360）：
  - 植物数量
  - 僵尸数量
  - 抛射物数量

## 地图偏移调整

默认地图起始位置为 `(-4.5, 0, 0)`，适用于5列地图。如果需要调整：

1. 选择 MapGridDebugger 对象
2. 修改 `Map Offset` 参数
3. 建议公式：`X = -(列数 * 格子大小) / 2`
   - 例如：5列 × 1.0单位 = -2.5
   - 例如：9列 × 1.0单位 = -4.5

## 配合关卡系统使用

地图网格会根据当前加载的关卡配置自动调整：

1. 使用菜单加载关卡：`PVZ > Load Level 1/2/3...`
2. 网格会自动更新为对应关卡的配置：
   - Day关卡：5行9列
   - Night关卡：5行9列（带墓碑）
   - Pool关卡：6行9列（中间2行是水池）
   - Roof关卡：5行9列（斜坡地形）

## 性能提示

- Gizmos 只在 Unity Editor 中绘制，不会影响发布版本性能
- 如果实体数量过多，可以关闭某些可视化选项
- 行列标签使用 `UnityEditor.Handles`，只在编辑器中有效

## 调试技巧

### 验证植物种植位置：
1. 在Scene视图中观察绿色网格
2. 种植植物后，查看植物标记是否在正确格子中
3. 检查标签显示的 `P(行,列)` 是否正确

### 验证僵尸移动路径：
1. 观察红色线框立方体（僵尸）
2. 查看僵尸是否沿着正确的行移动
3. 检查移动方向指示线是否指向左侧

### 验证攻击范围：
1. 观察植物和僵尸的相对位置
2. 当它们在同一行时，应该能够相互作用
3. 抛射物的黄色轨迹线应该指向目标方向

## 故障排除

### 网格不显示：
- 确保在 Play Mode 下运行
- 检查 `enableGridDrawing` 是否为 true
- 确认场景中存在 LevelConfigComponent 实体
- 查看 Console 是否有错误信息

### 实体不显示：
- 确保对应的显示开关已启用（showPlants/showZombies等）
- 检查是否有相应的实体存在（查看实体统计面板）
- 确认 World.DefaultGameObjectInjectionWorld 已创建

### 坐标标签不显示：
- 标签只在 Unity Editor 中显示
- 确保 `showRowColumnIndex` 和 `showGridCoordinates` 已启用
- 标签使用 `UnityEditor.Handles.Label`，运行时在Scene视图中查看

## 扩展建议

可以根据需要扩展这些调试工具：

1. **添加更多可视化元素**：
   - 攻击范围圆圈
   - 视野范围指示
   - 路径预测线

2. **交互式调试**：
   - 点击格子显示详细信息
   - 拖拽调整地图偏移
   - 运行时切换可视化选项

3. **性能监控**：
   - 显示System执行时间
   - 实体创建/销毁统计
   - 内存使用情况

## 相关文件

- `Assets/Scripts/Debug/MapGridDebugDrawer.cs` - 地图网格绘制器
- `Assets/Scripts/Debug/EntityPositionDebugDrawer.cs` - 实体位置绘制器
- `Assets/Scripts/SceneInitializer.cs` - 自动创建调试工具
- `Assets/Scripts/Components/LevelConfigComponent.cs` - 关卡配置组件
- `Assets/Configs/LevelConfig.json` - 关卡配置数据
