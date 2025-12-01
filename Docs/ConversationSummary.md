# 对话摘要

## 修改内容
- 为植物和僵尸组件及其配置结构新增子弹预制体路径字段，并更新 `GameConfig.json`、`GameConfigLoader`、各类 Authoring 及测试脚本，使子弹外观可配置。
- 新增子弹视图相关脚本：`ProjectileViewComponent`、`ProjectileViewPool`、`ProjectileViewSystem`、`ProjectileViewCleanupSystem`，通过对象池管理子弹的 GameObject 实例并与 DOTS 实体同步。
- 调整 `PlantAttackSystem` 生成子弹时附加 `ProjectileViewPrefabComponent`，`ZombieSpawnSystem` 读取默认僵尸子弹预制体路径，为后续僵尸发射子弹铺路。

## 关键解释
- `ProjectileViewCleanupSystem` 由 `PresentationSystemGroup` 每帧自动调度，检测失去 `ProjectileComponent` 的实体并回收其 GameObject。
- `EntityCommandBuffer.DestroyEntity` 会销毁实体以及其全部组件；如需仅移除单个组件应使用 `RemoveComponent<T>`。
- `ProjectileViewComponent` 作为 `ICleanupComponentData` 挂在子弹实体上，确保实体被销毁后还能暂存托管引用，下一帧由清理系统回收。
- `ICleanupComponentData` 组件在实体被标记销毁后仍会保留一帧（或直到 ECS 播放完所有 ECB），给系统留出处理和释放托管资源的窗口。
