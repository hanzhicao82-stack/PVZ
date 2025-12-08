using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Systems
{
    /// <summary>
    /// 子弹移动系统 - 处理子弹的移动和销毁逻辑（性能优化版）
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct ProjectileMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 可以在这里初始化
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbParallel = ecb.AsParallelWriter();

            // 使用 Job 并行处理子弹移动
            new ProjectileMoveJob
            {
                DeltaTime = deltaTime,
                ECB = ecbParallel,
                MinBound = -20f,
                MaxBound = 20f
            }.ScheduleParallel();

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        /// <summary>
        /// 子弹移动和边界检查的 Job（Burst 编译，并行执行）
        /// </summary>
        [BurstCompile]
        partial struct ProjectileMoveJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;
            public float MinBound;
            public float MaxBound;

            void Execute(
                ref LocalTransform transform,
                in ProjectileComponent projectile,
                Entity entity,
                [ChunkIndexInQuery] int chunkIndex)
            {
                // 更新位置
                float3 newPosition = transform.Position + projectile.Direction * projectile.Speed * DeltaTime;
                transform.Position = newPosition;

                // 边界检查（快速剔除）
                if (newPosition.x < MinBound || newPosition.x > MaxBound)
                {
                    ECB.DestroyEntity(chunkIndex, entity);
                }
            }
        }
    }
}
