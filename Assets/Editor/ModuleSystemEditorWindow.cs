using UnityEditor;
using UnityEngine;
using Framework;
using System.Linq;

namespace PVZ
{
    /// <summary>
    /// 模块系统编辑器窗口
    /// </summary>
    public class ModuleSystemEditorWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private TextAsset _configFile;
        private GameConfiguration _config;
        private bool[] _moduleFoldouts;

        [MenuItem("PVZ/模块系统管理器")]
        public static void ShowWindow()
        {
            var window = GetWindow<ModuleSystemEditorWindow>("模块系统管理器");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            // 尝试加载默认配置
            _configFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Configs/GameModuleConfig.json");
            if (_configFile != null)
            {
                LoadConfiguration();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            DrawHeader();
            DrawConfigSelector();
            
            if (_config != null)
            {
                DrawModuleList();
                DrawActions();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("模块系统管理器", EditorStyles.boldLabel);
            EditorGUILayout.Space();
        }

        private void DrawConfigSelector()
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("配置文件:", GUILayout.Width(80));
            
            var newConfigFile = EditorGUILayout.ObjectField(_configFile, typeof(TextAsset), false) as TextAsset;
            if (newConfigFile != _configFile)
            {
                _configFile = newConfigFile;
                LoadConfiguration();
            }

            if (GUILayout.Button("刷新", GUILayout.Width(60)))
            {
                LoadConfiguration();
            }

            if (GUILayout.Button("保存", GUILayout.Width(60)))
            {
                SaveConfiguration();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void DrawModuleList()
        {
            if (_config.modules == null || _config.modules.Count == 0)
            {
                EditorGUILayout.HelpBox("没有配置任何模块", UnityEditor.MessageType.Warning);
                return;
            }

            // 初始化折叠状态数组
            if (_moduleFoldouts == null || _moduleFoldouts.Length != _config.modules.Count)
            {
                _moduleFoldouts = new bool[_config.modules.Count];
            }

            EditorGUILayout.LabelField($"项目: {_config.projectName} (v{_config.version})", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"类型: {_config.projectType}");
            EditorGUILayout.Space();

            EditorGUILayout.LabelField($"模块列表 ({_config.modules.Count} 个)", EditorStyles.boldLabel);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (int i = 0; i < _config.modules.Count; i++)
            {
                DrawModuleItem(_config.modules[i], i);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawModuleItem(ModuleConfig module, int index)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            EditorGUILayout.BeginHorizontal();
            
            // 启用开关
            bool newEnabled = EditorGUILayout.Toggle(module.enabled, GUILayout.Width(20));
            if (newEnabled != module.enabled)
            {
                module.enabled = newEnabled;
                EditorUtility.SetDirty(_configFile);
            }

            // 模块名称（使用折叠标签）
            _moduleFoldouts[index] = EditorGUILayout.Foldout(_moduleFoldouts[index], 
                GetModuleDisplayName(module.moduleId), true);

            // 状态指示
            GUILayout.FlexibleSpace();
            var statusColor = module.enabled ? Color.green : Color.gray;
            GUI.color = statusColor;
            GUILayout.Label(module.enabled ? "●" : "○", GUILayout.Width(20));
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();

            // 折叠内容
            if (_moduleFoldouts[index])
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField("模块ID:", module.moduleId);
                
                // 显示依赖关系
                var dependencies = GetModuleDependencies(module.moduleId);
                if (dependencies != null && dependencies.Length > 0)
                {
                    EditorGUILayout.LabelField("依赖:", string.Join(", ", dependencies));
                }

                // 参数编辑
                EditorGUILayout.LabelField("参数:", EditorStyles.miniLabel);
                string newParams = EditorGUILayout.TextArea(module.parametersJson, GUILayout.Height(40));
                if (newParams != module.parametersJson)
                {
                    module.parametersJson = newParams;
                    EditorUtility.SetDirty(_configFile);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void DrawActions()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("全部启用", GUILayout.Height(30)))
            {
                foreach (var module in _config.modules)
                {
                    module.enabled = true;
                }
                EditorUtility.SetDirty(_configFile);
            }

            if (GUILayout.Button("全部禁用", GUILayout.Height(30)))
            {
                foreach (var module in _config.modules)
                {
                    module.enabled = false;
                }
                EditorUtility.SetDirty(_configFile);
            }

            if (GUILayout.Button("验证依赖", GUILayout.Height(30)))
            {
                ValidateDependencies();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (GUILayout.Button("打开文档", GUILayout.Height(25)))
            {
                var docPath = "Assets/Docs/ModuleSystem_Guide.md";
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(docPath);
                if (asset != null)
                {
                    AssetDatabase.OpenAsset(asset);
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "文档文件不存在:\n" + docPath, "确定");
                }
            }
        }

        private void LoadConfiguration()
        {
            if (_configFile == null)
            {
                _config = null;
                return;
            }

            try
            {
                _config = JsonUtility.FromJson<GameConfiguration>(_configFile.text);
                UnityEngine.Debug.Log($"配置加载成功: {_config.modules.Count} 个模块");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"配置文件解析失败:\n{ex.Message}", "确定");
                _config = null;
            }
        }

        private void SaveConfiguration()
        {
            if (_configFile == null || _config == null)
                return;

            try
            {
                string json = JsonUtility.ToJson(_config, true);
                string path = AssetDatabase.GetAssetPath(_configFile);
                System.IO.File.WriteAllText(path, json);
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("成功", "配置已保存", "确定");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"保存失败:\n{ex.Message}", "确定");
            }
        }

        private string GetModuleDisplayName(string moduleId)
        {
            // 尝试从模块工厂获取显示名称
            ModuleFactory.Initialize();
            var moduleType = ModuleFactory.GetModuleType(moduleId);
            
            if (moduleType != null)
            {
                try
                {
                    var instance = System.Activator.CreateInstance(moduleType) as IGameModule;
                    return $"{instance.DisplayName} ({moduleId})";
                }
                catch { }
            }

            return moduleId;
        }

        private string[] GetModuleDependencies(string moduleId)
        {
            ModuleFactory.Initialize();
            var moduleType = ModuleFactory.GetModuleType(moduleId);
            
            if (moduleType != null)
            {
                try
                {
                    var instance = System.Activator.CreateInstance(moduleType) as IGameModule;
                    return instance.Dependencies;
                }
                catch { }
            }

            return null;
        }

        private void ValidateDependencies()
        {
            if (_config == null || _config.modules == null)
                return;

            ModuleFactory.Initialize();
            var errors = new System.Text.StringBuilder();
            var enabledModules = _config.modules.Where(m => m.enabled).Select(m => m.moduleId).ToList();

            foreach (var module in _config.modules.Where(m => m.enabled))
            {
                var dependencies = GetModuleDependencies(module.moduleId);
                if (dependencies != null)
                {
                    foreach (var dep in dependencies)
                    {
                        if (!enabledModules.Contains(dep))
                        {
                            errors.AppendLine($"✗ 模块 [{module.moduleId}] 依赖的模块 [{dep}] 未启用");
                        }
                    }
                }
            }

            if (errors.Length > 0)
            {
                EditorUtility.DisplayDialog("依赖验证失败", errors.ToString(), "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("验证成功", "所有依赖关系正确!", "确定");
            }
        }
    }
}
