using System;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 渲染器配置基�?
    /// </summary>
    [Serializable]
    public class RenderConfig
    {
        public bool enabled = true;
        public int priority = 0;
    }

    /// <summary>
    /// Spine 渲染器配�?
    /// </summary>
    [Serializable]
    public class SpineRenderConfig : RenderConfig
    {
        [Header("LOD 设置")]
        [Tooltip("是否启用 LOD（距离分级优化）")]
        public bool lodEnabled = true;

        [Tooltip("近距离阈值（每帧更新）")]
        public float lodNearDistance = 15f;

        [Tooltip("远距离阈值（降低更新频率）")]
        public float lodFarDistance = 30f;

        [Header("剔除设置")]
        [Tooltip("是否启用视锥剔除")]
        public bool cullingEnabled = true;

        [Tooltip("剔除边界扩展（屏幕外多远开始剔除）")]
        [Range(0f, 0.5f)]
        public float cullingMargin = 0.1f;

        [Header("更新频率")]
        [Tooltip("基础更新频率�?=每帧�?=隔帧更新")]
        [Range(1, 10)]
        public int baseUpdateFrequency = 1;

        [Tooltip("是否启用帧跳过优化")]
        public bool frameSkipEnabled = true;

        [Header("动画设置")]
        [Tooltip("是否启用动画状态缓存")]
        public bool animationCacheEnabled = true;

        [Tooltip("颜色更新是否只在近距离")]
        public bool colorUpdateNearOnly = true;

        /// <summary>
        /// 创建默认配置
        /// </summary>
        public static SpineRenderConfig Default()
        {
            return new SpineRenderConfig
            {
                enabled = true,
                lodEnabled = true,
                lodNearDistance = 15f,
                lodFarDistance = 30f,
                cullingEnabled = true,
                cullingMargin = 0.1f,
                baseUpdateFrequency = 1,
                frameSkipEnabled = true,
                animationCacheEnabled = true,
                colorUpdateNearOnly = true
            };
        }

        /// <summary>
        /// 创建高性能配置（移动端�?
        /// </summary>
        public static SpineRenderConfig HighPerformance()
        {
            return new SpineRenderConfig
            {
                enabled = true,
                lodEnabled = true,
                lodNearDistance = 10f,
                lodFarDistance = 20f,
                cullingEnabled = true,
                cullingMargin = 0.2f,
                baseUpdateFrequency = 2,
                frameSkipEnabled = true,
                animationCacheEnabled = true,
                colorUpdateNearOnly = true
            };
        }

        /// <summary>
        /// 创建高质量配置（PC�?
        /// </summary>
        public static SpineRenderConfig HighQuality()
        {
            return new SpineRenderConfig
            {
                enabled = true,
                lodEnabled = false,
                lodNearDistance = 15f,
                lodFarDistance = 30f,
                cullingEnabled = true,
                cullingMargin = 0.05f,
                baseUpdateFrequency = 1,
                frameSkipEnabled = false,
                animationCacheEnabled = true,
                colorUpdateNearOnly = false
            };
        }
    }

    /// <summary>
    /// Mesh 渲染器配�?
    /// </summary>
    [Serializable]
    public class MeshRenderConfig : RenderConfig
    {
        [Header("批处理")]
        [Tooltip("是否启用动态批处理")]
        public bool dynamicBatchingEnabled = true;

        [Tooltip("是否启用静态批处理")]
        public bool staticBatchingEnabled = true;

        [Header("阴影")]
        [Tooltip("阴影投射模式：Off, On, TwoSided, ShadowsOnly")]
        public string shadowCastingMode = "On";

        [Tooltip("是否接收阴影")]
        public bool receiveShadows = true;

        [Header("LOD")]
        [Tooltip("LOD 偏移（控制 LOD 切换距离）")]
        [Range(0.1f, 10f)]
        public float lodBias = 1.0f;

        [Tooltip("最�?LOD 等级")]
        [Range(0, 7)]
        public int maxLODLevel = 3;

        [Header("性能")]
        [Tooltip("是否启用视锥剔除")]
        public bool frustumCullingEnabled = true;

        [Tooltip("是否启用遮挡剔除")]
        public bool occlusionCullingEnabled = false;

        /// <summary>
        /// 创建默认配置
        /// </summary>
        public static MeshRenderConfig Default()
        {
            return new MeshRenderConfig
            {
                enabled = true,
                dynamicBatchingEnabled = true,
                staticBatchingEnabled = true,
                shadowCastingMode = "On",
                receiveShadows = true,
                lodBias = 1.0f,
                maxLODLevel = 3,
                frustumCullingEnabled = true,
                occlusionCullingEnabled = false
            };
        }

        /// <summary>
        /// 创建高性能配置
        /// </summary>
        public static MeshRenderConfig HighPerformance()
        {
            return new MeshRenderConfig
            {
                enabled = true,
                dynamicBatchingEnabled = true,
                staticBatchingEnabled = true,
                shadowCastingMode = "Off",
                receiveShadows = false,
                lodBias = 2.0f,
                maxLODLevel = 2,
                frustumCullingEnabled = true,
                occlusionCullingEnabled = true
            };
        }
    }

    /// <summary>
    /// Sprite 渲染器配�?
    /// </summary>
    [Serializable]
    public class SpriteRenderConfig : RenderConfig
    {
        [Header("批处理")]
        [Tooltip("是否启用精灵批处理")]
        public bool spriteBatchingEnabled = true;

        [Header("排序")]
        [Tooltip("排序层级偏移")]
        public int sortingLayerOffset = 0;

        [Tooltip("排序顺序偏移")]
        public int orderInLayerOffset = 0;

        [Header("剔除")]
        [Tooltip("是否启用视锥剔除")]
        public bool cullingEnabled = true;

        [Tooltip("剔除边界扩展")]
        [Range(0f, 2f)]
        public float cullingMargin = 0.5f;

        /// <summary>
        /// 创建默认配置
        /// </summary>
        public static SpriteRenderConfig Default()
        {
            return new SpriteRenderConfig
            {
                enabled = true,
                spriteBatchingEnabled = true,
                sortingLayerOffset = 0,
                orderInLayerOffset = 0,
                cullingEnabled = true,
                cullingMargin = 0.5f
            };
        }
    }
}
