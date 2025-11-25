# 游戏UI设置说明

## UI Canvas结构

在Unity场景中创建以下UI结构：

```
Canvas (Screen Space - Overlay)
├── GameUI
│   ├── TopPanel
│   │   ├── TimerText (Text)
│   │   ├── WaveText (Text)
│   │   └── ZombiesKilledText (Text)
│   ├── StateText (Text) - 居中显示
│   ├── VictoryPanel (Panel)
│   │   ├── VictoryText (Text): "胜利！"
│   │   ├── RestartButton (Button)
│   │   └── QuitButton (Button)
│   └── DefeatPanel (Panel)
│       ├── DefeatText (Text): "失败"
│       ├── RestartButton (Button)
│       └── QuitButton (Button)
```

## 组件配置步骤

1. 创建Canvas (右键 UI > Canvas)
2. 在Canvas下创建空GameObject命名为"GameUI"
3. 添加`GameUIManager`脚本到"GameUI"对象
4. 配置引用：
   - TimerText: 拖拽Timer Text组件
   - WaveText: 拖拽Wave Text组件
   - StateText: 拖拽State Text组件
   - ZombiesKilledText: 拖拽Zombies Killed Text组件
   - VictoryPanel: 拖拽Victory Panel
   - DefeatPanel: 拖拽Defeat Panel

5. 配置按钮事件：
   - RestartButton.OnClick() -> GameUIManager.OnRestartGame()
   - QuitButton.OnClick() -> GameUIManager.OnQuitGame()
   - 添加开始按钮调用GameUIManager.OnStartGame()

## Text组件推荐设置

- TimerText:
  - Font Size: 36
  - Alignment: 右上角
  - Color: White
  
- WaveText:
  - Font Size: 28
  - Alignment: 左上角
  - Color: White

- StateText:
  - Font Size: 48
  - Alignment: 中心
  - Best Fit: Enable
  - Color: 动态变化（脚本控制）

## 游戏流程

1. 场景加载 -> GameConfigLoader初始化GameStateComponent (状态: Preparing)
2. 玩家点击开始按钮 -> GameUIManager.OnStartGame() -> 状态变为Playing
3. GameLoopSystem开始倒计时
4. 时间耗尽或僵尸到达终点 -> 显示胜利/失败面板
