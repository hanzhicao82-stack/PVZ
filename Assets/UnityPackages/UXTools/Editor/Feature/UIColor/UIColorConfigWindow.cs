#if UNITY_EDITOR
using ThunderFireUITool;
using UnityEditor;
using UnityEngine;

public class UIColorConfigWindow : EditorWindow
{
    public static int select = 0;
    private string[] names = new string[2];
    private UIColorAsset colorConfigScriptObject;
    private Editor colorConfigEditor;

    private Vector2 scrollPos;

    private string SaveString;

    //  TODO 先屏蔽掉，优化完再打开
    //[MenuItem(ThunderFireUIToolConfig.Menu_UIColor, false, 53)]
    public static void ShowObjectWindow()
    {
        var window = GetWindow<UIColorConfigWindow>(true, EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_颜色预设编辑器), true);
        window.minSize = new Vector2(550, 450);
        select = 0;
    }

    private void OnEnable()
    {
        names[0] = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_颜色);
        names[1] = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_渐变);
        SaveString = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_保存);

        colorConfigScriptObject = JsonAssetManager.GetAssets<UIColorAsset>();//AssetDatabase.LoadAssetAtPath<UIColorAsset>(UIColorConfig.ColorConfigPath + UIColorConfig.ColorConfigName + ".asset");

        colorConfigEditor = Editor.CreateEditor(colorConfigScriptObject);
    }

    private void OnDestroy()
    {
        DestroyImmediate(colorConfigScriptObject);
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        select = GUILayout.Toolbar(select, names, GUILayout.Width(120), GUILayout.Height(25));
        //Debug.Log(select);
        if (select == 0)
        {
            colorConfigEditor.OnInspectorGUI();
        }
        EditorGUILayout.EndScrollView();
        if (GUILayout.Button(SaveString))
        {
            if (select == 0)
            {
                SaveColor();
                if (ColorChooseWindow.r_window != null)
                {
                    ColorChooseWindow.r_window.Refresh();
                }
            }
        }
    }

    private void SaveColor()
    {
        if (colorConfigScriptObject.HasSameNameOrEmptyName())
        {
            EditorUtility.DisplayDialog("Warning", "Has Empty Name or Duplicated Name.",
                EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_确定));
            return;
        }

        JsonAssetManager.SaveAssets(colorConfigScriptObject);
        colorConfigScriptObject.GenColorDefScript();
    }

}
#endif