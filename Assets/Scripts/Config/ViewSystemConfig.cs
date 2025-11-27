using UnityEngine;

namespace PVZ.DOTS.Config
{
    /// <summary>
    /// 视图系统配置 - 控制使用哪种渲染系统
    /// </summary>
    [CreateAssetMenu(fileName = "ViewSystemConfig", menuName = "PVZ/View System Config")]
    public class ViewSystemConfig : ScriptableObject
    {
        private static ViewSystemConfig _instance;

        public static ViewSystemConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ViewSystemConfig>("ViewSystemConfig");
                    if (_instance == null)
                    {
                        UnityEngine.Debug.LogWarning("ViewSystemConfig not found in Resources. Using default settings.");
                        _instance = CreateInstance<ViewSystemConfig>();
                    }
                }
                return _instance;
            }
        }

        [Header("渲染系统选择")]
        [Tooltip("启用 Spine 渲染系统")]
        public bool enableSpineSystem = true;

        [Tooltip("启用 MeshRenderer 渲染系统")]
        public bool enableMeshRendererSystem = true;

        [Header("性能设置")]
        [Tooltip("同时更新的最大实体数（0=无限制）")]
        public int maxEntitiesPerFrame = 0;

        [Tooltip("启用视图更新缓存")]
        public bool enableViewUpdateCache = true;
    }
}
