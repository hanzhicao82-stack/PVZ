using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Debug
{
    /// <summary>
    /// 游戏调试绘制器 - 使用 Gizmos 可视化游戏对象
    /// </summary>
    public class GameDebugDrawer : MonoBehaviour
    {
        [Header("绘制开关")]
        public bool drawPlants = true;
        public bool drawZombies = true;
        public bool drawProjectiles = true;
        public bool drawHealthBars = true;
        public bool drawAttackRanges = false;

        [Header("颜色配置")]
        public Color plantColor = Color.green;
        public Color zombieColor = Color.red;
        public Color projectileColor = Color.yellow;
        public Color healthBarBackground = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        public Color healthBarForeground = Color.green;

        private EntityManager _entityManager;

        void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

            if (World.DefaultGameObjectInjectionWorld == null || _entityManager == null)
                return;

            // 绘制植物
            if (drawPlants)
            {
                DrawPlants();
            }

            // 绘制僵尸
            if (drawZombies)
            {
                DrawZombies();
            }

            // 绘制子弹
            if (drawProjectiles)
            {
                DrawProjectiles();
            }
        }

        void DrawPlants()
        {
            if (_entityManager == null)
                return;

            var query = _entityManager.CreateEntityQuery(
                typeof(PlantComponent),
                typeof(LocalTransform)
            );

            if (query.IsEmptyIgnoreFilter)
            {
                query.Dispose();
                return;
            }

            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            
            foreach (var entity in entities)
            {
                var transform = _entityManager.GetComponentData<LocalTransform>(entity);
                var plant = _entityManager.GetComponentData<PlantComponent>(entity);

                // 根据植物类型设置颜色
                switch (plant.Type)
                {
                    case PlantType.Peashooter:
                        Gizmos.color = Color.green;
                        break;
                    case PlantType.Sunflower:
                        Gizmos.color = Color.yellow;
                        break;
                    case PlantType.WallNut:
                        Gizmos.color = new Color(0.6f, 0.4f, 0.2f);
                        break;
                    case PlantType.SnowPea:
                        Gizmos.color = Color.cyan;
                        break;
                    case PlantType.Repeater:
                        Gizmos.color = new Color(0f, 0.8f, 0f);
                        break;
                    default:
                        Gizmos.color = plantColor;
                        break;
                }

                // 绘制植物身体
                Vector3 position = transform.Position;
                Gizmos.DrawWireSphere(position + new Vector3(0, 0.5f, 0), 0.4f);
                Gizmos.DrawSphere(position + new Vector3(0, 0.5f, 0), 0.3f);

                // 绘制攻击范围
                if (drawAttackRanges && plant.AttackRange > 0)
                {
                    Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.2f);
                    Gizmos.DrawWireSphere(position + new Vector3(0, 0.5f, 0), plant.AttackRange);
                }

                // 绘制生命值
                if (drawHealthBars && _entityManager.HasComponent<HealthComponent>(entity))
                {
                    var health = _entityManager.GetComponentData<HealthComponent>(entity);
                    DrawHealthBar(position + new Vector3(0, 1.2f, 0), health.CurrentHealth, health.MaxHealth);
                }
            }

            entities.Dispose();
            query.Dispose();
        }

        void DrawZombies()
        {
            if (_entityManager == null)
                return;

            var query = _entityManager.CreateEntityQuery(
                typeof(ZombieComponent),
                typeof(LocalTransform)
            );

            if (query.IsEmptyIgnoreFilter)
            {
                query.Dispose();
                return;
            }

            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            
            foreach (var entity in entities)
            {
                var transform = _entityManager.GetComponentData<LocalTransform>(entity);
                var zombie = _entityManager.GetComponentData<ZombieComponent>(entity);

                // 根据僵尸类型设置颜色
                switch (zombie.Type)
                {
                    case ZombieType.Normal:
                        Gizmos.color = Color.red;
                        break;
                    case ZombieType.ConeHead:
                        Gizmos.color = new Color(1f, 0.5f, 0f);
                        break;
                    case ZombieType.BucketHead:
                        Gizmos.color = new Color(0.5f, 0.5f, 0.5f);
                        break;
                    case ZombieType.Flag:
                        Gizmos.color = new Color(1f, 0f, 1f);
                        break;
                    default:
                        Gizmos.color = zombieColor;
                        break;
                }

                // 绘制僵尸身体
                Vector3 position = transform.Position;
                Gizmos.DrawWireCube(position + new Vector3(0, 0.6f, 0), new Vector3(0.5f, 1.2f, 0.3f));
                Gizmos.DrawCube(position + new Vector3(0, 0.6f, 0), new Vector3(0.4f, 1f, 0.25f));

                // 绘制移动方向
                Gizmos.color = Color.white;
                Vector3 directionEnd = position + new Vector3(-0.5f, 0.5f, 0);
                Gizmos.DrawLine(position + new Vector3(0, 0.5f, 0), directionEnd);

                // 绘制生命值
                if (drawHealthBars && _entityManager.HasComponent<HealthComponent>(entity))
                {
                    var health = _entityManager.GetComponentData<HealthComponent>(entity);
                    DrawHealthBar(position + new Vector3(0, 1.5f, 0), health.CurrentHealth, health.MaxHealth);
                }
            }

            entities.Dispose();
            query.Dispose();
        }

        void DrawProjectiles()
        {
            if (_entityManager == null)
                return;

            var query = _entityManager.CreateEntityQuery(
                typeof(ProjectileComponent),
                typeof(LocalTransform)
            );

            if (query.IsEmptyIgnoreFilter)
            {
                query.Dispose();
                return;
            }

            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            
            foreach (var entity in entities)
            {
                var transform = _entityManager.GetComponentData<LocalTransform>(entity);
                var projectile = _entityManager.GetComponentData<ProjectileComponent>(entity);

                // 根据子弹类型设置颜色
                switch (projectile.Type)
                {
                    case ProjectileType.Pea:
                        Gizmos.color = Color.green;
                        break;
                    case ProjectileType.FrozenPea:
                        Gizmos.color = Color.cyan;
                        break;
                    case ProjectileType.Melon:
                        Gizmos.color = Color.red;
                        break;
                    default:
                        Gizmos.color = projectileColor;
                        break;
                }

                // 绘制子弹
                Vector3 position = transform.Position;
                Gizmos.DrawSphere(position, 0.15f);

                // 绘制速度方向
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.5f);
                Vector3 directionEnd = position + new Vector3(projectile.Direction.x, projectile.Direction.y, projectile.Direction.z) * 0.5f;
                Gizmos.DrawLine(position, directionEnd);
            }

            entities.Dispose();
            query.Dispose();
        }

        void DrawHealthBar(Vector3 position, float currentHealth, float maxHealth)
        {
            if (maxHealth <= 0) return;

            float healthPercent = Mathf.Clamp01(currentHealth / maxHealth);
            float barWidth = 0.6f;
            float barHeight = 0.1f;

            // 背景条
            Gizmos.color = healthBarBackground;
            DrawBar(position, barWidth, barHeight);

            // 生命值条
            if (healthPercent > 0.6f)
                Gizmos.color = Color.green;
            else if (healthPercent > 0.3f)
                Gizmos.color = Color.yellow;
            else
                Gizmos.color = Color.red;

            DrawBar(position, barWidth * healthPercent, barHeight);
        }

        void DrawBar(Vector3 position, float width, float height)
        {
            Vector3 leftBottom = position + new Vector3(-width / 2, 0, 0);
            Vector3 rightBottom = position + new Vector3(width / 2, 0, 0);
            Vector3 leftTop = position + new Vector3(-width / 2, height, 0);
            Vector3 rightTop = position + new Vector3(width / 2, height, 0);

            // 绘制四条边
            Gizmos.DrawLine(leftBottom, rightBottom);
            Gizmos.DrawLine(rightBottom, rightTop);
            Gizmos.DrawLine(rightTop, leftTop);
            Gizmos.DrawLine(leftTop, leftBottom);

            // 填充（使用多条线模拟）
            for (float i = 0; i <= height; i += 0.02f)
            {
                Vector3 start = leftBottom + new Vector3(0, i, 0);
                Vector3 end = rightBottom + new Vector3(0, i, 0);
                Gizmos.DrawLine(start, end);
            }
        }

        // 在 Scene 视图中显示调试信息
        void OnGUI()
        {
            if (!Application.isPlaying || World.DefaultGameObjectInjectionWorld == null || _entityManager == null)
                return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.BeginVertical("box");

            GUILayout.Label("=== 游戏调试信息 ===", new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold });
            GUILayout.Space(10);

            // 统计实体数量
            var plantQuery = _entityManager.CreateEntityQuery(typeof(PlantComponent));
            var zombieQuery = _entityManager.CreateEntityQuery(typeof(ZombieComponent));
            var projectileQuery = _entityManager.CreateEntityQuery(typeof(ProjectileComponent));

            GUILayout.Label($"植物数量: {plantQuery.CalculateEntityCount()}");
            GUILayout.Label($"僵尸数量: {zombieQuery.CalculateEntityCount()}");
            GUILayout.Label($"子弹数量: {projectileQuery.CalculateEntityCount()}");

            plantQuery.Dispose();
            zombieQuery.Dispose();
            projectileQuery.Dispose();

            GUILayout.Space(10);
            GUILayout.Label("--- 绘制选项 ---", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            drawPlants = GUILayout.Toggle(drawPlants, "显示植物");
            drawZombies = GUILayout.Toggle(drawZombies, "显示僵尸");
            drawProjectiles = GUILayout.Toggle(drawProjectiles, "显示子弹");
            drawHealthBars = GUILayout.Toggle(drawHealthBars, "显示生命值");
            drawAttackRanges = GUILayout.Toggle(drawAttackRanges, "显示攻击范围");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
