# System 单例 Context 访问模式

## 设计理念

降低模块与系统之间的耦合度，让 ECS System 自己通过单例 Context 获取所需服务，而不是由模块手动注入。

## 架构对比

### ❌ 旧方式（高耦合）

```csharp
// RenderingModule 需要知道 System 的具体接口
public class RenderingModule : GameModuleBase
{
    public override void Initialize(IModuleContext context)
    {
        var resourceService = context.GetService<IResourceService>();
        
        var viewLoaderSystem = world.GetOrCreateSystemManaged<ViewLoaderSystem>();
        
        // 模块手动注入服务（紧耦合）
        viewLoaderSystem.SetResourceService(resourceService);
        viewLoaderSystem.SetPoolService(poolService);
    }
}

// System 需要暴露注入接口
public partial class ViewLoaderSystem : SystemBase
{
    private IResourceService _resourceService;
    
    public void SetResourceService(IResourceService service) // 注入方法
    {
        _resourceService = service;
    }
}
```

**问题**：
- 模块需要知道每个 System 的注入接口
- System 需要暴露公共的 setter 方法
- 添加新服务时需要修改模块和 System 代码
- 难以测试和 Mock

### ✅ 新方式（低耦合）

```csharp
// RenderingModule 只负责创建 System
public class RenderingModule : GameModuleBase
{
    public override void Initialize(IModuleContext context)
    {
        var world = context.GetService<World>();
        
        // 只创建 System，不做任何注入
        _viewLoaderSystem = world.GetOrCreateSystemManaged<ViewLoaderSystem>();
        _spineViewSystem = world.GetOrCreateSystemManaged<SpineViewSystem>();
    }
}

// System 自己通过单例 Context 获取服务
public partial class ViewLoaderSystem : SystemBase
{
    private IModuleContext _context;
    private IResourceService _resourceService;
    
    private IModuleContext GetContext()
    {
        if (_context == null)
        {
            _context = GameBootstrap.Instance?.Context;
        }
        return _context;
    }
    
    private IResourceService GetResourceService()
    {
        if (_resourceService == null)
        {
            _resourceService = GetContext()?.GetService<IResourceService>();
        }
        return _resourceService;
    }
    
    private void LoadViewForEntity(...)
    {
        var resourceService = GetResourceService();
        if (resourceService != null)
        {
            prefab = resourceService.Load<GameObject>(path);
        }
    }
}
```

**优势**：
- 模块不需要知道 System 的具体实现
- System 自主管理依赖（懒加载）
- 添加新服务无需修改模块代码
- 便于单元测试（可 Mock GameBootstrap.Instance）

## 实现细节

### 1. GameBootstrap 单例

```csharp
public class GameBootstrap : MonoBehaviour
{
    // 全局单例实例
    public static GameBootstrap Instance { get; private set; }
    
    // 全局模块上下文
    public IModuleContext Context => _moduleRegistry;
    
    private void Awake()
    {
        // 设置单例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        Initialize();
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
```

### 2. System 中的服务访问模式

**标准模板**：

```csharp
public partial class YourSystem : SystemBase
{
    // 缓存字段
    private IModuleContext _context;
    private IResourceService _resourceService;
    private IPoolService _poolService;
    
    // 懒加载 Context
    private IModuleContext GetContext()
    {
        if (_context == null)
        {
            var bootstrap = GameBootstrap.Instance;
            if (bootstrap != null)
            {
                _context = bootstrap.Context;
            }
            else
            {
                Debug.LogError("GameBootstrap instance not found!");
            }
        }
        return _context;
    }
    
    // 懒加载服务
    private IResourceService GetResourceService()
    {
        if (_resourceService == null)
        {
            var context = GetContext();
            if (context != null)
            {
                _resourceService = context.GetService<IResourceService>();
            }
        }
        return _resourceService;
    }
    
    // 在需要时使用
    protected override void OnUpdate()
    {
        var resourceService = GetResourceService();
        if (resourceService != null)
        {
            // 使用服务
        }
    }
}
```

## 使用示例

### 示例 1: ViewLoaderSystem 加载预制体

```csharp
public partial class ViewLoaderSystem : SystemBase
{
    private void LoadViewForEntity(...)
    {
        string prefabPath = ...;
        GameObject prefab = null;
        
        // 获取资源服务（懒加载）
        var resourceService = GetResourceService();
        if (resourceService != null)
        {
            // 使用服务缓存加载
            prefab = resourceService.Load<GameObject>(prefabPath);
        }
        else
        {
            // 降级方案：直接使用 Resources.Load
            prefab = Resources.Load<GameObject>(prefabPath);
        }
        
        // 实例化视图...
    }
}
```

### 示例 2: 自定义战斗系统访问多个服务

```csharp
public partial class CombatSystem : SystemBase
{
    private IModuleContext _context;
    private IEventBus _eventBus;
    private IAudioService _audioService;
    private IPoolService _poolService;
    
    private IModuleContext GetContext()
    {
        if (_context == null)
        {
            _context = GameBootstrap.Instance?.Context;
        }
        return _context;
    }
    
    private IEventBus GetEventBus()
    {
        if (_eventBus == null)
        {
            _eventBus = GetContext()?.GetService<IEventBus>();
        }
        return _eventBus;
    }
    
    private IAudioService GetAudioService()
    {
        if (_audioService == null)
        {
            _audioService = GetContext()?.GetService<IAudioService>();
        }
        return _audioService;
    }
    
    private IPoolService GetPoolService()
    {
        if (_poolService == null)
        {
            _poolService = GetContext()?.GetService<IPoolService>();
        }
        return _poolService;
    }
    
    protected override void OnUpdate()
    {
        // 处理僵尸死亡
        foreach (var zombie in Query<ZombieComponent>())
        {
            if (zombie.Health <= 0)
            {
                // 播放音效
                GetAudioService()?.PlaySound("zombie_death");
                
                // 从对象池获取特效
                var effect = GetPoolService()?.Get("effect.death");
                
                // 发布事件
                GetEventBus()?.Publish(new ZombieDeathEvent { ... });
            }
        }
    }
}
```

### 示例 3: 植物系统响应事件

```csharp
public partial class PlantSystem : SystemBase
{
    private IEventBus _eventBus;
    private bool _subscribed = false;
    
    protected override void OnCreate()
    {
        base.OnCreate();
        
        // 订阅事件
        var eventBus = GetEventBus();
        if (eventBus != null && !_subscribed)
        {
            eventBus.Subscribe<SunCollectedEvent>(OnSunCollected);
            _subscribed = true;
        }
    }
    
    private void OnSunCollected(SunCollectedEvent evt)
    {
        // 处理阳光收集
        Debug.Log($"阳光收集: {evt.Amount}");
    }
    
    private IEventBus GetEventBus()
    {
        if (_eventBus == null)
        {
            _eventBus = GameBootstrap.Instance?.Context?.GetService<IEventBus>();
        }
        return _eventBus;
    }
}
```

## 性能考虑

### 1. 懒加载缓存

服务引用只在第一次访问时获取，之后使用缓存：

```csharp
private IResourceService GetResourceService()
{
    if (_resourceService == null)  // 检查缓存
    {
        _resourceService = GetContext()?.GetService<IResourceService>();
    }
    return _resourceService;  // 返回缓存
}
```

**性能影响**：几乎为零，只有首次调用有少量开销。

### 2. 空检查

始终检查服务是否可用：

```csharp
var service = GetResourceService();
if (service != null)  // 防御性检查
{
    service.Load(...);
}
```

### 3. 避免频繁调用 GetContext()

在 OnCreate() 或 OnStartRunning() 中预获取常用服务：

```csharp
protected override void OnStartRunning()
{
    base.OnStartRunning();
    
    // 预获取常用服务
    _resourceService = GetResourceService();
    _audioService = GetAudioService();
    _eventBus = GetEventBus();
}

protected override void OnUpdate()
{
    // 直接使用缓存的服务（无需再次调用 GetXxxService()）
    if (_audioService != null)
    {
        _audioService.PlaySound("test");
    }
}
```

## 测试友好性

### Mock GameBootstrap 进行单元测试

```csharp
[Test]
public void TestViewLoaderSystem()
{
    // 创建 Mock Context
    var mockContext = new Mock<IModuleContext>();
    var mockResourceService = new Mock<IResourceService>();
    
    mockContext.Setup(c => c.GetService<IResourceService>())
               .Returns(mockResourceService.Object);
    
    // 创建 Mock GameBootstrap
    var mockBootstrap = new Mock<GameBootstrap>();
    mockBootstrap.Setup(b => b.Context).Returns(mockContext.Object);
    
    // 设置单例（通过反射或测试辅助类）
    GameBootstrap.Instance = mockBootstrap.Object;
    
    // 测试系统
    var system = World.CreateSystemManaged<ViewLoaderSystem>();
    system.Update();
    
    // 验证服务调用
    mockResourceService.Verify(s => s.Load<GameObject>(It.IsAny<string>()), 
                                Times.Once());
}
```

## 最佳实践

### ✅ 推荐做法

1. **懒加载服务**：只在需要时获取
2. **缓存引用**：避免重复调用 GetService()
3. **空检查**：始终检查服务是否可用
4. **降级方案**：提供 fallback 逻辑（如直接 Resources.Load）

### ❌ 避免做法

1. **在 OnUpdate() 中重复调用 GetService()**：性能损耗
2. **不检查 null**：可能导致空引用异常
3. **在静态字段中缓存服务**：生命周期管理问题
4. **在 OnCreate() 中立即访问服务**：服务可能尚未初始化

## 常见问题

### Q1: GameBootstrap.Instance 为 null？

**原因**：
- GameBootstrap 尚未初始化
- 在 Awake() 之前访问
- 场景中没有 GameBootstrap

**解决**：
```csharp
private IModuleContext GetContext()
{
    if (_context == null)
    {
        var bootstrap = GameBootstrap.Instance;
        if (bootstrap == null)
        {
            Debug.LogError("GameBootstrap not initialized yet!");
            return null;
        }
        _context = bootstrap.Context;
    }
    return _context;
}
```

### Q2: 服务返回 null？

**原因**：
- 服务模块未在配置中启用
- 模块初始化顺序问题
- 服务注册失败

**解决**：
```csharp
var service = GetResourceService();
if (service == null)
{
    Debug.LogWarning("ResourceService not available, using fallback");
    prefab = Resources.Load<GameObject>(path);  // 降级方案
}
```

### Q3: 如何在编辑器中测试？

**方法**：
1. 创建测试场景，添加 GameBootstrap GameObject
2. 配置 gameConfigJson 引用
3. 进入 Play Mode 自动初始化
4. 通过 Debug.Log 验证服务可用性

## 总结

单例 Context 访问模式的核心优势：

| 特性 | 旧方式（注入） | 新方式（单例） |
|------|-------------|-------------|
| 耦合度 | 高（模块知道 System 细节） | 低（模块只创建 System） |
| 扩展性 | 差（新服务需修改模块） | 好（System 自主获取） |
| 可测试性 | 难（需要模拟注入） | 易（Mock 单例即可） |
| 代码量 | 多（每个服务一个 setter） | 少（统一 Get 模式） |
| 性能 | 略好（直接引用） | 相同（懒加载缓存） |

**推荐**：在所有 ECS System 中使用此模式访问模块服务。
