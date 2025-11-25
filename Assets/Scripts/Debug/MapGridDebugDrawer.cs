using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Debug
{
    /// <summary>
    /// 地图网格可视化调试工具
    /// 使用Gizmos绘制关卡配置中的地图格子
    /// </summary>
    public class MapGridDebugDrawer : MonoBehaviour
    {
        [Header("网格可视化设置")]
        [Tooltip("是否启用网格绘制")]
        public bool enableGridDrawing = true;
        
        [Tooltip("网格线颜色")]
        public Color gridColor = new Color(0, 1, 0, 0.5f);
        
        [Tooltip("格子填充颜色")]
        public Color cellColor = new Color(0, 1, 0, 0.1f);
        
        [Tooltip("是否显示格子填充")]
        public bool showCellFill = true;
        
        [Tooltip("是否显示行列索引")]
        public bool showRowColumnIndex = true;
        
        [Tooltip("网格线宽度")]
        [Range(1f, 5f)]
        public float lineWidth = 2f;

        [Header("地图偏移")]
        [Tooltip("地图起始位置偏移")]
        public Vector3 mapOffset = new Vector3(-4.5f, 0, 0);

        private World gameWorld;
        private EntityManager entityManager;

        private void Start()
        {
            // 获取默认World
            gameWorld = World.DefaultGameObjectInjectionWorld;
            if (gameWorld != null)
            {
                entityManager = gameWorld.EntityManager;
            }
        }

        private void OnDrawGizmos()
        {
            if (!enableGridDrawing)
                return;

            // 尝试获取关卡配置
            if (gameWorld == null || !gameWorld.IsCreated)
            {
                gameWorld = World.DefaultGameObjectInjectionWorld;
                if (gameWorld == null) return;
                entityManager = gameWorld.EntityManager;
            }

            // 查找关卡配置实体
            var query = entityManager.CreateEntityQuery(typeof(LevelConfigComponent));
            if (query.IsEmptyIgnoreFilter)
            {
                query.Dispose();
                return;
            }

            var levelEntity = query.GetSingletonEntity();
            var levelConfig = entityManager.GetComponentData<LevelConfigComponent>(levelEntity);
            query.Dispose();

            DrawMapGrid(levelConfig);
        }

        private void DrawMapGrid(LevelConfigComponent config)
        {
            int rows = config.RowCount;
            int columns = config.ColumnCount;
            float cellSize = config.CellWidth;

            // 计算地图总尺寸
            float totalWidth = columns * cellSize;
            float totalHeight = rows * cellSize;

            // 绘制格子填充
            if (showCellFill)
            {
                Gizmos.color = cellColor;
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < columns; col++)
                    {
                        Vector3 cellCenter = mapOffset + new Vector3(
                            col * cellSize + cellSize * 0.5f,
                            0,
                            row * cellSize + cellSize * 0.5f
                        );
                        
                        Gizmos.DrawCube(cellCenter, new Vector3(cellSize * 0.95f, 0.1f, cellSize * 0.95f));
                    }
                }
            }

            // 绘制网格线
            Gizmos.color = gridColor;

            // 绘制水平线
            for (int row = 0; row <= rows; row++)
            {
                Vector3 start = mapOffset + new Vector3(0, 0, row * cellSize);
                Vector3 end = mapOffset + new Vector3(totalWidth, 0, row * cellSize);
                DrawThickLine(start, end, lineWidth);
            }

            // 绘制垂直线
            for (int col = 0; col <= columns; col++)
            {
                Vector3 start = mapOffset + new Vector3(col * cellSize, 0, 0);
                Vector3 end = mapOffset + new Vector3(col * cellSize, 0, totalHeight);
                DrawThickLine(start, end, lineWidth);
            }

            // 绘制行列索引标签
            if (showRowColumnIndex)
            {
                DrawGridLabels(rows, columns, cellSize);
            }

            // 绘制地图边界框
            DrawMapBorder(totalWidth, totalHeight);
        }

        private void DrawThickLine(Vector3 start, Vector3 end, float thickness)
        {
            // 使用多条平行线模拟粗线效果
            Gizmos.DrawLine(start, end);
            
            Vector3 offset = Vector3.up * 0.01f;
            Gizmos.DrawLine(start + offset, end + offset);
            Gizmos.DrawLine(start - offset, end - offset);
        }

        private void DrawGridLabels(int rows, int columns, float cellSize)
        {
            // 这部分需要使用Handles在Scene视图中绘制文字
            // Gizmos本身不支持文字绘制
#if UNITY_EDITOR
            UnityEditor.Handles.color = gridColor;
            
            // 绘制行号
            for (int row = 0; row < rows; row++)
            {
                Vector3 labelPos = mapOffset + new Vector3(-cellSize * 0.5f, 0.2f, row * cellSize + cellSize * 0.5f);
                UnityEditor.Handles.Label(labelPos, $"R{row}");
            }

            // 绘制列号
            for (int col = 0; col < columns; col++)
            {
                Vector3 labelPos = mapOffset + new Vector3(col * cellSize + cellSize * 0.5f, 0.2f, -cellSize * 0.5f);
                UnityEditor.Handles.Label(labelPos, $"C{col}");
            }
#endif
        }

        private void DrawMapBorder(float totalWidth, float totalHeight)
        {
            // 绘制地图四角的标记点
            Gizmos.color = Color.yellow;
            float markerSize = 0.3f;

            Vector3[] corners = new Vector3[]
            {
                mapOffset, // 左下
                mapOffset + new Vector3(totalWidth, 0, 0), // 右下
                mapOffset + new Vector3(totalWidth, 0, totalHeight), // 右上
                mapOffset + new Vector3(0, 0, totalHeight) // 左上
            };

            foreach (var corner in corners)
            {
                Gizmos.DrawSphere(corner, markerSize);
            }

            // 绘制中心点
            Vector3 center = mapOffset + new Vector3(totalWidth * 0.5f, 0, totalHeight * 0.5f);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(center, markerSize * 1.5f);
        }

        // 在运行时也能看到网格信息
        private void OnGUI()
        {
            if (!enableGridDrawing)
                return;

            if (gameWorld == null || !gameWorld.IsCreated)
                return;

            var query = entityManager.CreateEntityQuery(typeof(LevelConfigComponent));
            if (query.IsEmptyIgnoreFilter)
            {
                query.Dispose();
                return;
            }

            var levelEntity = query.GetSingletonEntity();
            var levelConfig = entityManager.GetComponentData<LevelConfigComponent>(levelEntity);
            query.Dispose();

            // 显示地图信息
            GUILayout.BeginArea(new Rect(10, 200, 300, 150));
            GUILayout.Box("地图网格信息");
            GUILayout.Label($"行数: {levelConfig.RowCount}");
            GUILayout.Label($"列数: {levelConfig.ColumnCount}");
            GUILayout.Label($"格子大小: {levelConfig.CellWidth}");
            GUILayout.Label($"地图尺寸: {levelConfig.ColumnCount * levelConfig.CellWidth} x {levelConfig.RowCount * levelConfig.CellWidth}");
            GUILayout.Label($"关卡类型: {levelConfig.Type}");
            GUILayout.Label($"难度: {levelConfig.Difficulty}");
            GUILayout.EndArea();
        }
    }
}
