# 模块系统框架 (Module System Framework)

## 📖 简介

这是一个轻量级的游戏模块化框架，用于管理Unity项目中功能模块的生命周期和依赖关系。

## 🎯 设计目标

- **模块化**: 将游戏功能分解为独立、可复用的模块
- **配置驱动**: 通过JSON配置文件控制模块组合
- **依赖管理**: 自动解析和满足模块间的依赖关系
- **灵活组合**: 不同项目/场景可以使用不同的模块组合

## 📁 文件说明

### 核心文件

| 文件 | 说明 |
|------|------|
| `IGameModule.cs` | 模块接口定义和基类 |
| `ModuleRegistry.cs` | 模块注册表，管理模块生命周期 |
| `ModuleFactory.cs` | 模块工厂，负责创建模块实例 |
| `GameBootstrap.cs` | 游戏启动器，入口点 |

### 类关系

```
IGameModule (接口)
    ↑
    |
GameModuleBase (抽象基类)
    ↑
    |
CoreECSModule, RenderViewModule, ... (具体模块)
```

## 🚀 快速开始

### 1. 定义模块

```csharp
using PVZ.Framework.ModuleSystem;

public class MyModule : GameModuleBase
{
    public override string ModuleId => "my.module";
    public override string DisplayName => "我的模块";
    public override string[] Dependencies => new[] { "core.ecs" };
    public override int Priority => 100;

    protected override void OnInitialize()
    {
        // 初始化逻辑
        var world = Context.GetWorld();
        Debug.Log("模块已初始化");
    }
}
```

### 2. 创建配置文件

```json
{
  "projectName": "My Game",
  "modules": [
    {
      "moduleId": "core.ecs",
      "enabled": true
    },
    {
      "moduleId": "my.module",
      "enabled": true
    }
  ]
}
```

### 3. 启动游戏

```csharp
// 在场景中添加GameBootstrap组件
GameObject bootstrapObj = new GameObject("GameBootstrap");
var bootstrap = bootstrapObj.AddComponent<GameBootstrap>();
bootstrap.gameConfigJson = yourConfigFile;
bootstrap.autoInitialize = true;
```

## 🔧 核心概念

### 模块 (Module)

模块是独立的功能单元，必须实现`IGameModule`接口：

- **ModuleId**: 唯一标识符
- **DisplayName**: 显示名称
- **Dependencies**: 依赖的其他模块
- **Priority**: 初始化优先级（越小越先）
- **Initialize**: 初始化方法
- **Shutdown**: 关闭方法
- **Update**: 每帧更新（可选）

### 模块上下文 (ModuleContext)

通过`IModuleContext`，模块可以：

- 获取其他模块: `Context.GetModule<T>()`
- 访问ECS World: `Context.GetWorld()`
- 读取配置参数: `Context.GetConfigParameter<T>(key)`
- 注册/获取服务: `Context.RegisterService<T>()` / `GetService<T>()`

### 依赖解析

模块系统会：
1. 检测循环依赖
2. 检测缺失依赖
3. 按依赖顺序和优先级排序
4. 按正确顺序初始化模块

### 生命周期

```
注册 → 依赖解析 → 初始化 → 运行(Update) → 关闭
```

## 📊 模块优先级

推荐的优先级范围：

| 范围 | 用途 | 示例 |
|------|------|------|
| 0-50 | 核心基础模块 | ECS, Input, Audio |
| 50-100 | 通用功能模块 | 渲染, 动画, 物理 |
| 100-150 | 游戏逻辑模块 | 战斗, 关卡, AI |
| 150-200 | UI和优化模块 | UI, LOD, Culling |
| 200+ | 调试工具模块 | Debug, Profiler |

## 🔍 API参考

### IGameModule接口

```csharp
public interface IGameModule
{
    string ModuleId { get; }
    string DisplayName { get; }
    string Version { get; }
    string[] Dependencies { get; }
    int Priority { get; }
    bool IsInitialized { get; }
    
    void Initialize(IModuleContext context);
    void Update(float deltaTime);
    void Shutdown();
}
```

### ModuleRegistry类

```csharp
// 注册模块
registry.RegisterModule(IGameModule module);

// 设置World
registry.SetWorld(World world);

// 设置配置参数
registry.SetConfigParameter(string key, object value);

// 初始化所有模块
registry.InitializeAllModules();

// 更新所有模块
registry.UpdateAllModules(float deltaTime);

// 关闭所有模块
registry.ShutdownAllModules();
```

### ModuleFactory类

```csharp
// 初始化（自动扫描所有模块）
ModuleFactory.Initialize();

// 手动注册模块类型
ModuleFactory.RegisterModuleType(string moduleId, Type type);

// 获取模块类型
Type type = ModuleFactory.GetModuleType(string moduleId);

// 创建模块实例
IGameModule module = ModuleFactory.CreateModule(string moduleId);

// 获取所有模块ID
IEnumerable<string> ids = ModuleFactory.GetAllModuleIds();
```

## 🎨 设计模式

### 依赖注入 (Dependency Injection)

```csharp
protected override void OnInitialize()
{
    // 通过Context获取依赖
    var viewModule = Context.GetModule<RenderViewModule>();
    var world = Context.GetWorld();
}
```

### 服务定位器 (Service Locator)

```csharp
// 注册服务
Context.RegisterService<IDataService>(new DataService());

// 获取服务
var service = Context.GetService<IDataService>();
```

### 工厂模式 (Factory Pattern)

```csharp
// 通过ModuleFactory创建模块
var module = ModuleFactory.CreateModule("my.module");
```

## ⚡ 性能考虑

- **初始化开销**: 一次性，约0.1-0.2秒（可接受）
- **运行时开销**: 几乎为零（只有Update调用）
- **内存开销**: 可忽略（只有注册表和模块实例）

### 优化建议

1. **最小化Update**: 只在必要的模块中重写Update
2. **延迟初始化**: 复杂初始化可以分帧执行
3. **按需加载**: 通过配置禁用不需要的模块

## 🧪 测试

### 单元测试示例

```csharp
[Test]
public void TestModuleInitialization()
{
    var registry = new ModuleRegistry();
    var module = new MyModule();
    
    registry.RegisterModule(module);
    registry.InitializeAllModules();
    
    Assert.IsTrue(module.IsInitialized);
}
```

## 📚 扩展阅读

- [模块系统使用指南](../../../Docs/ModuleSystem_Guide.md)
- [迁移指南](../../../Docs/ModuleSystem_Migration.md)
- [实现说明](../../../Docs/ModuleSystem_Implementation.md)

## 🤝 最佳实践

1. **单一职责**: 每个模块只负责一个功能域
2. **明确依赖**: 在Dependencies中声明所有依赖
3. **避免循环依赖**: 设计模块时注意依赖方向
4. **合理优先级**: 基础模块优先级低，业务模块优先级高
5. **错误处理**: 在Initialize中捕获并记录异常
6. **资源清理**: 在Shutdown中释放所有资源

## ⚠️ 注意事项

- 模块ID必须全局唯一
- 依赖的模块必须在配置中启用
- 避免在Initialize中执行耗时操作
- 不要在模块间直接引用，使用Context通信
- 模块工厂会自动扫描，无需手动注册类型

## 📄 许可

本框架代码可自由用于PVZ项目及其衍生项目。

---

**版本**: 1.0.0  
**作者**: GitHub Copilot  
**日期**: 2025-12-10
