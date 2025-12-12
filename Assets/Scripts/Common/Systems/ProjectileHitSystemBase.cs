using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

namespace Common
{
    /// <summary>
    /// 子弹碰撞检测系统抽象基类 - 提供通用的碰撞检测框架
    /// 子类需要实现具体的目标查询和伤害应用逻辑
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public abstract partial class ProjectileHitSystemBase : SystemBase
    {
        protected const float DEFAULT_COLLISION_RADIUS = 0.5f;
        protected const float DEFAULT_COLLISION_RADIUS_SQ = DEFAULT_COLLISION_RADIUS * DEFAULT_COLLISION_RADIUS;

        /// <summary>
        /// 获取碰撞半径，可由子类重写
        /// </summary>
        protected virtual float GetCollisionRadius() => DEFAULT_COLLISION_RADIUS;

        /// <summary>
        /// 获取碰撞半径的平方，可由子类重写
        /// </summary>
        protected virtual float GetCollisionRadiusSq() => DEFAULT_COLLISION_RADIUS_SQ;

        protected override void OnUpdate()
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            // 执行碰撞检测
            ProcessCollisions(ecb);

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        /// <summary>
        /// 处理碰撞检测，由子类实现
        /// </summary>
        protected abstract void ProcessCollisions(EntityCommandBuffer ecb);

        /// <summary>
        /// 检查两个位置是否发生碰撞
        /// </summary>
        protected bool IsColliding(float3 pos1, float3 pos2, float radiusSq)
        {
            float distanceSq = math.distancesq(pos1, pos2);
            return distanceSq <= radiusSq;
        }

        /// <summary>
        /// 检查两个位置是否发生碰撞（使用默认半径）
        /// </summary>
        protected bool IsColliding(float3 pos1, float3 pos2)
        {
            return IsColliding(pos1, pos2, GetCollisionRadiusSq());
        }
    }
}
