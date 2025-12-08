using Unity.Entities;
using Unity.Profiling;
using UnityEngine;
using PVZ.DOTS.Components;
using System.Text;

namespace PVZ.DOTS.Debug
{
    /// <summary>
    /// 性能诊断工具 - 显示详细的性能分析信息
    /// </summary>
    public class PerformanceDiagnostics : MonoBehaviour
    {
        [Header("显示设置")]
        public bool showDiagnostics = true;
        public KeyCode toggleKey = KeyCode.F1;
        
        private EntityManager _entityManager;
        private StringBuilder _sb = new StringBuilder();
        
        // FPS 计算
        private float _deltaTime = 0f;
        private float _fps = 0f;
        private int _frameCount = 0;
        private float _updateInterval = 0.5f;
        private float _timeSinceUpdate = 0f;
        
        // 性能计数器
        private ProfilerRecorder _mainThreadRecorder;
        private ProfilerRecorder _renderThreadRecorder;
        private ProfilerRecorder _gcRecorder;

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                _entityManager = world.EntityManager;
            }
            
            // 初始化性能计数器
            _mainThreadRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
            _renderThreadRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Render Thread", 15);
            _gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 15);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                showDiagnostics = !showDiagnostics;
            }

            if (!showDiagnostics)
                return;

            // 计算 FPS
            _deltaTime += Time.unscaledDeltaTime;
            _timeSinceUpdate += Time.unscaledDeltaTime;
            _frameCount++;

            if (_timeSinceUpdate >= _updateInterval)
            {
                _fps = _frameCount / _timeSinceUpdate;
                _frameCount = 0;
                _timeSinceUpdate = 0f;
            }
        }

        private void OnGUI()
        {
            if (!showDiagnostics || _entityManager == null)
                return;

            // 设置 GUI 样式
            int padding = 10;
            int width = 400;
            int startY = 10;
            
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.alignment = TextAnchor.UpperLeft;
            boxStyle.fontSize = 12;
            
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 11;
            labelStyle.normal.textColor = Color.white;

            _sb.Clear();
            
            // === FPS 和性能 ===
            _sb.AppendLine("=== 性能诊断 (按 F1 切换) ===");
            _sb.AppendLine();
            
            Color fpsColor = _fps >= 30 ? Color.green : (_fps >= 20 ? Color.yellow : Color.red);
            _sb.AppendLine($"<color=#{ColorUtility.ToHtmlStringRGB(fpsColor)}>FPS: {_fps:F1}</color>");
            _sb.AppendLine($"帧时间: {(_deltaTime / Mathf.Max(_frameCount, 1)) * 1000f:F2} ms");
            _sb.AppendLine();
            
            // === 实体统计 ===
            _sb.AppendLine("=== 实体统计 ===");
            var plantCount = GetEntityCount<PlantComponent>();
            var zombieCount = GetEntityCount<ZombieComponent>();
            var projectileCount = GetEntityCount<ProjectileComponent>();
            var healthBarCount = GetEntityCount<HealthBarComponent>();
            
            _sb.AppendLine($"植物: {plantCount}");
            _sb.AppendLine($"僵尸: {zombieCount}");
            _sb.AppendLine($"子弹: {projectileCount}");
            _sb.AppendLine($"血条: {healthBarCount}");
            _sb.AppendLine($"<b>总实体: {plantCount + zombieCount + projectileCount}</b>");
            _sb.AppendLine();
            
            // === 系统性能 ===
            _sb.AppendLine("=== 系统性能 ===");
            
            // 主线程时间
            if (_mainThreadRecorder.Valid)
            {
                double mainThreadMs = GetRecorderAverage(_mainThreadRecorder) * 1e-6; // 纳秒转毫秒
                _sb.AppendLine($"主线程: {mainThreadMs:F2} ms");
            }
            
            // 渲染线程时间
            if (_renderThreadRecorder.Valid)
            {
                double renderThreadMs = GetRecorderAverage(_renderThreadRecorder) * 1e-6;
                _sb.AppendLine($"渲染线程: {renderThreadMs:F2} ms");
            }
            
            // GC 分配
            if (_gcRecorder.Valid)
            {
                double gcAllocKB = GetRecorderAverage(_gcRecorder) / 1024.0;
                _sb.AppendLine($"GC 分配: {gcAllocKB:F2} KB/帧");
            }
            
            _sb.AppendLine();
            
            // === 内存使用 ===
            _sb.AppendLine("=== 内存使用 ===");
            long totalMemory = System.GC.GetTotalMemory(false) / (1024 * 1024);
            _sb.AppendLine($"托管内存: {totalMemory} MB");
            _sb.AppendLine($"Unity 总内存: {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024)} MB");
            _sb.AppendLine();
            
            // === 优化建议 ===
            _sb.AppendLine("=== 优化建议 ===");
            
            if (_fps < 20)
            {
                _sb.AppendLine("<color=red>• FPS 过低！检查以下项：</color>");
                if (projectileCount > 1000)
                    _sb.AppendLine("  - 子弹过多 (>1000)，考虑禁用子弹视图");
                if (healthBarCount > 500)
                    _sb.AppendLine("  - 血条过多 (>500)");
                if (plantCount + zombieCount > 2000)
                    _sb.AppendLine("  - 战斗单位过多 (>2000)");
            }
            else if (_fps < 30)
            {
                _sb.AppendLine("<color=yellow>• FPS 偏低，可优化</color>");
            }
            else
            {
                _sb.AppendLine("<color=green>• 性能良好！</color>");
            }

            // 绘制
            GUILayout.BeginArea(new Rect(padding, startY, width, Screen.height - startY - padding));
            GUILayout.Box(_sb.ToString(), boxStyle, GUILayout.Width(width - padding * 2));
            GUILayout.EndArea();
        }

        private int GetEntityCount<T>() where T : unmanaged, IComponentData
        {
            var query = _entityManager.CreateEntityQuery(typeof(T));
            int count = query.CalculateEntityCount();
            query.Dispose();
            return count;
        }

        private double GetRecorderAverage(ProfilerRecorder recorder)
        {
            if (!recorder.Valid || recorder.Count == 0)
                return 0;

            double sum = 0;
            int count = Mathf.Min(recorder.Count, recorder.Capacity);
            
            unsafe
            {
                var samples = stackalloc ProfilerRecorderSample[count];
                recorder.CopyTo(samples, count);
                
                for (int i = 0; i < count; i++)
                {
                    sum += samples[i].Value;
                }
            }
            
            return sum / count;
        }

        private void OnDestroy()
        {
            _mainThreadRecorder.Dispose();
            _renderThreadRecorder.Dispose();
            _gcRecorder.Dispose();
        }
    }
}
