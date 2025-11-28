using System;
using UnityEngine;
using PVZ.DOTS.Components;

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

        [Header("Spine 资源映射")]
        [Tooltip("Spine 植物预制体映射列表（Resources 相对路径）")]
        public SpinePlantPrefabEntry[] spinePlantPrefabs = Array.Empty<SpinePlantPrefabEntry>();

        [Tooltip("Spine 僵尸预制体映射列表（Resources 相对路径）")]
        public SpineZombiePrefabEntry[] spineZombiePrefabs = Array.Empty<SpineZombiePrefabEntry>();

        /// <summary>
        /// 获取指定植物类型的 Spine 预制体路径
        /// </summary>
        public string GetSpinePlantPrefabPath(PlantType plantType)
        {
            if (spinePlantPrefabs == null)
            {
                return string.Empty;
            }

            foreach (var entry in spinePlantPrefabs)
            {
                if (entry != null && entry.plantType == plantType && !string.IsNullOrEmpty(entry.prefabPath))
                {
                    return entry.prefabPath;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取指定僵尸类型的 Spine 预制体路径
        /// </summary>
        public string GetSpineZombiePrefabPath(ZombieType zombieType)
        {
            if (spineZombiePrefabs == null)
            {
                return string.Empty;
            }

            foreach (var entry in spineZombiePrefabs)
            {
                if (entry != null && entry.zombieType == zombieType && !string.IsNullOrEmpty(entry.prefabPath))
                {
                    return entry.prefabPath;
                }
            }

            return string.Empty;
        }
    }

    [System.Serializable]
    public class SpinePlantPrefabEntry
    {
        [Tooltip("植物类型")] public PlantType plantType;
        [Tooltip("Spine 预制体 Resources 路径")] public string prefabPath;
    }

    [System.Serializable]
    public class SpineZombiePrefabEntry
    {
        [Tooltip("僵尸类型")] public ZombieType zombieType;
        [Tooltip("Spine 预制体 Resources 路径")] public string prefabPath;
    }
}
