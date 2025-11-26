# CombatSystem 性能优化说明

## 问题分析

### 原始实现的性能问题

```csharp
// 嵌套循环：每个子弹检查所有僵尸
foreach (子弹) {
    foreach (僵尸) {
        if (不在同一行) continue;  // 但仍然遍历了
        检查距离...
    }
}
```

**性能开销：**
- 复杂度：O(P × Z) 其中 P=子弹数，Z=僵尸数
- 1000个单位时：假设500子弹 × 500僵尸 = 250,000 次循环
- 每帧都进行完整遍历
- 大量无效的行检查（不在同一行的僵尸也被遍历）
- 使用 `math.distance()` 包含开方运算，代价较高

## 优化方案

### 1. 使用空间分区（行索引）

```csharp
// 构建行索引
NativeParallelMultiHashMap<int, Entity> zombiesByLane;
foreach (僵尸) {
    zombiesByLane.Add(僵尸.行, 僵尸实体);
}

// 子弹只检查同行僵尸
foreach (子弹) {
    foreach (同行的僵尸 in zombiesByLane[子弹.行]) {
        检查距离...
    }
}
```

**优化效果：**
- 复杂度：O(P + Z + P × Z_lane) 其中 Z_lane=每行平均僵尸数
- 如果有5行：500子弹 × (500僵尸/5行) = 50,000 次循环
- **减少了 80% 的循环次数**

### 2. 使用平方距离

```csharp
// 原来：使用 math.distance() 需要开方
float distance = math.distance(pos1, pos2);
if (distance < 0.5f)

// 优化：使用 math.distancesq() 避免开方
float distanceSq = math.distancesq(pos1, pos2);
if (distanceSq < 0.25f)  // 0.5^2 = 0.25
```

**优化效果：**
- 避免了 sqrt() 运算，约快 2-3 倍
- 在循环中效果显著

### 3. 只使用 XZ 平面距离

```csharp
// 原来：使用 XY 平面（但游戏在 XZ 平面）
float distance = math.distance(pos.xy, pos2.xy);

// 优化：使用正确的 XZ 平面
float distanceSq = math.distancesq(pos.xz, pos2.xz);
```

### 4. 提前跳出循环

```csharp
bool hitTarget = false;
do {
    if (碰撞) {
        处理碰撞...
        hitTarget = true;
        break;  // 子弹击中目标后立即退出
    }
} while (...&& !hitTarget);
```

### 5. 缓存常量

```csharp
private const float COLLISION_RADIUS = 0.5f;
private const float COLLISION_RADIUS_SQ = 0.25f;  // 预计算
```

## 性能对比

| 场景 | 原始实现 | 优化后 | 提升 |
|------|---------|--------|------|
| 100单位 (50子弹×50僵尸) | 2,500次循环 | ~500次循环 | **5倍** |
| 500单位 (250子弹×250僵尸) | 62,500次循环 | ~12,500次循环 | **5倍** |
| 1000单位 (500子弹×500僵尸) | 250,000次循环 | ~50,000次循环 | **5倍** |

**实际帧时间预估：**
- 原始：1000单位时可能 5-10ms
- 优化后：1000单位时约 1-2ms
- **减少 70-80% 的时间开销**

## 进一步优化建议

如果性能仍有问题，可以考虑：

1. **使用 Burst 编译器**
   ```csharp
   [BurstCompile]
   public partial struct CombatSystem : ISystem
   ```

2. **使用 Job 并行化**
   - 将碰撞检测拆分到多个线程
   - 使用 IJobEntity 或 IJobParallelFor

3. **使用更精细的空间分区**
   - 除了行索引，还可以加列索引
   - 使用网格哈希表 (Spatial Hash)

4. **减少组件访问次数**
   - 缓存组件引用
   - 批量处理伤害

5. **使用物理引擎**
   - Unity Physics 或 Havok Physics
   - 利用硬件加速的碰撞检测

## 代码维护说明

- `NativeParallelMultiHashMap` 必须手动 Dispose
- `EntityManager.Exists()` 检查避免访问已销毁的实体
- 使用 `GameLogger` 而非 `Debug.Log` 提高性能
