using Unity.Collections;
using Unity.Entities;

namespace Common
{
    /// <summary>
    /// 攻击系统抽象基类 - 封装通用的攻击逻辑和命令缓冲区管理
    /// 子类需要实现具体的攻击执行逻辑
    /// </summary>
    public abstract partial class AttackSystemBase : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            // 确保在没有 GameStateComponent 时系统不会运行，避免时序问题或日志噪音
            RequireForUpdate<GameStateComponent>();
        }

        /// <summary>
        /// 创建临时命令缓冲区，统一管理生命周期
        /// </summary>
        protected EntityCommandBuffer CreateCommandBuffer() => new EntityCommandBuffer(Allocator.Temp);

        /// <summary>
        /// 回放并释放命令缓冲区
        /// </summary>
        protected void PlaybackAndDispose(EntityCommandBuffer ecb)
        {
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        protected override void OnUpdate()
        {
            // If GameStateComponent is missing (e.g., test scenes), silently skip attacks.
            if (!SystemAPI.TryGetSingletonRW<GameStateComponent>(out var gameState))
                return;

            if (gameState.ValueRO.CurrentState == GameState.Playing)
            {
                ExecuteAttack();
            }
        }

        /// <summary>
        /// 执行具体的攻击逻辑，由子类实现
        /// </summary>
        protected abstract void ExecuteAttack();

        /// <summary>
        /// 判断同一行是否存在位于当前位置前方的目标
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
    }
}
