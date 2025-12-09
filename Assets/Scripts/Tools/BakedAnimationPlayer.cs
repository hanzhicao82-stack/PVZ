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
            if (bakeData != null)
            {
                // 获取烘焙的 Mesh
                mesh = bakeData.bakedMesh;
                
                if (mesh == null)
                {
                    UnityEngine.Debug.LogError($"[BakedAnimationPlayer] BakeData 中没有烘焙 Mesh！");
                    return;
                }
                
                // 创建运行时材质实例
                if (material != null)
                {
                    runtimeMaterial = new Material(material);
                }
                else
                {
                    UnityEngine.Debug.LogError($"[BakedAnimationPlayer] 材质未设置！");
                    return;
                }
                
                // 创建 MaterialPropertyBlock 用于设置动画参数
                propertyBlock = new MaterialPropertyBlock();
                
                SetupMaterial();
                
                UnityEngine.Debug.Log($"[BakedAnimationPlayer] 初始化完成: Mesh={mesh.name}, 顶点数={mesh.vertexCount}");
                
                if (autoPlay)
                {
                    Play();
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[BakedAnimationPlayer] {gameObject.name}: BakeData 未设置！");
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
            // 更新 Shader 参数（使用 MaterialPropertyBlock 避免材质实例化）
            currentFrame = Mathf.FloorToInt(currentTime * bakeData.frameRate) % bakeData.totalFrames;
            propertyBlock.SetFloat("_AnimationTime", currentTime);
                
            // 调试输出（每秒输出一次）
            if (Time.frameCount % 60 == 0)
            {
                UnityEngine.Debug.Log($"[BakedAnimationPlayer] Time={currentTime:F2}, Frame={currentFrame}, " +
                    $"FrameRate={bakeData.frameRate}, TotalFrames={bakeData.totalFrames}");
            }
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
                // 确保贴图使用正确的过滤模式
                bakeData.positionMap.filterMode = FilterMode.Bilinear;
                bakeData.positionMap.wrapMode = TextureWrapMode.Clamp;
                runtimeMaterial.SetTexture("_PositionMap", bakeData.positionMap);
                
                // 检查贴图是否可读
                try
                {
                    Color testPixel = bakeData.positionMap.GetPixel(0, 0);
                    UnityEngine.Debug.Log($"[BakedAnimationPlayer] Position Map: {bakeData.positionMap.width}x{bakeData.positionMap.height}, " +
                        $"Format: {bakeData.positionMap.format}, TestPixel(0,0): ({testPixel.r:F3}, {testPixel.g:F3}, {testPixel.b:F3})");
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError($"[BakedAnimationPlayer] 贴图不可读！需要在 Import Settings 中勾选 Read/Write Enabled: {e.Message}");
                }
            }
            else
            {
                UnityEngine.Debug.LogError($"[BakedAnimationPlayer] Position Map 为空！");
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
            
            UnityEngine.Debug.Log($"[BakedAnimationPlayer] Setup Complete: " +
                $"FrameRate={bakeData.frameRate}, TotalFrames={bakeData.totalFrames}, " +
                $"VertexCount={bakeData.vertexCount}, TextureSize={bakeData.textureWidth}x{bakeData.textureHeight}, " +
                $"ClipLength={bakeData.clipLength:F2}s");
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
            
            if (material != null)
            {
                material.SetFloat("_AnimationTime", currentTime);
            }
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
