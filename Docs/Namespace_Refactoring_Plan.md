# PVZ项目命名空间重构方案

## 一、重构目标
将当前复杂的命名空间结构简化为三个顶级命名空间：
- **Framework** - 框架层代码
- **Common** - 通用组件和系统
- **PVZ** - 游戏业务逻辑

---

## 二、命名空间映射表

### 2.1 保持不变的命名空间（Framework和Common）

| 当前命名空间 | 新命名空间 | 文件数 | 说明 |
|------------|----------|--------|------|
| PVZ.Framework.ModuleSystem | Framework.ModuleSystem | 4 | 保持 |
| PVZ.Framework.EventBus | Framework.EventBus | 3 | 保持 |
| PVZ.Framework.Services | Framework.Services | 2 | 保持 |
| PVZ.Framework.Rendering | Framework.Rendering | 1 | 保持 |
| PVZ.Framework.Debug | Framework.Debug | 5 | 保持 |
| PVZ.Common.Components | Common.Components | 9 | 保持 |
| PVZ.Common.Systems | Common.Systems | 8 | 保持 |
| PVZ.Common.Modules | Common.Modules | 3 | 保持 |
| PVZ.Common.Utils | Common.Utils | ? | 保持 |

**子命名空间合计：**
- Framework.*: 15个文件
- Common.*: 20+个文件

---

### 2.2 需要重构的命名空间（改为PVZ）

| 当前命名空间 | 新命名空间 | 文件数 | 建议子命名空间 |
|------------|----------|--------|--------------|
| PVZ.SpecGame.PVE.Components | **PVZ.Components** | 5 | 游戏组件 |
| PVZ.SpecGame.PVE.Systems | **PVZ.Systems** | 7 | 游戏系统 |
| PVZ.SpecGame.PVE.Config | **PVZ.Config** | 3 | 配置加载 |
| PVZ.SpecGame.PVE.Data | **PVZ.Data** | 1 | 数据结构 |
| PVZ.SpecGame.PVE.Modules | **PVZ.Modules** | 1 | 游戏模块 |
| PVZ.SpecGame.PVE.Examples | **PVZ.Examples** | 4 | 示例代码 |
| PVZ.Type.TowerDefense.Components | **PVZ.Components** | 3 | 合并到PVZ.Components |
| PVZ.Type.TowerDefense.Systems | **PVZ.Systems** | 6 | 合并到PVZ.Systems |
| PVZ.DOTS | **PVZ** | 5 | 根命名空间 |
| PVZ.DOTS.Authoring | **PVZ.Authoring** | 3 | Unity Authoring |
| PVZ.DOTS.Tools | **PVZ.Tools** | 4 | 工具类 |
| PVZ.DOTS.Utils | **PVZ.Utils** | 2 | 工具方法 |
| PVZ.DOTS.Debug | **PVZ.Debug** | 2 | Debug相关 |
| PVZ.Game | **PVZ** | 1 | 合并到根命名空间 |
| PVZ.Game.Modules | **PVZ.Modules** | 1 | 合并到PVZ.Modules |
| PVZ.Tests | **PVZ.Tests** | 1 | 测试代码 |

**重构后的PVZ子命名空间：**
- PVZ (根): ~6个文件
- PVZ.Components: 8个文件
- PVZ.Systems: 13个文件
- PVZ.Config: 3个文件
- PVZ.Data: 1个文件
- PVZ.Modules: 2个文件
- PVZ.Authoring: 3个文件
- PVZ.Tools: 4个文件
- PVZ.Utils: 2个文件
- PVZ.Debug: 2个文件
- PVZ.Examples: 4个文件
- PVZ.Tests: 1个文件

---

## 三、Using语句重构映射

### 3.1 高频Using语句（需要全局替换）

| 当前Using语句 | 新Using语句 | 使用次数 | 影响范围 |
|-------------|-----------|---------|---------|
| using PVZ.Common.Components; | using Common.Components; | 41 | 高优先级 |
| using PVZ.SpecGame.PVE.Components; | using PVZ.Components; | 16 | 高优先级 |
| using PVZ.Type.TowerDefense.Components; | using PVZ.Components; | 13 | 高优先级 |
| using PVZ.DOTS.Utils; | using PVZ.Utils; | 11 | 高优先级 |
| using PVZ.Framework.ModuleSystem; | using Framework.ModuleSystem; | 10 | 中优先级 |
| using PVZ.Common.Systems; | using Common.Systems; | 5 | 中优先级 |
| using PVZ.Framework.EventBus; | using Framework.EventBus; | 4 | 中优先级 |
| using PVZ.Framework.Services; | using Framework.Services; | 4 | 中优先级 |
| using PVZ.Type.TowerDefense.Systems; | using PVZ.Systems; | 4 | 中优先级 |
| using PVZ.SpecGame.PVE.Systems; | using PVZ.Systems; | 2 | 低优先级 |
| using PVZ.Common.Modules; | using Common.Modules; | 1 | 低优先级 |
| using PVZ.Common.Utils; | using Common.Utils; | 1 | 低优先级 |
| using PVZ.SpecGame.PVE.Modules; | using PVZ.Modules; | 1 | 低优先级 |
| using PVZ.SpecGame.PVE.Config; | using PVZ.Config; | 1 | 低优先级 |
| using PVZ.Framework.Rendering; | using Framework.Rendering; | 1 | 低优先级 |

---

## 四、详细重构步骤

### 步骤1：更新Framework命名空间
**操作：** 移除"PVZ."前缀

```csharp
// 修改前
namespace PVZ
using PVZ.Framework.ModuleSystem;

// 修改后
namespace PVZ
using Framework.ModuleSystem;
```

**影响文件：**
- Framework/ModuleSystem/*.cs (4个文件)
- Framework/EventBus/*.cs (3个文件)
- Framework/Services/*.cs (2个文件)
- Framework/Rendering/*.cs (1个文件)
- Framework/Debug/*.cs (5个文件)

**修改内容：**
1. namespace声明：移除"PVZ."
2. 所有using语句：移除"PVZ."

---

### 步骤2：更新Common命名空间
**操作：** 移除"PVZ."前缀

```csharp
// 修改前
namespace PVZ
using PVZ.Common.Components;

// 修改后
namespace Common.Components
using Common.Components;
```

**影响文件：**
- Common/Components/*.cs (9个文件)
- Common/Systems/*.cs (8个文件)
- Common/Modules/*.cs (3个文件)
- Common/Utils/*.cs

---

### 步骤3：合并SpecGame.PVE到PVZ
**操作：** 重命名命名空间

```csharp
// 修改前
namespace PVZ
namespace PVZ
namespace PVZ

// 修改后
namespace PVZ
namespace PVZ
namespace PVZ
```

**影响文件：**
- SpecGame/PVE/Components/*.cs (5个文件) → PVZ.Components
- SpecGame/PVE/Systems/*.cs (7个文件) → PVZ.Systems
- SpecGame/PVE/Config/*.cs (3个文件) → PVZ.Config
- SpecGame/PVE/Data/*.cs (1个文件) → PVZ.Data
- SpecGame/PVE/Modules/*.cs (1个文件) → PVZ.Modules
- SpecGame/PVE/Examples/*.cs (4个文件) → PVZ.Examples

---

### 步骤4：合并Type.TowerDefense到PVZ
**操作：** 重命名并合并到PVZ

```csharp
// 修改前
namespace PVZ
namespace PVZ

// 修改后
namespace PVZ
namespace PVZ
```

**影响文件：**
- Type/TowerDefense/Components/*.cs (3个文件) → PVZ.Components
- Type/TowerDefense/Systems/*.cs (6个文件) → PVZ.Systems

**注意：** 需要检查是否有命名冲突

---

### 步骤5：重构DOTS命名空间
**操作：** 简化PVZ.DOTS.*

```csharp
// 修改前
namespace PVZ
namespace PVZ
namespace PVZ
namespace PVZ
namespace PVZ

// 修改后
namespace PVZ
namespace PVZ
namespace PVZ
namespace PVZ
namespace PVZ
```

**影响文件：**
- Game/*.cs (PVZ.DOTS → PVZ, 5个文件)
- Game/Authoring/*.cs (3个文件)
- Framework/Tools/*.cs (4个文件) → 移到PVZ.Tools
- Framework/Utils/*.cs (2个文件) → 移到PVZ.Utils
- Framework/Debug/PerformanceTestSpawner.cs等 (2个文件) → 移到PVZ.Debug

---

### 步骤6：处理Game命名空间
**操作：** 合并到PVZ

```csharp
// 修改前
namespace PVZ
namespace PVZ

// 修改后
namespace PVZ
namespace PVZ
```

**影响文件：**
- Game/SceneModuleInitializer.cs → PVZ
- Common/Modules/EventBusModule.cs (当前是PVZ.Game.Modules) → PVZ.Modules

---

## 五、潜在冲突检查

### 5.1 命名空间合并冲突
合并以下命名空间时需要检查类名冲突：

1. **PVZ.Components合并：**
   - PVZ.SpecGame.PVE.Components (5个文件)
   - PVZ.Type.TowerDefense.Components (3个文件)
   - **需要检查：** 是否有同名组件

2. **PVZ.Systems合并：**
   - PVZ.SpecGame.PVE.Systems (7个文件)
   - PVZ.Type.TowerDefense.Systems (6个文件)
   - **需要检查：** 是否有同名系统

3. **PVZ.Modules合并：**
   - PVZ.SpecGame.PVE.Modules (1个文件)
   - PVZ.Game.Modules (1个文件)
   - **需要检查：** GameModules vs EventBusModule

### 5.2 Using语句更新优先级
1. **第一批：** Framework和Common (低风险)
2. **第二批：** PVZ.Components和PVZ.Systems (高频使用)
3. **第三批：** PVZ.Utils、PVZ.Tools等 (中频使用)
4. **第四批：** 其他低频命名空间

---

## 六、实施建议

### 6.1 分阶段重构
**阶段1：准备工作**
- [ ] 创建完整的文件备份
- [ ] 运行所有单元测试，记录当前状态
- [ ] 创建命名冲突检查脚本

**阶段2：Framework命名空间重构**
- [ ] 更新Framework文件夹下所有namespace声明
- [ ] 全局替换using语句
- [ ] 编译测试

**阶段3：Common命名空间重构**
- [ ] 更新Common文件夹下所有namespace声明
- [ ] 全局替换using语句
- [ ] 编译测试

**阶段4：PVZ命名空间重构**
- [ ] 重构SpecGame.PVE.*
- [ ] 重构Type.TowerDefense.*
- [ ] 重构DOTS.*
- [ ] 重构Game.*
- [ ] 检查并解决命名冲突
- [ ] 全局替换所有using语句
- [ ] 完整编译测试

**阶段5：验证和清理**
- [ ] 运行所有单元测试
- [ ] 检查是否有遗漏的using语句
- [ ] 清理未使用的using语句
- [ ] 更新项目文档

### 6.2 自动化工具建议
建议使用以下工具辅助重构：
1. Visual Studio的重命名功能（Ctrl+R, R）
2. 正则表达式批量替换
3. ReSharper或Rider的命名空间重构功能
4. 自定义PowerShell脚本批量处理

### 6.3 风险控制
- 每个阶段完成后立即编译测试
- 使用版本控制系统（Git）创建检查点
- 保留详细的重构日志
- 建议在独立分支上进行重构

---

## 七、预期收益

1. **命名空间层级减少：** 从3-4层减少到1-2层
2. **代码可读性提升：** 命名空间更简洁直观
3. **Using语句简化：** 移除冗余的"PVZ"前缀
4. **项目结构清晰：** 明确的Framework、Common、PVZ三层架构
5. **维护成本降低：** 减少命名空间管理复杂度

---

## 八、统计总结

### 当前状态
- **总命名空间数：** 24个
- **Framework命名空间：** 5个（需移除PVZ前缀）
- **Common命名空间：** 4个（需移除PVZ前缀）
- **需要重构的命名空间：** 15个
- **涉及文件：** 84个.cs文件
- **Using语句总数：** 115+次引用

### 重构后
- **总命名空间数：** 约20个（简化结构）
- **顶级命名空间：** 3个（Framework, Common, PVZ）
- **PVZ子命名空间：** 约12个
- **预期减少命名空间嵌套层级：** 1-2层

---

## 九、执行命令参考

### PowerShell批量替换示例

```powershell
# 示例：批量替换Framework命名空间
Get-ChildItem -Path "Assets\Scripts\Framework" -Recurse -Filter "*.cs" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $content = $content -replace 'namespace PVZ
    $content = $content -replace 'using PVZ\.Framework\.', 'using Framework.'
    Set-Content -Path $_.FullName -Value $content -NoNewline
}

# 全局替换using语句
Get-ChildItem -Path "Assets\Scripts" -Recurse -Filter "*.cs" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $content = $content -replace 'using PVZ\.Framework\.', 'using Framework.'
    Set-Content -Path $_.FullName -Value $content -NoNewline
}
```

---

**文档创建时间：** 2025-12-11  
**文档版本：** v1.0  
**状态：** 待审核
