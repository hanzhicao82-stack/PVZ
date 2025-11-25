#if UNITY_EDITOR && ODIN_INSPECTOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ThunderFireUITool
{
    public class UIAtlasCheckUserData : ScriptableObject
    {
        #if UXTOOLS_DEV
        [MenuItem(ThunderFireUIToolConfig.Menu_CreateAssets + "/" + ThunderFireUIToolConfig.ResourceCheck + "/UIResCheckUserData", false, -1)]
        #endif
        public static UIAtlasCheckUserData Create()
        {
            var settings = CreateInstance<UIAtlasCheckUserData>();
            if (settings == null)
                Debug.LogError("Create UIAtlasCheckUserData Failed!");

            string path = Path.GetDirectoryName(ThunderFireUIToolConfig.UICheckUserDataFullPath);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var assetPath = ThunderFireUIToolConfig.UICheckUserDataFullPath;
            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return settings;
        }

        public bool UICheckEnable = false;

        public void Save(bool enable)
        {
            UICheckEnable = enable;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif