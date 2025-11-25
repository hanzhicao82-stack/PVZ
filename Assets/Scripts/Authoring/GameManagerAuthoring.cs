using Unity.Entities;
using UnityEngine;

namespace PVZ.DOTS.Authoring
{
    /// <summary>
    /// 游戏管理器 Authoring - 管理游戏全局配置
    /// </summary>
    public class GameManagerAuthoring : MonoBehaviour
    {
        [Header("游戏配置")]
        public int gridRows = 5;
        public int gridColumns = 9;
        public float gridCellWidth = 1.5f;
        public float gridCellHeight = 2f;

        [Header("初始资源")]
        public int startingSun = 150;

        [Header("僵尸生成")]
        public float zombieSpawnInterval = 5f;
        public float zombieSpawnStartDelay = 10f;

        class Baker : Baker<GameManagerAuthoring>
        {
            public override void Bake(GameManagerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                // 这里可以添加游戏配置组件
                // 例如：AddComponent(entity, new GameConfigComponent { ... });
            }
        }
    }
}
