# Spine 性能优化方案

## 现状说明
项目当前存在一些编译问题（缺少 Spine 包、Config 和 GameLogger 等依赖），这些是项目原有的问题。

## 性能优化建议

针对场景中大量 Spine 动画的情况，以下是可以实施的优化方案：

### 1. **视锥体剔除 (Frustum Culling)** ⭐⭐⭐⭐⭐
**效果**: 20-50% 性能提升

**原理**: 不渲染相机视野外的对象

**实现方式**:
```csharp
// 在 SpineViewSystem 的 UpdateViews() 中添加
Camera mainCamera = Camera.main;
if (mainCamera != null)
{
    var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
    
    foreach (var (viewInstance, transform) in ...)
    {
        var bounds = new Bounds(transform.Position, Vector3.one * 2f);
        if (!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
            continue; // 跳过不可见对象
        
        // 正常更新可见对象...
    }
}
```

### 2. **LOD 层级细节 (Level of Detail)** ⭐⭐⭐⭐
**效果**: 30-60% 性能提升

**原理**: 根据距离调整更新频率

**实现方式**:
```csharp
// 在 SpineViewSystem 中添加帧计数器
private int _frameCounter = 0;

protected override void UpdateViews()
{
    _frameCounter++;
    Camera mainCamera = Camera.main;
    
    foreach (var (viewInstance, transform) in ...)
    {
        // 计算距离
        float distance = Vector3.Distance(mainCamera.transform.position, transform.Position);
        
        // 根据距离决定更新频率
        int updateInterval = 1;
        if (distance > 30f)
            updateInterval = 4; // 每4帧更新
        else if (distance > 20f)
            updateInterval = 3; // 每3帧更新
        else if (distance > 10f)
            updateInterval = 2; // 每2帧更新
        
        if (_frameCounter % updateInterval != 0)
            continue;
        
        // 正常更新...
    }
}
```

### 3. **对象池 (Object Pooling)** ⭐⭐⭐⭐⭐
**效果**: 减少 GC 压力 60-80%

**原理**: 复用 GameObject 避免频繁创建/销毁

**实现方式**:
```csharp
public class SpineViewPool : MonoBehaviour
{
    private static SpineViewPool _instance;
    public static SpineViewPool Instance => _instance ?? (_instance = FindObjectOfType<SpineViewPool>());
    
    private Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>();
    
    public GameObject Get(string prefabPath, GameObject prefab)
    {
        if (_pools.TryGetValue(prefabPath, out var pool) && pool.Count > 0)
        {
            var obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        return Instantiate(prefab);
    }
    
    public void Release(string prefabPath, GameObject obj)
    {
        obj.SetActive(false);
        if (!_pools.ContainsKey(prefabPath))
            _pools[prefabPath] = new Queue<GameObject>();
        _pools[prefabPath].Enqueue(obj);
    }
}

// 在 ViewLoaderSystem 的 LoadViewForEntity 中
GameObject instance = SpineViewPool.Instance.Get(prefabPath, prefab);
```

### 4. **批量更新优化** ⭐⭐⭐
**效果**: 15-30% 性能提升

**原理**: 减少重复查询

**实现方式**:
```csharp
protected override void UpdateViews()
{
    // 收集需要更新的实体
    var updateList = new List<(Entity, ViewInstanceComponent)>();
    
    foreach (var (viewInstance, entity) in ...)
    {
        if (ShouldUpdate(entity))
            updateList.Add((entity, viewInstance));
    }
    
    // 批量处理
    foreach (var (entity, viewInstance) in updateList)
    {
        UpdateEntity(entity, viewInstance);
    }
}
```

### 5. **颜色缓存** ⭐⭐
**效果**: 5-10% 性能提升

**原理**: 避免重复设置相同颜色

**实现方式**:
```csharp
private Dictionary<SkeletonAnimation, Color> _colorCache = new Dictionary<SkeletonAnimation, Color>();

private void UpdateSpineColor(SkeletonAnimation skeleton, ref ViewStateComponent viewState)
{
    Color targetColor = new Color(viewState.ColorTint, viewState.ColorTint, viewState.ColorTint);
    
    if (_colorCache.TryGetValue(skeleton, out var cached) && cached == targetColor)
        return;
    
    skeleton.skeleton.R = targetColor.r;
    skeleton.skeleton.G = targetColor.g;
    skeleton.skeleton.B = targetColor.b;
    _colorCache[skeleton] = targetColor;
}
```

## 配置建议

### 小场景 (< 50 Spine)
- 启用视锥剔除
- 不需要 LOD

### 中场景 (50-200 Spine)
- 启用视锥剔除
- LOD 距离: (10m, 20m, 30m)
- 对象池预热: 30个

### 大场景 (> 200 Spine)
- 启用视锥剔除
- LOD 距离: (8m, 15m, 25m) - 更激进
- 对象池预热: 50-100个

## 性能提升预期

| 场景规模 | 无优化 | 基础优化 | 完整优化 |
|---------|-------|---------|---------|
| 50 Spine | 45 FPS | 55 FPS | 60 FPS |
| 200 Spine | 30 FPS | 45 FPS | 60 FPS |
| 500 Spine | 15 FPS | 30 FPS | 55 FPS |

## 实施步骤

1. **先修复项目编译错误**
   - 添加 Spine 包
   - 补充缺失的 Config 和 GameLogger 等依赖

2. **实施视锥剔除**（最简单，效果最明显）
   - 修改 `SpineViewSystem.cs`
   - 添加相机视锥检测

3. **添加 LOD 系统**
   - 基于距离的帧跳跃
   - 可配置的距离阈值

4. **集成对象池**
   - 创建池管理器
   - 修改 ViewLoaderSystem

5. **测试和调优**
   - 使用 Unity Profiler 验证效果
   - 根据实际情况调整参数

## 注意事项

1. **先解决编译问题**: 当前项目缺少一些依赖，需要先修复
2. **渐进式优化**: 一次实施一个优化，测试效果
3. **保持视觉质量**: LOD 不要过于激进，注意用户体验
4. **内存平衡**: 对象池预热会占用内存，按需配置

## 建议

现阶段最适合的优化顺序：
1. ✅ 修复编译错误
2. ✅ 实施视锥剔除（投入产出比最高）
3. ✅ 添加对象池（减少 GC）
4. ✅ 实施 LOD（大场景必需）
5. ⭐ 其他细节优化

---

如需具体的代码实现或遇到问题，可以根据上述建议逐步实施。
