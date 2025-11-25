using Unity.Entities;
using Unity.Mathematics;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Systems
{
    /// <summary>
    /// 阳光生产系统 - 处理向日葵生产阳光的逻辑
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct SunProductionSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // 遍历所有阳光生产者
            foreach (var (sunProducer, plant) in 
                SystemAPI.Query<RefRW<SunProducerComponent>, RefRO<PlantComponent>>())
            {
                // 检查是否到达生产时间
                if (currentTime - sunProducer.ValueRO.LastProductionTime >= sunProducer.ValueRO.ProductionInterval)
                {
                    // 这里可以触发阳光生成事件或增加游戏资源
                    // 实际实现需要与游戏管理系统配合
                    
                    sunProducer.ValueRW.LastProductionTime = currentTime;
                    
                    // TODO: 创建阳光实体或更新游戏资源
                }
            }
        }
    }
}
