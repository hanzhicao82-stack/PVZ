#if UNITY_EDITOR
using UnityEditor;

namespace ThunderFireUITool
{
    public static class UXToolsAssetsCreator
    {
        /// <summary>
        /// 初始化所有的配置文件, 出包时使用
        /// </summary>
        #if UXTOOLS_DEV
        [MenuItem(ThunderFireUIToolConfig.Menu_CreateAssets + "/Create All Assets", false, -99)]
        #endif
        public static void CreateAllAssets()
        {
#if UXTOOLS_DEV
            //UXTool Localization
            LocalizationDecode.Decode();
            InspectorLocalizationDecode.Decode();
            LocalizationDecode.BuildUIScript();
            EditorLocalizationSettings.Create();
#endif

            HierarchyManagementSetting.Create();
            HierarchyManagementEditorData.Create();
            HierarchyManagementData.Create();

            UIColorCreator.CreateColor();

            CreateLocationLinesData.Create();
            PrefabOpenedSetting.Create();

            QuickBackgroundData.Create();
            PrefabTabsData.Create();

            SwitchSetting.Create();
        }
    }
}
#endif