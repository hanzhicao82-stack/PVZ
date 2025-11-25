using System.Collections.Generic;
using UnityEngine;

public static class UIColorUtils
{
    public static UIColorAsset colorConfig
    {
        get;
        private set;
    }

    public static void LoadGamePlayerConfig()
    {
        colorConfig = Resources.Load<UIColorAsset>(UIColorConfig.ColorConfigName);
        InitRuntimeData();
    }

    private static Dictionary<int, Color> colorDict = new Dictionary<int, Color>();
    private static Dictionary<int, string> colorStringDict = new Dictionary<int, string>();

    public static void InitRuntimeData()
    {
        colorDict.Clear();
        foreach (var single in colorConfig.defList)
        {
            var hash = Animator.StringToHash(single.ColorDefName);
            colorDict[hash] = single.colorValue;
            string color_ = ColorUtility.ToHtmlStringRGB(single.colorValue);
            if (color_ != null && color_.Length == 6)
                color_ = "#" + color_;
            colorStringDict[hash] = color_;
        }
    }

    public static string GetDefColorStr(UIColorGenDef.UIColorConfigDef def)
    {
        int val = (int)def;
        if (colorStringDict.TryGetValue(val, out var value))
            return value;
        Debug.Log($"ui_color_config 中 不存在 {def}");
        return "#FFFFFF";
    }

    public static Color GetDefColor(UIColorGenDef.UIColorConfigDef def)
    {
        int val = (int)def;
        if (colorDict.ContainsKey(val))
        {
            return colorDict[val];
        }
        Debug.Log($"ui_color_config 中 不存在 {def}");
        return Color.white;
    }
}