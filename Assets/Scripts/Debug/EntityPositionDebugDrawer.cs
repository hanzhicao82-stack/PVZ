using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Common;
using PVZ;
using Game.TowerDefense;

namespace Debug
{
    /// <summary>
    /// 实体位置和网格坐标调试工�?
    /// 绘制植物和僵尸在地图上的位置
    /// </summary>
    public class EntityPositionDebugDrawer : MonoBehaviour
    {
        [Header("实体可视化设置")]
        [Tooltip("是否显示植物")]
        public bool showPlants = true;
        
        [Tooltip("是否显示僵尸")]
        public bool showZombies = true;
        
        [Tooltip("是否显示抛射物")]
        public bool showProjectiles = true;

        [Header("颜色设置")]
        public Color plantColor = Color.green;
        public Color zombieColor = Color.red;
        public Color projectileColor = Color.yellow;

        [Header("尺寸设置")]
        [Range(0.1f, 1f)]
        public float plantSize = 0.5f;
        [Range(0.1f, 1f)]
        public float zombieSize = 0.5f;
        [Range(0.05f, 0.3f)]
        public float projectileSize = 0.1f;

        [Tooltip("是否显示格子坐标")]
        public bool showGridCoordinates = true;

        private World gameWorld;
        private EntityManager entityManager;

        private void Start()
        {
            gameWorld = World.DefaultGameObjectInjectionWorld;
            if (gameWorld != null)
            {
                entityManager = gameWorld.EntityManager;
            }
        }

        private void OnDrawGizmos()
        {
            if (gameWorld == null || !gameWorld.IsCreated)
            {
                gameWorld = World.DefaultGameObjectInjectionWorld;
                if (gameWorld == null) return;
                entityManager = gameWorld.EntityManager;
            }

            // 绘制植物
            if (showPlants)
            {
                DrawPlants();
            }

            // 绘制僵尸
            if (showZombies)
            {
                DrawZombies();
            }

            // 绘制抛射�?
            if (showProjectiles)
            {
                DrawProjectiles();
            }
        }

        private void DrawPlants()
        {
            var query = entityManager.CreateEntityQuery(typeof(PlantComponent), typeof(LocalTransform), typeof(GridPositionComponent));
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            
            Gizmos.color = plantColor;
            foreach (var entity in entities)
            {
                var transform = entityManager.GetComponentData<LocalTransform>(entity);
                var gridPos = entityManager.GetComponentData<GridPositionComponent>(entity);
                var plant = entityManager.GetComponentData<PlantComponent>(entity);

                // 绘制植物位置
                Gizmos.DrawWireSphere(transform.Position, plantSize);
                Gizmos.DrawCube(transform.Position + new float3(0, 0.5f, 0), new float3(plantSize, 0.1f, plantSize));

                // 绘制格子坐标
                if (showGridCoordinates)
                {
#if UNITY_EDITOR
                    UnityEditor.Handles.color = plantColor;
                    Vector3 labelPos = transform.Position + new float3(0, 1f, 0);
                    UnityEditor.Handles.Label(labelPos, $"P({gridPos.Row},{gridPos.Column})\n{plant.Type}");
#endif
                }
            }

            entities.Dispose();
            query.Dispose();
        }

        private void DrawZombies()
        {
            var query = entityManager.CreateEntityQuery(typeof(ZombieComponent), typeof(LocalTransform));
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            
            Gizmos.color = zombieColor;
            foreach (var entity in entities)
            {
                var transform = entityManager.GetComponentData<LocalTransform>(entity);
                var zombie = entityManager.GetComponentData<ZombieComponent>(entity);

                // 绘制僵尸位置
                Gizmos.DrawWireCube(transform.Position, new Vector3(zombieSize, zombieSize * 2, zombieSize));
                
                // 绘制移动方向指示�?
                float3 forward = new float3(-1, 0, 0) * zombieSize * 1.5f;
                Gizmos.DrawLine(transform.Position, transform.Position + forward);

                // 绘制僵尸类型
                if (showGridCoordinates)
                {
#if UNITY_EDITOR
                    UnityEditor.Handles.color = zombieColor;
                    Vector3 labelPos = transform.Position + new float3(0, 1.5f, 0);
                    UnityEditor.Handles.Label(labelPos, $"Z({zombie.Type})");
#endif
                }
            }

            entities.Dispose();
            query.Dispose();
        }

        private void DrawProjectiles()
        {
            var query = entityManager.CreateEntityQuery(typeof(ProjectileComponent), typeof(LocalTransform));
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            
            Gizmos.color = projectileColor;
            foreach (var entity in entities)
            {
                var transform = entityManager.GetComponentData<LocalTransform>(entity);
                var projectile = entityManager.GetComponentData<ProjectileComponent>(entity);

                // 绘制抛射�?
                Gizmos.DrawSphere(transform.Position, projectileSize);
                
                // 绘制移动轨迹指示
                float3 direction = new float3(projectile.Direction.x, projectile.Direction.y, projectile.Direction.z) * projectileSize * 3;
                Gizmos.DrawLine(transform.Position, transform.Position + direction);
            }

            entities.Dispose();
            query.Dispose();
        }

        private void OnGUI()
        {
            if (gameWorld == null || !gameWorld.IsCreated)
                return;

            // 统计实体数量
            var plantQuery = entityManager.CreateEntityQuery(typeof(PlantComponent));
            var zombieQuery = entityManager.CreateEntityQuery(typeof(ZombieComponent));
            var projectileQuery = entityManager.CreateEntityQuery(typeof(ProjectileComponent));

            int plantCount = plantQuery.CalculateEntityCount();
            int zombieCount = zombieQuery.CalculateEntityCount();
            int projectileCount = projectileQuery.CalculateEntityCount();

            plantQuery.Dispose();
            zombieQuery.Dispose();
            projectileQuery.Dispose();

            // 显示实体统计
            GUILayout.BeginArea(new Rect(10, 360, 300, 120));
            GUILayout.Box("实体统计");
            GUILayout.Label($"植物: {plantCount}");
            GUILayout.Label($"僵尸: {zombieCount}");
            GUILayout.Label($"抛射�? {projectileCount}");
            GUILayout.EndArea();
        }
    }
}
