using Unity.Entities;
using Common;

namespace Game.TowerDefense
{
    /// <summary>
    /// TowerDefense 游戏循环系统基类，继承自通用 GameLoopSystemBase
    /// 在通用基类基础上可以添加塔防游戏特定的循环逻辑
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public abstract partial class GameLoopSystemBase : Common.GameLoopSystemBase
    {
        // TowerDefense 游戏特定的循环逻辑可以在这里添加
    }
}
