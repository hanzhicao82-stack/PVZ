using UnityEngine;

namespace PVZ.DOTS.Tools
{
    /// <summary>
    /// GPU 动画压力测试工具 - 使用 DrawMeshInstanced
    /// 用于测试大量实例的渲染性能
    /// </summary>
    public class BakedAnimationStressTest : MonoBehaviour
    {
        [Header("烘焙数据")]
        [Tooltip("烘焙的动画数据")]
        public AnimationBakeData bakeData;

        [Header("渲染设置")]
        [Tooltip("使用的材质")]
        public Material material;

        [Header("生成设置")]
        [Tooltip("生成数量（DrawMeshInstanced 单批次最大 1023）")]
        [Range(1, 1023)]
        public int instanceCount = 100;

        [Tooltip("生成区域大小")]
        public Vector3 spawnAreaSize = new Vector3(50, 0, 50);

        [Tooltip("渲染层级")]
        public int layer = 0;
        
        [Tooltip("是否接收阴影")]
        public bool receiveShadows = true;
        
        [Tooltip("阴影投射模式")]
        public UnityEngine.Rendering.ShadowCastingMode castShadows = UnityEngine.Rendering.ShadowCastingMode.On;

        [Tooltip("是否随机播放时间偏移")]
        public bool randomTimeOffset = true;

        [Tooltip("是否随机旋转")]
        public bool randomRotation = true;

        [Tooltip("是否随机缩放")]
        public bool randomScale = false;

        [Tooltip("缩放范围")]
        public Vector2 scaleRange = new Vector2(0.8f, 1.2f);

        [Header("运行时信息")]
        [SerializeField]
        private int currentInstanceCount = 0;

        [SerializeField]
        private bool isPlaying = true;

        // 支持多个 Mesh
        private Material[] runtimeMaterials;
        private Mesh[] meshes;
        private Matrix4x4[] transformMatrices;
        private float[] timeOffsets;
        private float currentTime = 0f;

        private void Start()
        {
            Initialize();
        }

        private void OnValidate()
        {
            if (Application.isPlaying && currentInstanceCount != instanceCount)
            {
                Initialize();
            }
        }

        private void Initialize()
        {
            if (bakeData == null || material == null)
            {
                UnityEngine.Debug.LogError("[BakedAnimationStressTest] BakeData 或 Material 未设置！");
                return;
            }

            // 清理旧资源
            if (runtimeMaterials != null)
            {
                foreach (var mat in runtimeMaterials)
                {
                    if (mat != null) Destroy(mat);
                }
            }

            // 获取所有 Mesh 数据
            MeshBakeData[] meshDataList = null;
            if (bakeData.meshDataArray != null && bakeData.meshDataArray.Length > 0)
            {
                meshDataList = bakeData.meshDataArray;
            }
            else if (bakeData.bakedMesh != null)
            {
                // 向后兼容：单个 Mesh
                meshDataList = new MeshBakeData[] { 
                    new MeshBakeData {
                        bakedMesh = bakeData.bakedMesh,
                        positionMap = bakeData.positionMap,
                        normalMap = bakeData.normalMap,
                        vertexCount = bakeData.vertexCount,
                        mainTexture = null
                    }
                };
            }
            else
            {
                UnityEngine.Debug.LogError("[BakedAnimationStressTest] 没有可用的 Mesh！");
                return;
            }

            int meshCount = meshDataList.Length;
            meshes = new Mesh[meshCount];
            runtimeMaterials = new Material[meshCount];

            // 为每个 Mesh 创建材质
            for (int m = 0; m < meshCount; m++)
            {
                MeshBakeData meshData = meshDataList[m];
                meshes[m] = meshData.bakedMesh;

                // 创建运行时材质
                runtimeMaterials[m] = new Material(material);
                runtimeMaterials[m].enableInstancing = true;

                // 设置位置和法线贴图
                if (meshData.positionMap != null)
                {
                    meshData.positionMap.filterMode = FilterMode.Bilinear;
                    meshData.positionMap.wrapMode = TextureWrapMode.Clamp;
                    runtimeMaterials[m].SetTexture("_PositionMap", meshData.positionMap);
                }

                if (meshData.normalMap != null)
                {
                    meshData.normalMap.filterMode = FilterMode.Bilinear;
                    meshData.normalMap.wrapMode = TextureWrapMode.Clamp;
                    runtimeMaterials[m].SetTexture("_NormalMap", meshData.normalMap);
                }

                // 设置主贴图（重要！这是原始模型的贴图）
                if (meshData.mainTexture != null)
                {
                    runtimeMaterials[m].SetTexture("_MainTex", meshData.mainTexture);
                }

                // 设置动画参数（在材质上设置，所有实例共享）
                runtimeMaterials[m].SetFloat("_FrameRate", bakeData.frameRate);
                runtimeMaterials[m].SetInt("_TotalFrames", bakeData.totalFrames);
                runtimeMaterials[m].SetInt("_VertexCount", meshData.vertexCount);
            }

            // 生成变换矩阵
            transformMatrices = new Matrix4x4[instanceCount];
            timeOffsets = new float[instanceCount];

            for (int i = 0; i < instanceCount; i++)
            {
                // 随机位置
                Vector3 position = transform.position + new Vector3(
                    Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                    Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2),
                    Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
                );

                // 随机旋转
                Quaternion rotation = randomRotation 
                    ? Quaternion.Euler(0, Random.Range(0f, 360f), 0)
                    : Quaternion.identity;

                // 随机缩放
                Vector3 scale = randomScale
                    ? Vector3.one * Random.Range(scaleRange.x, scaleRange.y)
                    : Vector3.one;

                transformMatrices[i] = Matrix4x4.TRS(position, rotation, scale);

                // 随机时间偏移
                timeOffsets[i] = randomTimeOffset ? Random.Range(0f, bakeData.clipLength) : 0f;
            }

            currentInstanceCount = instanceCount;
            currentTime = 0f;
            isPlaying = true;

            UnityEngine.Debug.Log($"[BakedAnimationStressTest] 初始化 {instanceCount} 个实例 x {meshCount} 个 Mesh 用于 DrawMeshInstanced");
        }

        private void Update()
        {
            if (!isPlaying || runtimeMaterials == null || meshes == null || transformMatrices == null)
                return;

            currentTime += Time.deltaTime;

            // 循环动画
            if (currentTime >= bakeData.clipLength)
            {
                currentTime = currentTime % bakeData.clipLength;
            }

            // 渲染所有 Mesh（每个 Mesh 一次批量绘制）
            for (int m = 0; m < meshes.Length; m++)
            {
                // 设置动画时间（通过材质，所有实例共享同一时间）
                runtimeMaterials[m].SetFloat("_AnimationTime", currentTime);

                // 绘制所有实例（单次调用！）
                Graphics.DrawMeshInstanced(
                    meshes[m],
                    0,
                    runtimeMaterials[m],
                    transformMatrices,
                    instanceCount,
                    null,
                    castShadows,
                    receiveShadows,
                    layer
                );
            }
        }

        private void OnDestroy()
        {
            if (runtimeMaterials != null)
            {
                foreach (var mat in runtimeMaterials)
                {
                    if (mat != null) Destroy(mat);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 绘制生成区域
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, spawnAreaSize);
        }
    }
}
