using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using PVZ;

namespace Common
{
    /// <summary>
    /// 瑙嗗浘娓呯悊绯荤粺 - 璐熻矗娓呯悊琚攢姣佸疄浣撶殑瑙嗗浘瀹炰緥锛堟€ц兘浼樺寲鐗堬級
    /// 褰撳疄浣撹閿€姣佹椂锛岃嚜鍔ㄩ攢姣佸搴旂殑 GameObject
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
    public partial class ViewCleanupSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;
        private int _frameCounter = 0;
        private const int CLEANUP_CHECK_FREQUENCY = 10; // 锟?0甯ф鏌ヤ竴锟?

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            
            // 鏍规嵁閰嶇疆鍐冲畾鏄惁鍚敤
            var config = ViewSystemConfig.Instance;
            if (!config.enableSpineSystem && !config.enableMeshRendererSystem)
            {
                Enabled = false;
                UnityEngine.Debug.Log("ViewCleanupSystem is disabled (no view rendering enabled).");
            }
        }

        protected override void OnUpdate()
        {
            // 闄嶄綆妫€鏌ラ鐜囷紙娓呯悊鏄綆棰戞搷浣滐級
            _frameCounter++;
            if (_frameCounter % CLEANUP_CHECK_FREQUENCY != 0)
            {
                return;
            }

            // 娓呯悊閭ｄ簺瀹炰綋宸茶閿€姣佷絾 GameObject 杩樺瓨鍦ㄧ殑瑙嗗浘
            // 娉ㄦ剰锛氱敱锟?ViewInstanceComponent 锟?ICleanupComponentData锟?
            // 褰撳疄浣撻攢姣佹椂锛岀粍浠朵細淇濈暀锛岄渶瑕佹墜鍔ㄦ竻锟?GameObject
            var ecb = _ecbSystem.CreateCommandBuffer();
            
            foreach (var entity in SystemAPI.QueryBuilder()
                .WithAll<ViewInstanceComponent>()
                .WithNone<LocalTransform>()
                .Build()
                .ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                if (EntityManager.HasComponent<ViewInstanceComponent>(entity))
                {
                    var viewInstance = EntityManager.GetComponentData<ViewInstanceComponent>(entity);
                    if (viewInstance.GameObjectInstance != null)
                    {
                        Object.Destroy(viewInstance.GameObjectInstance);
                        viewInstance.GameObjectInstance = null;
                    }
                    // 绉婚櫎娓呯悊缁勪欢
                    ecb.RemoveComponent<ViewInstanceComponent>(entity);
                }
            }
        }
    }
}
