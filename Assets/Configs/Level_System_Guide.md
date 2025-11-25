# 关卡系统使用指南

## 概述

关卡系统允许通过JSON配置文件定义不同类型的地图关卡，支持多样化的游戏体验。

## 关卡类型

### 1. Day (白天关卡)
- **特点**: 基础关卡，阳光充足
- **可用植物**: 所有白天植物
- **僵尸**: 普通僵尸为主
- **难度**: 简单到中等

### 2. Night (夜晚关卡)
- **特点**: 阳光稀缺，需要蘑菇植物
- **限制**: 向日葵效率降低或不可用
- **特殊**: 可能出现墓碑
- **难度**: 中等

### 3. Pool (泳池关卡)
- **特点**: 6行地图，中间两行是泳池
- **特殊植物**: 需要莲叶才能在水上种植
- **僵尸**: 包括水上僵尸
- **难度**: 中等到困难

### 4. Fog (迷雾关卡)
- **特点**: 视野受限，只能看到近处的僵尸
- **策略**: 需要灯笼花或三叶草驱散迷雾
- **难度**: 困难

### 5. Roof (屋顶关卡)
- **特点**: 斜面地图，直线攻击变为抛物线
- **特殊**: 投手植物有优势
- **限制**: 不能种植地刺等地面植物
- **难度**: 困难

### 6. BossFight (Boss战)
- **特点**: 超长战斗时间，大量波次
- **Boss**: 特殊僵尸Boss
- **奖励**: 丰厚
- **难度**: 专家级

## JSON配置结构

```json
{
  "levelId": 1,
  "levelName": "白天1-1",
  "type": "Day",
  "difficulty": "Easy",
  "mapConfig": {
    "rowCount": 5,
    "columnCount": 9,
    "cellWidth": 1.5,
    "cellHeight": 2.0
  },
  "gameRules": {
    "gameDuration": 120.0,
    "totalWaves": 3,
    "maxZombiesReached": 5,
    "startingSun": 150,
    "waveIntensityMultiplier": 1.2
  },
  "zombieSpawn": {
    "interval": 8.0,
    "startDelay": 10.0
  },
  "specialRules": {
    "hasFog": false,
    "hasPool": false,
    "hasRoof": false,
    "isNightLevel": false,
    "hasGrave": false,
    "isBossLevel": false
  },
  "waves": [
    {
      "waveNumber": 1,
      "zombieType": "Normal",
      "count": 3,
      "spawnDelay": 0.0
    }
  ],
  "availablePlants": ["Peashooter", "Sunflower", "WallNut"]
}
```

## 使用方法

### 方法1: 通过SceneInitializer自动加载

1. 在GameManager上添加`SceneInitializer`脚本
2. 配置以下参数：
   - ✅ `autoLoadLevel = true`
   - 拖拽 `LevelConfig.json` 到 `levelConfigJson`
   - 设置 `startLevelId` (默认1)
3. 运行场景，自动加载指定关卡

### 方法2: 手动创建LevelConfigLoader

1. 在场景中创建GameObject "LevelConfigLoader"
2. 添加 `LevelConfigLoader` 组件
3. 拖拽 `LevelConfig.json` 到 `levelConfigJson`
4. 设置 `loadOnStart = true` 和 `levelToLoad`
5. 运行场景

### 方法3: 使用菜单命令

在Unity编辑器中：
- `PVZ > Load Level 1` - 加载第1关
- `PVZ > Load Level 2` - 加载第2关

## 关卡配置参数说明

### mapConfig (地图配置)
- `rowCount`: 行数 (5行或6行泳池)
- `columnCount`: 列数 (通常9列)
- `cellWidth`: 格子宽度
- `cellHeight`: 格子高度

### gameRules (游戏规则)
- `gameDuration`: 关卡时长（秒）
- `totalWaves`: 总波次数
- `maxZombiesReached`: 最多允许到达终点的僵尸数
- `startingSun`: 初始阳光值
- `waveIntensityMultiplier`: 波次强度倍数（每波僵尸数量递增）

### zombieSpawn (僵尸生成)
- `interval`: 生成间隔（秒）
- `startDelay`: 开始延迟（秒）

### specialRules (特殊规则)
- `hasFog`: 是否有迷雾
- `hasPool`: 是否有泳池
- `hasRoof`: 是否是屋顶
- `isNightLevel`: 是否是夜晚
- `hasGrave`: 是否有墓碑
- `isBossLevel`: 是否是Boss战

### waves (波次配置)
每个波次定义：
- `waveNumber`: 波次编号 (1, 2, 3...)
- `zombieType`: 僵尸类型 ("Normal", "ConeHead", "BucketHead", "Flag")
- `count`: 该类型僵尸数量
- `spawnDelay`: 该波开始后的生成延迟（秒）

### availablePlants (可用植物)
该关卡允许使用的植物列表：
- "Peashooter" - 豌豆射手
- "Sunflower" - 向日葵
- "WallNut" - 坚果墙
- "SnowPea" - 寒冰射手
- "Repeater" - 双发射手

## 创建新关卡

1. 编辑 `Assets/Configs/LevelConfig.json`
2. 在 `levels` 数组中添加新关卡配置
3. 设置唯一的 `levelId`
4. 根据关卡类型配置地图和规则
5. 定义波次和僵尸组成
6. 指定可用植物列表

## 系统集成

- `LevelConfigComponent` - ECS组件存储关卡配置
- `WaveConfigElement` - 动态缓冲存储波次配置
- `LevelPlantUnlockElement` - 动态缓冲存储植物解锁
- `LevelManagementSystem` - ECS系统管理波次推进
- `LevelConfigLoader` - MonoBehaviour加载JSON到ECS

## 示例关卡

配置文件包含5个示例关卡：
1. **白天1-1** (简单) - 3波，120秒
2. **白天1-2** (普通) - 5波，180秒
3. **夜晚2-1** (普通) - 6波，200秒，有墓碑
4. **泳池3-1** (普通) - 7波，240秒，6行地图
5. **屋顶5-1 Boss战** (专家) - 10波，300秒，大量强力僵尸

## 注意事项

1. `levelId` 必须唯一
2. `availablePlants` 必须匹配 `PlantType` 枚举
3. `waves` 中的 `zombieType` 必须匹配 `ZombieType` 枚举
4. 泳池关卡 `rowCount` 应设为 6
5. Boss关卡建议 `maxZombiesReached` 设为3以增加难度
