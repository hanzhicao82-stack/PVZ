using Unity.Collections;
using Unity.Entities;
using Common;

namespace Game.TowerDefense
{
    /// <summary>
    /// Attack 系统基类，封装通用的时间计算与命令缓冲区生命周期管理�?
    /// 子类只需关注具体的攻击执行逻辑�?
    /// </summary>
    public abstract partial class AttackSystemBase : SystemBase
    {
        /// <summary>
        /// 创建临时命令缓冲区，统一管理生命周期�?
        /// </summary>
        protected EntityCommandBuffer CreateCommandBuffer() => new EntityCommandBuffer(Allocator.Temp);

        /// <summary>
        /// 回放并释放命令缓冲区�?
        /// </summary>
        protected void PlaybackAndDispose(EntityCommandBuffer ecb)
        {
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        protected override void OnUpdate()
        {
            if (!SystemAPI.TryGetSingletonRW<GameStateComponent>(out var gameState))
            {
                UnityEngine.Debug.LogWarning("GameLoopSystem: 无法获取 GameStateComponent，这不应该发生！");
                return;
            }

            if (gameState.ValueRO.CurrentState == GameState.Playing)
            {
                _OnUpdate();
            }
        }

        /// <summary>
        /// 判断同一行是否存在位于当前位置前方的目标�?
        /// </summary>
        protected static bool HasTargetAhead(NativeParallelMultiHashMap<int, float> laneIndex, int lane, float currentX)
        {
            if (laneIndex.TryGetFirstValue(lane, out float targetX, out var iterator))
            {
                do
                {
                    if (targetX > currentX)
                        return true;
                }
                while (laneIndex.TryGetNextValue(out targetX, ref iterator));
            }

            return false;
        }

        protected virtual void _OnUpdate()
        {

        }

        
    }
}
