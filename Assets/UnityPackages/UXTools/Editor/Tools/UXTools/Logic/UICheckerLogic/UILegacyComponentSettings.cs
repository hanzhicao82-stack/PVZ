#if UNITY_EDITOR && ODIN_INSPECTOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ThunderFireUITool
{
    public class UILegacyComponentSettings : ScriptableObject
    {
        #if UXTOOLS_DEV
        [MenuItem(ThunderFireUIToolConfig.Menu_CreateAssets + "/" + ThunderFireUIToolConfig.ResourceCheck + "/UILegacyComponentCheckSetting", false, -1)]
        #endif
        public static UILegacyComponentSettings Create()
        {
            var settings = CreateInstance<UILegacyComponentSettings>();
            if (settings == null)
                Debug.LogError("Create UIAtlasCheckRuleSettings Failed!");

            string path = Path.GetDirectoryName(ThunderFireUIToolConfig.UICheckLegacyComponentFullPath);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            settings.LegacyComponents = new List<MonoScript>();
            AssetDatabase.CreateAsset(settings, ThunderFireUIToolConfig.UICheckLegacyComponentFullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return settings;
        }
        public List<MonoScript> LegacyComponents;
    }
}
#endif