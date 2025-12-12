# 服务层架构指南

## 概述

服务层提供跨模块的单例功能，是模块化架构中数据交换的核心机制之一。与事件总线配合使用，服务层负责提供可调用的功能接口（如音频播放、资源加载、数据存储等），而事件总线负责事件驱动的通知机制。

## 混合数据交换架构

```
┌─────────────────────────────────────────────────┐
│              模块化架构                          │
├─────────────────────────────────────────────────┤
│                                                 │
│  ┌──────────────┐      ┌──────────────┐        │
│  │  事件总线    │      │  服务层      │        │
│  │  EventBus    │      │  Services    │        │
│  └──────────────┘      └──────────────┘        │
│         ↓                      ↓                │
│  ┌─────────────────────────────────────────┐   │
│  │  植物模块 → 发布PlantPlaced事件         │   │
│  │          → 调用音频服务播放音效         │   │
│  └─────────────────────────────────────────┘   │
│                                                 │
│  ┌─────────────────────────────────────────┐   │
│  │  僵尸模块 → 订阅ZombieDeath事件         │   │
│  │          → 调用对象池服务回收GameObject  │   │
│  └─────────────────────────────────────────┘   │
│                                                 │
└─────────────────────────────────────────────────┘
```

### 使用原则

1. **事件总线（EventBus）**：用于模块间的通知和解耦
   - 游戏事件（植物种植、僵尸死亡、关卡完成等）
   - 不需要返回值的单向通知
   - 一对多的广播场景

2. **服务层（Services）**：用于提供可调用的功能
   - 需要返回值的操作（加载资源、读取存档）
   - 跨模块的共享功能（音频播放、对象池管理）
   - 全局单例功能

## 核心服务接口

### 1. 音频服务（IAudioService）

提供游戏音频播放功能，包括音效和背景音乐。

```csharp
public interface IAudioService
{
    void PlaySound(string clipName, float volume = 1f, float3? position = null);
    void PlayMusic(string clipName, bool loop = true, bool fadeIn = false, float fadeDuration = 1f);
    void StopMusic(bool fadeOut = false, float fadeDuration = 1f);
    void PauseMusic(bool pause);
    void SetMasterVolume(float volume);
    void SetSoundVolume(float volume);
    void SetMusicVolume(float volume);
    void Cleanup();
}
```

**使用场景**：
- 僵尸死亡播放音效
- 植物攻击播放声音
- 关卡背景音乐播放
- 全局音量控制

**示例**：
```csharp
var audioService = context.GetService<IAudioService>();

// 播放2D音效
audioService.PlaySound("plant_shoot", volume: 0.8f);

// 播放3D音效（带位置）
audioService.PlaySound("zombie_groan", volume: 1f, position: zombiePosition);

// 播放背景音乐，淡入效果
audioService.PlayMusic("grasswalk", fadeIn: true, fadeDuration: 2f);
```

### 2. 资源服务（IResourceService）

提供资源加载和管理功能。

```csharp
public interface IResourceService
{
    T Load<T>(string path) where T : Object;
    void LoadAsync<T>(string path, System.Action<T> onComplete) where T : Object;
    GameObject Instantiate(string path, float3 position, quaternion rotation);
    void UnloadAsset(string path);
    void PreloadAssets(string[] paths, System.Action onComplete = null);
    void ClearCache();
}
```

**使用场景**：
- 加载预制体（植物、僵尸）
- 加载材质和贴图
- 资源预加载和缓存
- 实例化游戏对象

**示例**：
```csharp
var resourceService = context.GetService<IResourceService>();

// 同步加载预制体
var zombiePrefab = resourceService.Load<GameObject>("Prefabs/Zombies/NormalZombie");

// 异步加载
resourceService.LoadAsync<GameObject>("Prefabs/Plants/Sunflower", (prefab) =>
{
    if (prefab != null)
    {
        var instance = Instantiate(prefab);
    }
});

// 直接实例化
var zombie = resourceService.Instantiate("Prefabs/Zombies/NormalZombie", position, rotation);
```

### 3. 存档服务（ISaveService）

提供数据持久化功能。

```csharp
public interface ISaveService
{
    void Save<T>(string key, T data);
    T Load<T>(string key, T defaultValue = default);
    bool HasKey(string key);
    void Delete(string key);
    void DeleteAll();
    void SaveAll();
}
```

**使用场景**：
- 玩家进度保存
- 游戏设置存储
- 统计数据记录
- 关卡解锁状态

**示例**：
```csharp
var saveService = context.GetService<ISaveService>();

// 保存玩家数据
var playerData = new PlayerData 
{ 
    PlayerName = "Player1", 
    Level = 5, 
    TotalScore = 10000 
};
saveService.Save("PlayerData", playerData);
saveService.SaveAll(); // 刷新到磁盘

// 加载玩家数据
var loadedData = saveService.Load("PlayerData", new PlayerData());
```

### 4. 对象池服务（IPoolService）

提供GameObject对象池管理功能。

```csharp
public interface IPoolService
{
    void CreatePool(string poolId, GameObject prefab, int initialSize = 10, int maxSize = 100);
    GameObject Get(string poolId);
    void Return(string poolId, GameObject obj);
    void WarmPool(string poolId, int count);
    void ClearPool(string poolId);
    void ClearAll();
}
```

**使用场景**：
- 子弹对象池（豌豆、西瓜等）
- 特效对象池（爆炸、火焰）
- 僵尸对象池（频繁生成和销毁）
- UI元素池（伤害数字）

**示例**：
```csharp
var poolService = context.GetService<IPoolService>();

// 创建对象池
var peaPrefab = Resources.Load<GameObject>("Prefabs/Projectiles/Pea");
poolService.CreatePool("projectile.pea", peaPrefab, initialSize: 50, maxSize: 200);

// 预热对象池
poolService.WarmPool("projectile.pea", 30);

// 从池中获取对象
var pea = poolService.Get("projectile.pea");
pea.transform.position = shootPosition;

// 归还到池
poolService.Return("projectile.pea", pea);
```

## 服务模块注册

服务通过ServiceModule包装成模块，在模块系统中初始化：

```csharp
public class AudioServiceModule : GameModuleBase
{
    public override string ModuleId => "service.audio";
    public override int Priority => 15;
    public override string[] Dependencies => new[] { "core.ecs" };

    private AudioService _audioService;

    public override void Initialize(IModuleContext context)
    {
        base.Initialize(context);
        
        // 创建并注册服务
        _audioService = new AudioService();
        _audioService.Initialize();
        Context.RegisterService<IAudioService>(_audioService);
    }

    public override void Shutdown()
    {
        _audioService?.Cleanup();
        _audioService = null;
        base.Shutdown();
    }
}
```

## 配置文件集成

在 `GameModuleConfig.json` 中配置服务模块：

```json
{
  "modules": [
    {
      "moduleId": "service.pool",
      "enabled": true,
      "parametersJson": "{\"defaultPoolSize\": 50, \"maxPoolSize\": 200}",
      "orderOverride": -1
    },
    {
      "moduleId": "service.resource",
      "enabled": true,
      "parametersJson": "{\"cacheEnabled\": true}",
      "orderOverride": -1
    },
    {
      "moduleId": "service.audio",
      "enabled": true,
      "parametersJson": "{\"masterVolume\": 1.0, \"soundVolume\": 0.8, \"musicVolume\": 0.6}",
      "orderOverride": -1
    },
    {
      "moduleId": "service.save",
      "enabled": true,
      "parametersJson": "{\"autoSave\": true, \"autoSaveInterval\": 60.0}",
      "orderOverride": -1
    }
  ]
}
```

## 服务与事件总线配合使用

### 完整示例：僵尸死亡处理

```csharp
public class ZombieDeathHandler
{
    private IAudioService _audioService;
    private IPoolService _poolService;
    private ISaveService _saveService;
    private IEventBus _eventBus;

    public void Initialize(IModuleContext context)
    {
        _audioService = context.GetService<IAudioService>();
        _poolService = context.GetService<IPoolService>();
        _saveService = context.GetService<ISaveService>();
        _eventBus = context.GetService<IEventBus>();

        // 订阅僵尸死亡事件
        _eventBus.Subscribe<ZombieDeathEvent>(OnZombieDeath);
    }

    private void OnZombieDeath(ZombieDeathEvent evt)
    {
        // 1. 播放死亡音效（服务）
        _audioService.PlaySound("zombie_death", volume: 1f, position: evt.Position);
        
        // 2. 从对象池获取死亡特效（服务）
        var deathEffect = _poolService.Get("effect.zombie_death");
        if (deathEffect != null)
        {
            deathEffect.transform.position = evt.Position.ToVector3();
        }
        
        // 3. 更新统计数据（服务）
        var stats = _saveService.Load<GameStatistics>("Statistics", new GameStatistics());
        stats.TotalZombiesKilled++;
        _saveService.Save("Statistics", stats);
        
        // 4. 发布经验获得事件（事件总线）
        _eventBus.Publish(new ExperienceGainedEvent
        {
            Amount = evt.ScoreValue,
            Source = "ZombieKill"
        });
    }
}
```

### 使用场景分析

| 功能 | 使用机制 | 原因 |
|------|---------|------|
| 播放音效 | 服务层 | 需要立即执行，需要返回值（AudioSource） |
| 加载资源 | 服务层 | 需要返回值（GameObject/Texture等） |
| 保存数据 | 服务层 | 需要立即执行，可能需要确认结果 |
| 对象池管理 | 服务层 | 需要返回值（GameObject实例） |
| 僵尸死亡通知 | 事件总线 | 一对多广播，模块解耦 |
| 关卡完成通知 | 事件总线 | 多个系统需要响应 |
| 分数变化通知 | 事件总线 | UI更新、音效播放等多重响应 |

## 最佳实践

### 1. 服务职责单一

每个服务专注于一个领域：
```csharp
// ✅ 好的设计
IAudioService   -> 只负责音频
IResourceService -> 只负责资源加载

// ❌ 避免
IGameService -> 包含音频、资源、存档等所有功能
```

### 2. 接口隔离

定义清晰的接口，避免臃肿：
```csharp
// ✅ 接口清晰
public interface IAudioService
{
    void PlaySound(string clipName, float volume = 1f, float3? position = null);
    void PlayMusic(string clipName, bool loop = true);
}

// ❌ 接口过于复杂
public interface IAudioService
{
    void PlaySound(...);
    void PlayMusic(...);
    void LoadAudioClip(...);
    void CreateAudioSource(...);
    void SetupAudioMixer(...);
    // 太多内部实现细节暴露
}
```

### 3. 服务与事件协作

根据需求选择合适的机制：
```csharp
// 场景1：需要返回值 -> 使用服务
var prefab = resourceService.Load<GameObject>("Prefabs/Zombie");

// 场景2：通知多个模块 -> 使用事件
eventBus.Publish(new WaveCompletedEvent { WaveNumber = 5 });

// 场景3：服务调用后发布事件
audioService.PlaySound("level_complete");
eventBus.Publish(new LevelCompletedEvent());
```

### 4. 异步操作处理

对于耗时操作，提供异步接口：
```csharp
// 同步版本（快速操作）
var clip = audioService.LoadClip("small_sound");

// 异步版本（大资源）
resourceService.LoadAsync<GameObject>("LargeLevel", (prefab) =>
{
    // 加载完成后的处理
});
```

### 5. 错误处理

服务应该提供清晰的错误信息：
```csharp
public T Load<T>(string path) where T : Object
{
    var asset = Resources.Load<T>(path);
    if (asset == null)
    {
        Debug.LogError($"[ResourceService] Failed to load asset at path: {path}");
        return null;
    }
    return asset;
}
```

## 扩展服务

### 添加新服务的步骤

1. **定义接口**（IServices.cs）：
```csharp
public interface ITimeService
{
    float DeltaTime { get; }
    float TimeScale { get; set; }
    void PauseGame();
    void ResumeGame();
}
```

2. **实现服务**（ServiceImplementations.cs）：
```csharp
public class TimeService : ITimeService
{
    public float DeltaTime => Time.deltaTime * _timeScale;
    private float _timeScale = 1f;
    public float TimeScale
    {
        get => _timeScale;
        set => _timeScale = Mathf.Max(0, value);
    }

    public void PauseGame() => TimeScale = 0;
    public void ResumeGame() => TimeScale = 1;
}
```

3. **创建服务模块**（ServiceModules.cs）：
```csharp
public class TimeServiceModule : GameModuleBase
{
    public override string ModuleId => "service.time";
    public override int Priority => 10;
    
    private TimeService _timeService;

    public override void Initialize(IModuleContext context)
    {
        base.Initialize(context);
        _timeService = new TimeService();
        Context.RegisterService<ITimeService>(_timeService);
    }
}
```

4. **注册到配置**（GameModuleConfig.json）：
```json
{
  "moduleId": "service.time",
  "enabled": true,
  "parametersJson": "{}",
  "orderOverride": -1
}
```

## 性能优化建议

### 1. 对象池预热

游戏启动时预热常用对象池：
```csharp
void PreloadPools()
{
    poolService.CreatePool("projectile.pea", peaPrefab, 50, 200);
    poolService.WarmPool("projectile.pea", 40); // 预热40个
}
```

### 2. 资源缓存

启用资源缓存减少重复加载：
```csharp
// ResourceService 内部实现
private Dictionary<string, Object> _cache = new Dictionary<string, Object>();

public T Load<T>(string path) where T : Object
{
    if (_cache.TryGetValue(path, out var cached))
        return cached as T;
    
    var asset = Resources.Load<T>(path);
    _cache[path] = asset;
    return asset;
}
```

### 3. 音频混音器

使用AudioMixer管理音频分组：
```csharp
public class AudioService : IAudioService
{
    private AudioMixerGroup _soundGroup;
    private AudioMixerGroup _musicGroup;
    
    public void SetSoundVolume(float volume)
    {
        _soundGroup.audioMixer.SetFloat("SoundVolume", Mathf.Log10(volume) * 20);
    }
}
```

## 调试工具

### 服务监控编辑器窗口

创建编辑器窗口监控服务状态：
```csharp
public class ServiceMonitorWindow : EditorWindow
{
    [MenuItem("PVZ/Service Monitor")]
    static void ShowWindow()
    {
        GetWindow<ServiceMonitorWindow>("Service Monitor");
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return;

        var context = GameBootstrap.Instance?.Context;
        if (context == null) return;

        GUILayout.Label("已注册服务:", EditorStyles.boldLabel);
        
        DrawServiceStatus<IAudioService>(context, "音频服务");
        DrawServiceStatus<IResourceService>(context, "资源服务");
        DrawServiceStatus<ISaveService>(context, "存档服务");
        DrawServiceStatus<IPoolService>(context, "对象池服务");
    }

    void DrawServiceStatus<T>(IModuleContext context, string serviceName)
    {
        var service = context.GetService<T>();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(serviceName, GUILayout.Width(100));
        GUILayout.Label(service != null ? "✓ 运行中" : "✗ 未注册");
        EditorGUILayout.EndHorizontal();
    }
}
```

## 总结

服务层架构提供了：
1. **清晰的功能边界**：每个服务职责明确
2. **跨模块访问**：通过Context.GetService<T>()统一访问
3. **易于测试**：接口化设计便于Mock
4. **灵活扩展**：新增服务不影响现有系统
5. **配置驱动**：通过JSON配置启用/禁用服务

与事件总线结合，构成完整的模块间通信体系：
- **服务层**：提供功能调用（有返回值）
- **事件总线**：提供事件通知（无返回值）

下一步：参考 `ServiceExamples.cs` 了解具体使用方法。
