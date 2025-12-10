using UnityEngine;

namespace PVZ.DOTS.Tools
{
    /// <summary>
    /// GPU 动画播放器 - 使用烘焙的贴图播放动画
    /// 使用 Graphics.DrawMesh API 直接绘制，不依赖 MeshRenderer
    /// </summary>
    public class BakedAnimationPlayer : MonoBehaviour
    {
        [Header("烘焙数据")]
        [Tooltip("烘焙的动画数据")]
        public AnimationBakeData bakeData;

        [Header("渲染设置")]
        [Tooltip("使用的材质")]
        public Material material;
        
        [Tooltip("渲染层级")]
        public int layer = 0;
        
        [Tooltip("是否接收阴影")]
        public bool receiveShadows = true;
        
        [Tooltip("阴影投射模式")]
        public UnityEngine.Rendering.ShadowCastingMode castShadows = UnityEngine.Rendering.ShadowCastingMode.On;

        [Header("播放设置")]
        [Tooltip("是否自动播放")]
        public bool autoPlay = true;

        [Tooltip("播放速度倍率")]
        [Range(0.1f, 5f)]
        public float playbackSpeed = 1f;

        [Tooltip("是否循环播放")]
        public bool loop = true;

        [Header("运行时信息")]
        [SerializeField]
        private float currentTime = 0f;

        [SerializeField]
        private int currentFrame = 0;

        [SerializeField]
        private bool isPlaying = false;

        private Material runtimeMaterial;
        private Mesh mesh;
        private MaterialPropertyBlock propertyBlock;

        private void Start()
        {
            if (bakeData == null)
            {
                UnityEngine.Debug.LogWarning($"[BakedAnimationPlayer] {gameObject.name}: BakeData 未设置！");
                return;
            }

            // 如果有多个 Mesh，为每个创建子对象
            if (bakeData.meshDataArray != null && bakeData.meshDataArray.Length > 1)
            {
                UnityEngine.Debug.Log($"[BakedAnimationPlayer] 检测到 {bakeData.meshDataArray.Length} 个 Mesh，为每个创建独立播放器");
                
                for (int i = 0; i < bakeData.meshDataArray.Length; i++)
                {
                    // 为每个 Mesh 创建单独的 BakeData
                    AnimationBakeData singleMeshData = ScriptableObject.CreateInstance<AnimationBakeData>();
                    singleMeshData.animationName = bakeData.animationName;
                    singleMeshData.frameRate = bakeData.frameRate;
                    singleMeshData.totalFrames = bakeData.totalFrames;
                    singleMeshData.clipLength = bakeData.clipLength;
                    singleMeshData.vertexCount = bakeData.meshDataArray[i].vertexCount;
                    singleMeshData.positionMap = bakeData.meshDataArray[i].positionMap;
                    singleMeshData.normalMap = bakeData.meshDataArray[i].normalMap;
                    singleMeshData.bakedMesh = bakeData.meshDataArray[i].bakedMesh;
                    singleMeshData.textureWidth = bakeData.meshDataArray[i].vertexCount;
                    singleMeshData.textureHeight = bakeData.totalFrames;
                    
                    // 创建子对象
                    GameObject childObj = new GameObject($"Mesh_{i}_{bakeData.meshDataArray[i].meshName}");
                    childObj.transform.SetParent(transform, false);
                    
                    // 为该 Mesh 创建材质实例
                    Material meshMaterial = new Material(material);
                    if (bakeData.meshDataArray[i].mainTexture != null)
                    {
                        meshMaterial.mainTexture = bakeData.meshDataArray[i].mainTexture;
                    }
                    
                    // 添加播放器组件
                    BakedAnimationPlayer childPlayer = childObj.AddComponent<BakedAnimationPlayer>();
                    childPlayer.bakeData = singleMeshData;
                    childPlayer.material = meshMaterial;
                    childPlayer.layer = layer;
                    childPlayer.receiveShadows = receiveShadows;
                    childPlayer.castShadows = castShadows;
                    childPlayer.autoPlay = autoPlay;
                    childPlayer.playbackSpeed = playbackSpeed;
                    childPlayer.loop = loop;
                }
                
                // 禁用父对象的渲染
                enabled = false;
                return;
            }
            
            // 单个 Mesh 的处理（兼容模式）
            if (bakeData.bakedMesh != null)
            {
                mesh = bakeData.bakedMesh;
            }
            else
            {
                UnityEngine.Debug.LogError($"[BakedAnimationPlayer] BakeData 中没有烘焙 Mesh！");
                return;
            }
            
            // 创建运行时材质实例
            if (material != null)
            {
                runtimeMaterial = new Material(material);
                propertyBlock = new MaterialPropertyBlock();
            }
            else
            {
                UnityEngine.Debug.LogError($"[BakedAnimationPlayer] 材质未设置！");
                return;
            }
            
            SetupMaterial();
            
            if (autoPlay)
            {
                Play();
            }
        }

        private void Update()
        {
            if (isPlaying && bakeData != null && runtimeMaterial != null)
            {
                currentTime += Time.deltaTime * playbackSpeed;

                // 循环或停止
                if (currentTime >= bakeData.clipLength)
                {
                    if (loop)
                    {
                        currentTime = currentTime % bakeData.clipLength;
                    }
                    else
                    {
                        currentTime = bakeData.clipLength;
                        isPlaying = false;
                    }
                }

                // 更新动画时间
                UpdateAnimation();
            }
            
            // 每帧绘制 Mesh
            if (mesh != null && runtimeMaterial != null)
            {
                propertyBlock.SetFloat("_AnimationTime", currentTime);
                
                Graphics.DrawMesh(
                    mesh,
                    transform.localToWorldMatrix,
                    runtimeMaterial,
                    layer,
                    null,
                    0,
                    propertyBlock,
                    castShadows,
                    receiveShadows
                );
            }
        }
        
        private void OnDestroy()
        {
            // 清理运行时材质
            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
            }
        }

        private void UpdateAnimation()
        {
            // 更新当前帧
            currentFrame = Mathf.FloorToInt(currentTime * bakeData.frameRate) % bakeData.totalFrames;
        }

        /// <summary>
        /// 设置材质参数
        /// </summary>
        private void SetupMaterial()
        {
            if (runtimeMaterial == null || bakeData == null)
                return;

            // 设置贴图
            if (bakeData.positionMap != null)
            {
                bakeData.positionMap.filterMode = FilterMode.Bilinear;
                bakeData.positionMap.wrapMode = TextureWrapMode.Clamp;
                runtimeMaterial.SetTexture("_PositionMap", bakeData.positionMap);
            }
            else
            {
                UnityEngine.Debug.LogError("[BakedAnimationPlayer] Position Map 为空！");
            }

            if (bakeData.normalMap != null)
            {
                bakeData.normalMap.filterMode = FilterMode.Bilinear;
                bakeData.normalMap.wrapMode = TextureWrapMode.Clamp;
                runtimeMaterial.SetTexture("_NormalMap", bakeData.normalMap);
            }
            else
            {
                UnityEngine.Debug.LogError($"[BakedAnimationPlayer] Normal Map 为空！");
            }

            // 设置动画参数
            runtimeMaterial.SetFloat("_FrameRate", bakeData.frameRate);
            runtimeMaterial.SetInt("_TotalFrames", bakeData.totalFrames);
            runtimeMaterial.SetInt("_VertexCount", bakeData.vertexCount);
        }

        /// <summary>
        /// 播放动画
        /// </summary>
        public void Play()
        {
            isPlaying = true;
        }

        /// <summary>
        /// 暂停动画
        /// </summary>
        public void Pause()
        {
            isPlaying = false;
        }

        /// <summary>
        /// 停止动画（重置到起始帧）
        /// </summary>
        public void Stop()
        {
            isPlaying = false;
            currentTime = 0f;
            currentFrame = 0;
            
            if (propertyBlock != null)
            {
                propertyBlock.SetFloat("_AnimationTime", 0f);
            }
        }

        /// <summary>
        /// 跳转到指定时间
        /// </summary>
        public void Seek(float time)
        {
            if (bakeData == null)
                return;

            currentTime = Mathf.Clamp(time, 0f, bakeData.clipLength);
        }

        /// <summary>
        /// 跳转到指定帧
        /// </summary>
        public void SeekToFrame(int frame)
        {
            if (bakeData == null)
                return;

            frame = Mathf.Clamp(frame, 0, bakeData.totalFrames - 1);
            float time = frame / (float)bakeData.frameRate;
            Seek(time);
        }

        private void OnValidate()
        {
            // 编辑器中实时预览
            if (Application.isPlaying && runtimeMaterial != null && bakeData != null)
            {
                SetupMaterial();
            }
        }
    }
}
