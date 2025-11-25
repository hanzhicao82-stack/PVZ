#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace ThunderFireUITool
{
    public static class PrefabUtils
    {
        public static List<WidgetData> GetWidgetList()
        {
            WidgetListSetting list = JsonAssetManager.GetAssets<WidgetListSetting>();
            var data = list.Data;
            for (int i = data.Count - 1; i >= 0; i--)
            {
                string path = AssetDatabase.GUIDToAssetPath(data[i].GUID);
                if (!File.Exists(path) || path == "")
                {
                    data.RemoveAt(i);
                }

            }
            return data;
        }

        public static void DeleteLabel(string label)
        {
            WidgetListSetting list = JsonAssetManager.GetAssets<WidgetListSetting>();
            list.RemoveLabel(label);
            
            //删除setting中的label数据
            var labelSetting = JsonAssetManager.GetAssets<WidgetLabelsSettings>();
            labelSetting.RemoveLabel(label);
        }
    }
}
#endif
