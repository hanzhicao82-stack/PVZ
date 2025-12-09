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
                // 1. 实例化预制体到场景中（而不是临时对象）
                var instance = Instantiate(targetPrefab);
                instance.name = "TempAnimationBaker";
                
                var skinnedMesh = instance.GetComponentInChildren<SkinnedMeshRenderer>();
                if (skinnedMesh == null)
                {
                    DestroyImmediate(instance);
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("错误", "未找到 SkinnedMeshRenderer 组件！", "确定");
                    return;
                }

                Mesh bakedMesh = new Mesh();
                int vertexCount = skinnedMesh.sharedMesh.vertexCount;
                float clipLength = animationClip.length;
                int totalFrames = Mathf.CeilToInt(clipLength * frameRate);
                
                // 查找 Animator 所在的节点
                Animator existingAnimator = instance.GetComponentInChildren<Animator>();
                GameObject animatorObject = existingAnimator != null ? existingAnimator.gameObject : instance;
                
                UnityEngine.Debug.Log($"[AnimationBaker] Animator 位于: {GetTransformPath(animatorObject.transform, instance.transform)}");
                
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
                
                UnityEngine.Debug.Log($"[AnimationBaker] 在节点 '{animatorObject.name}' 添加 Animation 组件");
                
                // 诊断：检查 AnimationClip 的曲线绑定
                EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(animationClip);
                UnityEngine.Debug.Log($"[AnimationBaker] AnimationClip 诊断:");
                UnityEngine.Debug.Log($"  - 曲线数量: {curveBindings.Length}");
                if (curveBindings.Length > 0)
                {
                    UnityEngine.Debug.Log($"  - 前3个曲线: ");
                    for (int i = 0; i < Mathf.Min(3, curveBindings.Length); i++)
                    {
                        var binding = curveBindings[i];
                        UnityEngine.Debug.Log($"    [{i}] Path={binding.path}, Property={binding.propertyName}, Type={binding.type}");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError("[AnimationBaker] AnimationClip 没有任何动画曲线！这可能不是一个有效的动画片段。");
                    DestroyImmediate(instance);
                    EditorUtility.ClearProgressBar();
                    return;
                }
                
                // 检查骨骼层级
                UnityEngine.Debug.Log($"[AnimationBaker] SkinnedMeshRenderer 诊断:");
                UnityEngine.Debug.Log($"  - 骨骼数量: {(skinnedMesh.bones != null ? skinnedMesh.bones.Length : 0)}");
                UnityEngine.Debug.Log($"  - RootBone: {(skinnedMesh.rootBone != null ? skinnedMesh.rootBone.name : "null")}");
                if (skinnedMesh.bones != null && skinnedMesh.bones.Length > 0)
                {
                    UnityEngine.Debug.Log($"  - 前3根骨骼: ");
                    for (int i = 0; i < Mathf.Min(3, skinnedMesh.bones.Length); i++)
                    {
                        if (skinnedMesh.bones[i] != null)
                        {
                            Transform bone = skinnedMesh.bones[i];
                            UnityEngine.Debug.Log($"    [{i}] {bone.name} (Path: {GetTransformPath(bone, instance.transform)})");
                        }
                    }
                }

                // 2. 自动设置正确的贴图尺寸（必须等于数据尺寸）
                int actualWidth = vertexCount;
                int actualHeight = totalFrames;
                
                UnityEngine.Debug.Log($"[AnimationBaker] 使用贴图尺寸: {actualWidth}x{actualHeight} (顶点数x帧数)");
                
                // 创建贴图
                Texture2D positionMap = new Texture2D(actualWidth, actualHeight, TextureFormat.RGBAFloat, false);
                Texture2D normalMap = new Texture2D(actualWidth, actualHeight, TextureFormat.RGBAFloat, false);

                // 保存第一帧的烘焙 Mesh 作为基础网格
                Mesh savedBakedMesh = null;
                
                // 调试：记录第一帧和最后一帧的位置差异
                Vector3[] firstFrameVertices = null;

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
                            animation.Sample(); // 强制采样
                        }
                        
                        // 同时使用 SampleAnimation 双保险（注意：需要在正确的根节点上调用）
                        animationClip.SampleAnimation(animatorObject, time);
                        
                        // 强制更新整个骨骼层级
                        if (skinnedMesh.rootBone != null)
                        {
                            RecursiveUpdateTransforms(skinnedMesh.rootBone);
                        }
                    
                        // 强制更新 SkinnedMeshRenderer
                        skinnedMesh.updateWhenOffscreen = true;
                        skinnedMesh.forceMatrixRecalculationPerRender = true;
                        
                        // 调试：检查骨骼位置（每5帧输出一次）
                        Transform[] bones = skinnedMesh.bones;
                        if (frame % 5 == 0 || frame == totalFrames - 1)
                        {
                            Vector3 firstBonePos = bones != null && bones.Length > 0 && bones[0] != null ? bones[0].localPosition : Vector3.zero;
                            Quaternion firstBoneRot = bones != null && bones.Length > 0 && bones[0] != null ? bones[0].localRotation : Quaternion.identity;
                            UnityEngine.Debug.Log($"[AnimationBaker] 帧{frame}/{totalFrames-1} Time={time:F3}s 骨骼[0]: Pos={firstBonePos} Rot={firstBoneRot.eulerAngles}");
                        }
                        
                        // 烘焙网格
                        skinnedMesh.BakeMesh(bakedMesh);

                        // 保存第一帧的烘焙 Mesh
                        if (frame == 0)
                    {
                        savedBakedMesh = new Mesh();
                        savedBakedMesh.vertices = bakedMesh.vertices;
                        savedBakedMesh.normals = bakedMesh.normals;
                        savedBakedMesh.triangles = bakedMesh.triangles;
                        savedBakedMesh.uv = bakedMesh.uv;
                        savedBakedMesh.tangents = bakedMesh.tangents;
                        savedBakedMesh.RecalculateBounds();
                        savedBakedMesh.name = $"{outputName}_BakedMesh";
                        
                        // 保存第一帧顶点位置用于比较
                        firstFrameVertices = (Vector3[])bakedMesh.vertices.Clone();
                    }
                    
                    // 最后一帧时检查位置变化
                    if (frame == totalFrames - 1 && firstFrameVertices != null)
                    {
                        Vector3[] lastFrameVertices = bakedMesh.vertices;
                        float maxDiff = 0f;
                        int maxDiffVertex = 0;
                        
                        for (int v = 0; v < Mathf.Min(vertexCount, firstFrameVertices.Length, lastFrameVertices.Length); v++)
                        {
                            float diff = Vector3.Distance(firstFrameVertices[v], lastFrameVertices[v]);
                            if (diff > maxDiff)
                            {
                                maxDiff = diff;
                                maxDiffVertex = v;
                            }
                        }
                        
                        UnityEngine.Debug.Log($"[AnimationBaker] 动画检测: 最大顶点位置变化 = {maxDiff:F4} (顶点 {maxDiffVertex})");
                        
                        if (maxDiff < 0.001f)
                        {
                            UnityEngine.Debug.LogWarning($"[AnimationBaker] 警告：动画几乎没有顶点位置变化！请检查：\n" +
                                "1. AnimationClip 是否包含骨骼动画\n" +
                                "2. 预制体的骨骼绑定是否正确\n" +
                                "3. 是否选择了正确的动画片段");
                        }
                    }

                    // 写入贴图
                    Vector3[] vertices = bakedMesh.vertices;
                    Vector3[] normals = bakedMesh.normals;

                    for (int v = 0; v < vertexCount; v++)
                    {
                        if (frame < totalFrames)
                        {
                            // 位置贴图（RGB = XYZ）
                            Vector3 pos = vertices[v];
                            positionMap.SetPixel(v, frame, new Color(pos.x, pos.y, pos.z, 1f));

                            // 法线贴图（RGB = XYZ）
                            Vector3 normal = normals[v];
                            normalMap.SetPixel(v, frame, new Color(
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

                // 4. 应用并保存贴图
                positionMap.Apply();
                normalMap.Apply();

                // 确保输出目录存在
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                // 保存为 EXR 格式（支持浮点数）
                string posMapPath = $"{outputPath}{outputName}_PositionMap.exr";
                string normalMapPath = $"{outputPath}{outputName}_NormalMap.exr";

                byte[] posBytes = positionMap.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
                byte[] normalBytes = normalMap.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);

                File.WriteAllBytes(posMapPath, posBytes);
                File.WriteAllBytes(normalMapPath, normalBytes);

                // 5. 保存烘焙后的 Mesh
                string meshPath = $"{outputPath}{outputName}_BakedMesh.asset";
                AssetDatabase.CreateAsset(savedBakedMesh, meshPath);

                // 6. 创建配置数据
                AnimationBakeData bakeData = ScriptableObject.CreateInstance<AnimationBakeData>();
                bakeData.animationName = animationClip.name;
                bakeData.frameRate = frameRate;
                bakeData.totalFrames = totalFrames;
                bakeData.vertexCount = vertexCount;
                bakeData.clipLength = clipLength;
                bakeData.textureWidth = vertexCount;  // 实际使用的宽度
                bakeData.textureHeight = totalFrames; // 实际使用的高度

                string dataPath = $"{outputPath}{outputName}_BakeData.asset";
                AssetDatabase.CreateAsset(bakeData, dataPath);

                // 7. 清理和刷新
                DestroyImmediate(instance);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // 7. 配置贴图导入设置
                ConfigureTextureImport(posMapPath);
                ConfigureTextureImport(normalMapPath);
                AssetDatabase.Refresh();

                // 8. 重新加载 BakeData 并保存贴图引用
                AnimationBakeData loadedBakeData = AssetDatabase.LoadAssetAtPath<AnimationBakeData>(dataPath);
                if (loadedBakeData != null)
                {
                    loadedBakeData.positionMap = AssetDatabase.LoadAssetAtPath<Texture2D>(posMapPath);
                    loadedBakeData.normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>(normalMapPath);
                    loadedBakeData.bakedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    EditorUtility.SetDirty(loadedBakeData);
                    AssetDatabase.SaveAssets();
                }

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("成功", 
                    $"动画烘焙完成！\n\n" +
                    $"输出文件：\n" +
                    $"- {posMapPath}\n" +
                    $"- {normalMapPath}\n" +
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
                
                UnityEngine.Debug.Log($"[AnimationBaker] 配置贴图导入设置: {path}");
            }
        }
        
        // 辅助方法：获取 Transform 的完整路径
        private string GetTransformPath(Transform transform, Transform root)
        {
            if (transform == root) return "";
            
            string path = transform.name;
            Transform current = transform.parent;
            
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            
            return path;
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

    /// <summary>
    /// 动画烘焙数据配置
    /// </summary>
    public class AnimationBakeData : ScriptableObject
    {
        public string animationName;
        public int frameRate;
        public int totalFrames;
        public int vertexCount;
        public float clipLength;
        public int textureWidth;
        public int textureHeight;

        [Header("资源引用")]
        public Texture2D positionMap;
        public Texture2D normalMap;
        public Mesh bakedMesh;
    }
}
