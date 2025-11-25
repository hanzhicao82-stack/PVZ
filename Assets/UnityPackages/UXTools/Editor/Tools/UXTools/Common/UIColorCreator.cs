#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace ThunderFireUITool
{
    static public class UIColorCreator
    {
        #if UXTOOLS_DEV
        [MenuItem(ThunderFireUIToolConfig.Menu_CreateAssets + "/" + ThunderFireUIToolConfig.UIColor + "/UI Color Assets", false, -48)]
        #endif
        public static void CreateColor()
        {
            var assetPath = UIColorConfig.ColorConfigPath + UIColorConfig.ColorConfigName + ".json";
            UIColorAsset config = JsonAssetManager.CreateAssets<UIColorAsset>(assetPath);
            config.GenColorDefScript();
        }
    }
}
#endif