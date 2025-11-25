# Main场景配置指南

## 快速设置（推荐方式）

### 方法1：使用菜单命令（最快）
1. 在Unity编辑器中打开 `Assets/Scenes/SampleScene.unity`
2. 点击菜单：`PVZ > Setup Main Scene`
3. 在Hierarchy中选中新创建的"GameManager"对象
4. 在Inspector中拖拽 `Assets/Configs/GameConfig.json` 到 `Game Config Json` 字段
5. 点击Play运行游戏

### 方法2：手动创建
1. 在Unity中新建场景（或使用现有的SampleScene）
2. 右键Hierarchy > Create Empty，命名为"GameManager"
3. 添加 `SceneInitializer` 脚本到GameManager
4. 配置SceneInitializer：
   - ✅ Auto Create UI: true
   - ✅ Auto Create Config Loader: true
   - 拖拽 `GameConfig.json` 到 `Game Config Json` 字段
5. 运行场景

## 场景自动创建的内容

运行场景时，SceneInitializer会自动创建：

### 1. GameConfigLoader
- 加载 `GameConfig.json`
- 初始化 `GameConfigComponent` (僵尸生成配置)
- 初始化 `GameStateComponent` (游戏状态、倒计时)
- 填充植物/僵尸配置缓冲

### 2. Canvas (UI系统)
```
Canvas
└── GameUI (GameUIManager脚本)
    ├── TopPanel
    │   ├── TimerText: "时间: 03:00" (右上角)
    │   ├── WaveText: "波次: 0/5" (左上角)
    │   └── ZombiesKilledText: "击杀: 0" (中上)
    ├── StateText: "准备中..." (屏幕中心)
    ├── VictoryPanel (初始隐藏)
    │   ├── ResultText: "胜利！"
    │   ├── RestartButton
    │   └── QuitButton
    └── DefeatPanel (初始隐藏)
        ├── ResultText: "失败"
        ├── RestartButton
        └── QuitButton
```

## 必需的ECS Systems（自动运行）

以下系统在DOTS环境中自动注册：
- ✅ `GameLoopSystem` - 倒计时、胜负判定
- ✅ `ZombieSpawnSystem` - 僵尸生成
- ✅ `ZombieMovementSystem` - 僵尸移动
- ✅ `PlantAttackSystem` - 植物攻击
- ✅ `ProjectileMovementSystem` - 子弹移动
- ✅ `CombatSystem` - 碰撞检测与伤害
- ✅ `SunProductionSystem` - 阳光生成

## 测试游戏流程

1. 场景加载后，GameStateComponent状态为 `Preparing`
2. UI显示"准备中..."
3. 使用F1-F4快捷键测试：
   - **F1**: 生成豌豆射手 (x=-5, lane=2)
   - **F2**: 生成向日葵 (x=-3, lane=2)
   - **F3**: 生成坚果墙 (x=-2, lane=2)
   - **F4**: 生成普通僵尸 (x=10, lane=2)
4. 或者在代码中调用 `GameUIManager.OnStartGame()` 开始游戏
5. 观察倒计时、僵尸移动、植物攻击效果

## 游戏胜负条件

**胜利条件**：
- 时间耗尽时场上没有僵尸

**失败条件**：
- 5个僵尸到达终点 (x < -8)
- 或时间耗尽时仍有僵尸存活

## 配置调整

修改 `Assets/Configs/GameConfig.json`:
```json
{
  "gameSettings": {
    "gameDuration": 180.0,      // 游戏时长（秒）
    "totalWaves": 5,            // 总波次
    "maxZombiesReached": 5      // 允许到达终点的僵尸数
  }
}
```

## 注意事项

1. **Unity版本**：需要Unity 2022.3+ 和 Entities 1.0+
2. **TextMeshPro**：如需更好的文本渲染，可替换Text为TextMeshProUGUI
3. **调试工具**：
   - 启用 `GameDebugDrawer` 查看Gizmos可视化
   - 使用 `GameTestSetup` 的F1-F4快捷键快速测试
4. **性能优化**：ECS系统自动多线程，支持大量实体
