#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ThunderFireUITool
{
    
    [Serializable]
    public class WidgetData
    {
        public string GUID;
        public string Label;
        public bool IsPack;
    }

    
    //设为组件的Prefab列表
    [Serializable]
    public class WidgetListSetting
    {
        public List<WidgetData> Data = new List<WidgetData>();

        private static int previewSize = 100;
        public void Add(string guid, string label, bool isPack)
        {
            Data.Add(new WidgetData(){GUID = guid, Label = label, IsPack = isPack});
            JsonAssetManager.SaveAssets(this);
            OnValueChanged();
        }

        public bool Has(string guid)
        {
            var index = Data.FindIndex(i => i.GUID == guid);
            return index >= 0;
        }

        public bool Remove(string guid)
        {
            var index = Data.FindIndex(i => i.GUID == guid); 
            if (index >= 0)
            {   
                Data.RemoveAt(index);
                
                JsonAssetManager.SaveAssets(this);
                OnValueChanged();
                return true;
            }

            return false;
        }

        public void ResortLast(string guid, string label, bool isPack)
        {
            var index = Data.FindIndex(i => i.GUID == guid); 
            if (index >= 0)
            {   
                Data.RemoveAt(index);
            }
            Data.Add(new WidgetData(){GUID = guid, Label = label, IsPack = isPack});
            JsonAssetManager.SaveAssets(this);
            Utils.UpdatePreviewTexture(guid, previewSize);
            OnValueChanged();
        }

        public void RemoveLabel(string label)
        {
            foreach (var info in Data)
            {
                if (info.Label == label)
                {
                    info.Label = null;
                }
            }
            JsonAssetManager.SaveAssets(this);
        }

        private void OnValueChanged()
        {
            if (WidgetRepositoryWindow.GetInstance() != null)
            {
                WidgetRepositoryWindow.GetInstance().RefreshWindow();
            }
        }
    }
}
#endif