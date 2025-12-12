using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

namespace Common
{
    /// <summary>
    /// 子弹移动系统抽象基类 - 提供通用的子弹移动和边界检查逻辑
    /// 子类需要实现具体的子弹移动逻辑
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public abstract partial class ProjectileMovementSystemBase : SystemBase
    {
        protected float MinBound { get; set; } = -20f;
        protected float MaxBound { get; set; } = 20f;

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // 执行子弹移动，由子类实现
            ProcessProjectileMovement(deltaTime, ecb);

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        /// <summary>
        /// 处理子弹移动，由子类实现具体逻辑
        /// </summary>
        protected abstract void ProcessProjectileMovement(float deltaTime, EntityCommandBuffer ecb);

        /// <summary>
        /// 检查位置是否超出边界
        /// </summary>
        protected virtual bool IsOutOfBounds(float3 position)
        {
            return position.x < MinBound || position.x > MaxBound ||
                   position.y < MinBound || position.y > MaxBound ||
                   position.z < MinBound || position.z > MaxBound;
        }

        /// <summary>
        /// 设置移动边界
        /// </summary>
        protected void SetBounds(float min, float max)
        {
            MinBound = min;
            MaxBound = max;
        }
    }
}
