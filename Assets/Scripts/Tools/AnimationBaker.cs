using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace PVZ.DOTS.Tools
{
    /// <summary>
    /// 动画烘焙工具 - 将 Animation Clip 烘焙到贴图
    /// 用于 GPU 动画（Vertex Animation Texture, VAT）
    /// </summary>
    public class AnimationBaker : EditorWindow
    {
        [Header("输入")]
        [Tooltip("要烘焙的网格模型")]
        private GameObject targetPrefab;

        [Tooltip("要烘焙的动画片段")]
        private AnimationClip animationClip;

        [Header("烘焙设置")]
        [Tooltip("采样帧率")]
        private int frameRate = 30;

        [Header("输出")]
        [Tooltip("输出文件夹路径")]
        private string outputPath = "Assets/BakedAnimations/";

        [Tooltip("输出文件名前缀")]
        private string outputName = "BakedAnim";

        private Vector2 scrollPos;

        [MenuItem("Tools/Animation Baker")]
        public static void ShowWindow()
        {
            var window = GetWindow<AnimationBaker>("动画烘焙工具");
            window.minSize = new Vector2(400, 600);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUILayout.Label("动画烘焙工具", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // === 输入设置 ===
            EditorGUILayout.LabelField("输入设置", EditorStyles.boldLabel);
            targetPrefab = EditorGUILayout.ObjectField("目标预制体", targetPrefab, typeof(GameObject), false) as GameObject;
            animationClip = EditorGUILayout.ObjectField("动画片段", animationClip, typeof(AnimationClip), false) as AnimationClip;

            GUILayout.Space(10);

            // === 烘焙设置 ===
            EditorGUILayout.LabelField("烘焙设置", EditorStyles.boldLabel);
            frameRate = EditorGUILayout.IntSlider("采样帧率", frameRate, 10, 60);

            GUILayout.Space(10);

            // === 输出设置 ===
            EditorGUILayout.LabelField("输出设置", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            outputPath = EditorGUILayout.TextField("输出路径", outputPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("选择输出文件夹", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    outputPath = "Assets" + path.Replace(Application.dataPath, "") + "/";
                }
            }
            EditorGUILayout.EndHorizontal();

            outputName = EditorGUILayout.TextField("输出名称", outputName);

            GUILayout.Space(20);

            // === 信息显示 ===
            if (targetPrefab != null && animationClip != null)
            {
                EditorGUILayout.HelpBox(GetBakeInfo(), MessageType.Info);
            }

            GUILayout.Space(10);

            // === 烘焙按钮 ===
            GUI.enabled = targetPrefab != null && animationClip != null;

            if (GUILayout.Button("烘焙动画到贴图", GUILayout.Height(40)))
            {
                BakeAnimation();
            }

            GUI.enabled = true;

            GUILayout.Space(10);

            // === 帮助信息 ===
            EditorGUILayout.HelpBox(
                "使用说明：\n" +
                "1. 选择包含 SkinnedMeshRenderer 或 MeshFilter 的预制体\n" +
                "2. 选择要烘焙的 AnimationClip\n" +
                "3. 设置采样帧率和贴图尺寸\n" +
                "4. 点击烘焙按钮\n\n" +
                "输出文件：\n" +
                "- PositionMap.exr（顶点位置贴图）\n" +
                "- NormalMap.exr（法线贴图）\n" +
                "- BakeData.asset（烘焙数据配置）",
                MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        private string GetBakeInfo()
        {
            var meshFilter = targetPrefab.GetComponentInChildren<MeshFilter>();
            var skinnedMesh = targetPrefab.GetComponentInChildren<SkinnedMeshRenderer>();

            Mesh mesh = null;
            if (skinnedMesh != null)
                mesh = skinnedMesh.sharedMesh;
            else if (meshFilter != null)
                mesh = meshFilter.sharedMesh;

            if (mesh == null)
                return "错误：未找到网格数据！";

            int vertexCount = mesh.vertexCount;
            float clipLength = animationClip.length;
            int totalFrames = Mathf.CeilToInt(clipLength * frameRate);

            return $"网格信息：\n" +
                   $"- 顶点数：{vertexCount}\n" +
                   $"- 动画长度：{clipLength:F2} 秒\n" +
                   $"- 采样帧数：{totalFrames} 帧\n" +
                   $"- 贴图尺寸：{vertexCount} × {totalFrames} (自动计算)";
        }

        private void BakeAnimation()
        {
            EditorUtility.DisplayProgressBar("烘焙动画", "准备中...", 0f);

            try
            {
                // 1. 实例化预制体到场景中
                var instance = Instantiate(targetPrefab);
                instance.name = "TempAnimationBaker";

                // 查找所有 SkinnedMeshRenderer
                var skinnedMeshes = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
                if (skinnedMeshes == null || skinnedMeshes.Length == 0)
                {
                    DestroyImmediate(instance);
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("错误", "未找到 SkinnedMeshRenderer 组件！", "确定");
                    return;
                }

                float clipLength = animationClip.length;
                int totalFrames = Mathf.CeilToInt(clipLength * frameRate);

                // 查找 Animator 所在的节点
                Animator existingAnimator = instance.GetComponentInChildren<Animator>();
                GameObject animatorObject = existingAnimator != null ? existingAnimator.gameObject : instance;

                // 移除所有现有的动画组件
                var animators = instance.GetComponentsInChildren<Animator>();
                foreach (var anim in animators)
                {
                    DestroyImmediate(anim);
                }
                var animations = instance.GetComponentsInChildren<Animation>();
                foreach (var anim in animations)
                {
                    DestroyImmediate(anim);
                }

                // 在 Animator 所在的节点添加 Animation 组件
                Animation animation = animatorObject.AddComponent<Animation>();
                animation.playAutomatically = false;
                animation.AddClip(animationClip, animationClip.name);
                animation.clip = animationClip;

                // 验证 AnimationClip 有效性
                EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(animationClip);
                if (curveBindings.Length == 0)
                {
                    UnityEngine.Debug.LogError("[AnimationBaker] AnimationClip 没有任何动画曲线！");
                    DestroyImmediate(instance);
                    EditorUtility.ClearProgressBar();
                    return;
                }

                // 计算总顶点数（所有mesh的顶点数之和）
                int totalVertexCount = 0;
                foreach (var sm in skinnedMeshes)
                {
                    totalVertexCount += sm.sharedMesh.vertexCount;
                }

                // 为每个Mesh创建独立的贴图和数据
                MeshBakeData[] meshDataArray = new MeshBakeData[skinnedMeshes.Length];
                
                for (int meshIndex = 0; meshIndex < skinnedMeshes.Length; meshIndex++)
                {
                    var sm = skinnedMeshes[meshIndex];
                    int vertexCount = sm.sharedMesh.vertexCount;
                    
                    // 获取原始材质的主贴图
                    Texture2D mainTex = null;
                    if (sm.sharedMaterial != null)
                    {
                        mainTex = sm.sharedMaterial.mainTexture as Texture2D;
                    }
                    
                    meshDataArray[meshIndex] = new MeshBakeData
                    {
                        meshName = sm.gameObject.name,
                        vertexCount = vertexCount,
                        positionMap = new Texture2D(vertexCount, totalFrames, TextureFormat.RGBAFloat, false),
                        normalMap = new Texture2D(vertexCount, totalFrames, TextureFormat.RGBAFloat, false),
                        mainTexture = mainTex
                    };
                }

                try
                {
                    // 3. 烘焙每一帧
                    for (int frame = 0; frame < totalFrames; frame++)
                    {
                        float progress = (float)frame / totalFrames;
                        EditorUtility.DisplayProgressBar("烘焙动画", $"处理帧 {frame + 1}/{totalFrames}", progress);

                        float time = (frame / (float)frameRate) % clipLength;

                        // 使用 Animation 组件采样
                        var animState = animation[animationClip.name];
                        if (animState != null)
                        {
                            animState.enabled = true;
                            animState.weight = 1.0f;
                            animState.time = time;
                            animation.Sample();
                        }

                        // 同时使用 SampleAnimation 双保险
                        animationClip.SampleAnimation(animatorObject, time);

                        // 处理所有 SkinnedMeshRenderer
                        for (int meshIndex = 0; meshIndex < skinnedMeshes.Length; meshIndex++)
                        {
                            var skinnedMesh = skinnedMeshes[meshIndex];
                            var meshData = meshDataArray[meshIndex];
                            
                            // 强制更新整个骨骼层级
                            if (skinnedMesh.rootBone != null)
                            {
                                RecursiveUpdateTransforms(skinnedMesh.rootBone);
                            }

                            // 强制更新 SkinnedMeshRenderer
                            skinnedMesh.updateWhenOffscreen = true;
                            skinnedMesh.forceMatrixRecalculationPerRender = true;

                            // 烘焙网格
                            Mesh bakedMesh = new Mesh();
                            skinnedMesh.BakeMesh(bakedMesh);

                            // 保存第一帧的烘焙 Mesh（使用原始 sharedMesh 的拓扑结构）
                            if (frame == 0)
                            {
                                meshData.bakedMesh = new Mesh();
                                // 使用原始 Mesh 的拓扑、UV 等数据
                                meshData.bakedMesh.vertices = skinnedMesh.sharedMesh.vertices; // 使用原始顶点位置
                                meshData.bakedMesh.normals = skinnedMesh.sharedMesh.normals;
                                meshData.bakedMesh.triangles = skinnedMesh.sharedMesh.triangles;
                                meshData.bakedMesh.uv = skinnedMesh.sharedMesh.uv;
                                meshData.bakedMesh.tangents = skinnedMesh.sharedMesh.tangents;
                                meshData.bakedMesh.boneWeights = skinnedMesh.sharedMesh.boneWeights; // 保留骨骼权重信息（调试用）
                                meshData.bakedMesh.RecalculateBounds();
                                meshData.bakedMesh.name = $"{outputName}_BakedMesh_{meshIndex}";
                                
                                UnityEngine.Debug.Log($"[AnimationBaker] Mesh {meshIndex} ({skinnedMesh.gameObject.name}): 顶点数={meshData.vertexCount}, 三角形数={meshData.bakedMesh.triangles.Length / 3}");
                            }

                            // 写入贴图
                            Vector3[] vertices = bakedMesh.vertices;
                            Vector3[] normals = bakedMesh.normals;

                            // 安全检查：确保顶点数匹配
                            if (vertices.Length != meshData.vertexCount)
                            {
                                UnityEngine.Debug.LogError($"[AnimationBaker] Mesh {meshIndex} 帧{frame} 顶点数不匹配！期望: {meshData.vertexCount}, 实际: {vertices.Length}");
                                continue;
                            }
                            
                            // 第一帧时验证数据
                            if (frame == 0)
                            {
                                // 检查是否有异常的顶点位置
                                for (int v = 0; v < vertices.Length; v++)
                                {
                                    if (float.IsNaN(vertices[v].x) || float.IsInfinity(vertices[v].x) ||
                                        float.IsNaN(vertices[v].y) || float.IsInfinity(vertices[v].y) ||
                                        float.IsNaN(vertices[v].z) || float.IsInfinity(vertices[v].z))
                                    {
                                        UnityEngine.Debug.LogError($"[AnimationBaker] Mesh {meshIndex} 顶点{v} 位置异常: {vertices[v]}");
                                    }
                                }
                            }

                            for (int v = 0; v < meshData.vertexCount; v++)
                            {
                                // 位置贴图（RGB = XYZ）
                                Vector3 pos = vertices[v];
                                meshData.positionMap.SetPixel(v, frame, new Color(pos.x, pos.y, pos.z, 1f));

                                // 法线贴图（RGB = XYZ）
                                Vector3 normal = normals[v];
                                meshData.normalMap.SetPixel(v, frame, new Color(
                                    normal.x * 0.5f + 0.5f,
                                    normal.y * 0.5f + 0.5f,
                                    normal.z * 0.5f + 0.5f,
                                    1f
                                ));
                            }
                        }
                    }
                }

                finally
                {
                    // 清理临时对象
                    DestroyImmediate(instance);
                }

                // 4. 应用并保存所有贴图
                for (int i = 0; i < meshDataArray.Length; i++)
                {
                    meshDataArray[i].positionMap.Apply();
                    meshDataArray[i].normalMap.Apply();
                }

                // 确保输出目录存在
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                // 5. 保存所有贴图和 Mesh
                for (int i = 0; i < meshDataArray.Length; i++)
                {
                    var meshData = meshDataArray[i];
                    
                    // 保存贴图
                    string posMapPath = $"{outputPath}{outputName}_{i}_PositionMap.exr";
                    string normalMapPath = $"{outputPath}{outputName}_{i}_NormalMap.exr";
                    
                    byte[] posBytes = meshData.positionMap.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
                    byte[] normalBytes = meshData.normalMap.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
                    
                    File.WriteAllBytes(posMapPath, posBytes);
                    File.WriteAllBytes(normalMapPath, normalBytes);
                    
                    // 保存 Mesh
                    string meshPath = $"{outputPath}{outputName}_{i}_BakedMesh.asset";
                    AssetDatabase.CreateAsset(meshData.bakedMesh, meshPath);
                }

                // 6. 创建配置数据
                AnimationBakeData bakeData = ScriptableObject.CreateInstance<AnimationBakeData>();
                bakeData.animationName = animationClip.name;
                bakeData.frameRate = frameRate;
                bakeData.totalFrames = totalFrames;
                bakeData.clipLength = clipLength;
                bakeData.textureWidth = meshDataArray.Length > 0 ? meshDataArray[0].vertexCount : 0;
                bakeData.textureHeight = totalFrames;
                bakeData.meshDataArray = meshDataArray;
                
                // 兼容性：第一个mesh作为主mesh
                if (meshDataArray.Length > 0)
                {
                    bakeData.vertexCount = meshDataArray[0].vertexCount;
                    bakeData.bakedMesh = meshDataArray[0].bakedMesh;
                }

                string dataPath = $"{outputPath}{outputName}_BakeData.asset";
                AssetDatabase.CreateAsset(bakeData, dataPath);

                // 7. 刷新
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // 8. 配置所有贴图导入设置
                for (int i = 0; i < meshDataArray.Length; i++)
                {
                    string posMapPath = $"{outputPath}{outputName}_{i}_PositionMap.exr";
                    string normalMapPath = $"{outputPath}{outputName}_{i}_NormalMap.exr";
                    ConfigureTextureImport(posMapPath);
                    ConfigureTextureImport(normalMapPath);
                }
                AssetDatabase.Refresh();

                // 9. 重新加载 BakeData 并保存所有引用
                AnimationBakeData loadedBakeData = AssetDatabase.LoadAssetAtPath<AnimationBakeData>(dataPath);
                if (loadedBakeData != null)
                {
                    for (int i = 0; i < meshDataArray.Length; i++)
                    {
                        string posMapPath = $"{outputPath}{outputName}_{i}_PositionMap.exr";
                        string normalMapPath = $"{outputPath}{outputName}_{i}_NormalMap.exr";
                        string meshPath = $"{outputPath}{outputName}_{i}_BakedMesh.asset";
                        
                        loadedBakeData.meshDataArray[i].positionMap = AssetDatabase.LoadAssetAtPath<Texture2D>(posMapPath);
                        loadedBakeData.meshDataArray[i].normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>(normalMapPath);
                        loadedBakeData.meshDataArray[i].bakedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    }
                    
                    // 兼容性：设置第一个mesh的引用
                    if (loadedBakeData.meshDataArray.Length > 0)
                    {
                        loadedBakeData.positionMap = loadedBakeData.meshDataArray[0].positionMap;
                        loadedBakeData.normalMap = loadedBakeData.meshDataArray[0].normalMap;
                        loadedBakeData.bakedMesh = loadedBakeData.meshDataArray[0].bakedMesh;
                    }
                    
                    EditorUtility.SetDirty(loadedBakeData);
                    AssetDatabase.SaveAssets();
                }

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("成功",
                    $"动画烘焙完成！\n\n" +
                    $"烘焙了 {skinnedMeshes.Length} 个Mesh\n" +
                    $"总顶点数: {totalVertexCount}\n\n" +
                    $"输出文件：\n" +
                    $"- {dataPath}",
                    "确定");

                // 选中输出文件夹
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(outputPath);
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("错误", $"烘焙失败：\n{e.Message}", "确定");
                UnityEngine.Debug.LogError($"动画烘焙错误：{e}");
            }
        }

        /// <summary>
        /// 配置贴图导入设置
        /// </summary>
        private void ConfigureTextureImport(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.isReadable = true;
                importer.sRGBTexture = false; // 非常重要！位置数据不是颜色
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None; // 不缩放非2次幂贴图
                importer.maxTextureSize = 8192; // 最大尺寸
                importer.SaveAndReimport();
            }
        }

        // 辅助方法：递归更新所有子 Transform
        private void RecursiveUpdateTransforms(Transform root)
        {
            // 强制更新本地变换矩阵
            root.localPosition = root.localPosition;
            root.localRotation = root.localRotation;
            root.localScale = root.localScale;

            // 递归更新所有子物体
            for (int i = 0; i < root.childCount; i++)
            {
                RecursiveUpdateTransforms(root.GetChild(i));
            }
        }
    }
}
