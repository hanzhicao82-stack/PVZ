using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using PVZ.DOTS.Components;
using System; // 为可空值类型支持

namespace PVZ.DOTS.Debug
{
    /// <summary>
    /// 游戏测试设置 - 快速生成测试场景
    /// </summary>
    public class GameTestSetup : MonoBehaviour
    {
        [Header("测试场景配置")]
        public bool autoSetupOnStart = true;

        [Header("植物测试")]
        public bool spawnTestPlants = true;
        public int testPlantsCount = 3;

        [Header("僵尸测试")]
        public bool spawnTestZombies = true;
        public int testZombiesCount = 2;

        [Header("网格配置")]
        public Vector3 gridOrigin = new Vector3(-6f, 0f, -4f);
        public float gridCellWidth = 1.5f;
        public float gridCellHeight = 2f;

        private EntityManager _entityManager;

        void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (autoSetupOnStart)
            {
                SetupTestScene();
            }
        }

        [ContextMenu("设置测试场景")]
        public void SetupTestScene()
        {
            if (World.DefaultGameObjectInjectionWorld == null || _entityManager == null)
            {
                UnityEngine.Debug.LogError("EntityManager 未初始化！");
                return;
            }

            ClearAllEntities();

            if (spawnTestPlants)
            {
                SpawnTestPlants();
            }

            if (spawnTestZombies)
            {
                SpawnTestZombies();
            }

            UnityEngine.Debug.Log("测试场景设置完成！");
        }

        [ContextMenu("清除所有实体")]
        public void ClearAllEntities()
        {
            if (World.DefaultGameObjectInjectionWorld == null || _entityManager == null)
                return;

            // 清除植物
            var plantQuery = _entityManager.CreateEntityQuery(typeof(PlantComponent));
            _entityManager.DestroyEntity(plantQuery);

            // 清除僵尸
            var zombieQuery = _entityManager.CreateEntityQuery(typeof(ZombieComponent));
            _entityManager.DestroyEntity(zombieQuery);

            // 清除子弹
            var projectileQuery = _entityManager.CreateEntityQuery(typeof(ProjectileComponent));
            _entityManager.DestroyEntity(projectileQuery);

            UnityEngine.Debug.Log("所有实体已清除！");
        }

        [ContextMenu("生成测试植物")]
        public void SpawnTestPlants()
        {
            for (int i = 0; i < testPlantsCount; i++)
            {
                int row = i % 5;
                int column = 2 + i;

                Vector3 position = GridToWorldPosition(row, column);
                SpawnPlant(PlantType.Peashooter, row, column, position);
            }

            UnityEngine.Debug.Log($"已生成 {testPlantsCount} 个测试植物！");
        }

        [ContextMenu("生成测试僵尸")]
        public void SpawnTestZombies()
        {
            for (int i = 0; i < testZombiesCount; i++)
            {
                int row = i % 5;
                int column = 8;

                Vector3 position = GridToWorldPosition(row, column);
                SpawnZombie(ZombieType.Normal, row, position);
            }

            UnityEngine.Debug.Log($"已生成 {testZombiesCount} 个测试僵尸！");
        }

        PlantConfigElement? GetPlantConfig(PlantType type)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return null;
            var em = world.EntityManager;
            var query = em.CreateEntityQuery(typeof(PlantConfigElement));
            if (query.CalculateEntityCount() == 0) return null;
            Entity configEntity = query.GetSingletonEntity();
            var buffer = em.GetBuffer<PlantConfigElement>(configEntity);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Type == type) return buffer[i];
            }
            return null;
        }

        ZombieConfigElement? GetZombieConfig(ZombieType type)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return null;
            var em = world.EntityManager;
            var query = em.CreateEntityQuery(typeof(ZombieConfigElement));
            if (query.CalculateEntityCount() == 0) return null;
            Entity configEntity = query.GetSingletonEntity();
            var buffer = em.GetBuffer<ZombieConfigElement>(configEntity);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Type == type) return buffer[i];
            }
            return null;
        }

        public void SpawnPlant(PlantType type, int row, int column, Vector3 position)
        {
            Entity entity = _entityManager.CreateEntity();

            var cfg = GetPlantConfig(type);
            float attackDamage = cfg?.AttackDamage ?? 20f;
            float attackInterval = cfg?.AttackInterval ?? 1.5f;
            float attackRange = cfg?.AttackRange ?? 10f;
            int sunCost = cfg?.SunCost ?? 100;
            float health = cfg?.Health ?? 100f;
            float sunProdInterval = cfg?.SunProductionInterval ?? 0f;
            int sunProdAmount = cfg?.SunProductionAmount ?? 0;

            _entityManager.AddComponentData(entity, new PlantComponent
            {
                Type = type,
                AttackDamage = attackDamage,
                AttackInterval = attackInterval,
                LastAttackTime = 0f,
                AttackRange = attackRange,
                SunCost = sunCost
            });

            _entityManager.AddComponentData(entity, new HealthComponent
            {
                CurrentHealth = health,
                MaxHealth = health,
                IsDead = false
            });

            if (sunProdInterval > 0f && sunProdAmount > 0)
            {
                _entityManager.AddComponentData(entity, new SunProducerComponent
                {
                    ProductionInterval = sunProdInterval,
                    LastProductionTime = 0f,
                    SunAmount = sunProdAmount
                });
            }

            _entityManager.AddComponentData(entity, new GridPositionComponent
            {
                Row = row,
                Column = column,
                WorldPosition = position
            });

            _entityManager.AddComponentData(entity, LocalTransform.FromPosition(position));
        }

        public void SpawnZombie(ZombieType type, int lane, Vector3 position)
        {
            Entity entity = _entityManager.CreateEntity();
            var cfg = GetZombieConfig(type);
            float moveSpeed = cfg?.MovementSpeed ?? 1f;
            float attackDamage = cfg?.AttackDamage ?? 10f;
            float attackInterval = cfg?.AttackInterval ?? 1f;
            float health = cfg?.Health ?? 100f;

            _entityManager.AddComponentData(entity, new ZombieComponent
            {
                Type = type,
                MovementSpeed = moveSpeed,
                AttackDamage = attackDamage,
                AttackInterval = attackInterval,
                LastAttackTime = 0f,
                Lane = lane
            });

            _entityManager.AddComponentData(entity, new HealthComponent
            {
                CurrentHealth = health,
                MaxHealth = health,
                IsDead = false
            });

            _entityManager.AddComponentData(entity, new GridPositionComponent
            {
                Row = lane,
                Column = 9,
                WorldPosition = position
            });

            _entityManager.AddComponentData(entity, LocalTransform.FromPosition(position));
        }

        Vector3 GridToWorldPosition(int row, int column)
        {
            return gridOrigin + new Vector3(
                column * gridCellWidth + gridCellWidth / 2,
                0,
                row * gridCellHeight + gridCellHeight / 2
            );
        }

        void Update()
        {
            // 快捷键
            if (Input.GetKeyDown(KeyCode.F1))
            {
                SetupTestScene();
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                ClearAllEntities();
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                SpawnTestPlants();
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                SpawnTestZombies();
            }
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 310, 10, 300, 200));
            GUILayout.BeginVertical("box");

            GUILayout.Label("=== 测试控制 ===", new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold });
            GUILayout.Space(5);

            if (GUILayout.Button("F1 - 设置测试场景"))
            {
                SetupTestScene();
            }

            if (GUILayout.Button("F2 - 清除所有实体"))
            {
                ClearAllEntities();
            }

            if (GUILayout.Button("F3 - 生成测试植物"))
            {
                SpawnTestPlants();
            }

            if (GUILayout.Button("F4 - 生成测试僵尸"))
            {
                SpawnTestZombies();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
