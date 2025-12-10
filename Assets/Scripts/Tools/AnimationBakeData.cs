using UnityEngine;

namespace PVZ.DOTS.Tools
{
    /// <summary>
    /// 单个Mesh的动画烘焙数据
    /// </summary>
    [System.Serializable]
    public class MeshBakeData
    {
        public string meshName;
        public int vertexCount;
        public Texture2D positionMap;
        public Texture2D normalMap;
        public Mesh bakedMesh;
        public Texture2D mainTexture; // 原始材质的主贴图
    }

    /// <summary>
    /// 动画烘焙数据配置
    /// </summary>
    public class AnimationBakeData : ScriptableObject
    {
        public string animationName;
        public int frameRate;
        public int totalFrames;
        public float clipLength;
        public int textureWidth;
        public int textureHeight;

        [Header("多Mesh支持")]
        [Tooltip("所有Mesh的烘焙数据")]
        public MeshBakeData[] meshDataArray;
        
        [Header("资源引用（向后兼容）")]
        [Tooltip("第一个Mesh的数据，用于向后兼容")]
        public int vertexCount;
        public Texture2D positionMap;
        public Texture2D normalMap;
        public Mesh bakedMesh;
    }
}
