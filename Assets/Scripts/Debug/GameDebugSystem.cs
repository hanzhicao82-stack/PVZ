using Unity.Entities;


namespace Debug
{
    /// <summary>
    /// 游戏调试系统 - 使用 Gizmos 绘制游戏对象
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class GameDebugSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // 这个系统主要用于触发 Gizmos 绘制
            // 实际绘制�?GameDebugDrawer 中进�?
        }
    }
}
