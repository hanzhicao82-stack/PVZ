#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using UnityEngine.UIElements;

namespace ThunderFireUITool
{
    public class PrefabLabelModifyWindow : EditorWindow
    {
        private static PrefabLabelModifyWindow m_window;
        private static string OriginalLabelName = null;
        private string currentName = null;

        private Action<string> actionAfterClosed = null;

        public static void OpenWindow(string label, Action<string> action = null)
        {
            OriginalLabelName = label;
            int width = 298;
            int height = 155;
            InitWindowData();
            m_window = GetWindow<PrefabLabelModifyWindow>();
            m_window.actionAfterClosed = action;
            m_window.minSize = new Vector2(width, height);
            m_window.titleContent.text = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_组件类型);
        }

        public static string typeNameDes;
        public static string OKText;
        public static string CancelText;

        public static void InitWindowData()
        {
            typeNameDes = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_组件类型) + " :";
            OKText = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_确定);
            CancelText = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_取消);
        }

        private void OnDisable()
        {
            actionAfterClosed.Invoke(currentName);
        }

        private void OnEnable()
        {
            currentName = OriginalLabelName;
            VisualElement root = rootVisualElement;
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ThunderFireUIToolConfig.UIBuilderPath + "Constant/labelModify_popup.uxml");
            VisualElement labelFromUXML = visualTree.CloneTree();
            Label TextLabel = labelFromUXML.Q<Label>("textlabel");
            IMGUIContainer TextInput = labelFromUXML.Q<IMGUIContainer>("textinput");
            VisualElement Confirm = labelFromUXML.Q<VisualElement>("confirm");
            VisualElement Cancel = labelFromUXML.Q<VisualElement>("cancel");
            TextLabel.text = typeNameDes;
            TextLabel.style.fontSize = 14;
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.black;
            style.fontSize = 14;
            //TextInput.onGUIHandler += () => { currentName = GUI.TextField(new Rect(2.5f, 2.5f, 201, 20), currentName, style); };
            TextInput.onGUIHandler += () => { currentName = EditorGUILayout.TextField(currentName, style); };
            SelectorItem textinputS = new SelectorItem(labelFromUXML.Q<VisualElement>("textinputSelector"), TextInput);
            Confirm.Q<Label>("text").text = OKText;
            Cancel.Q<Label>("text").text = CancelText;
            root.Add(labelFromUXML);

            rootVisualElement.RegisterCallback((MouseDownEvent e) =>
            {
                textinputS.UnSelected();

            });
            new SelectorItem(labelFromUXML.Q<VisualElement>("cancelSelector"), Cancel, false);
            new SelectorItem(labelFromUXML.Q<VisualElement>("confirmSelector"), Confirm, false);
            Confirm.RegisterCallback((MouseDownEvent e) =>
            {
                Submit();

            });
            Cancel.RegisterCallback((MouseDownEvent e) =>
            {
                closeWindow();

            });
        }

        //[LabelText("$typeNameDes")]
        //public string labelName = OriginalLabelName;

        //[PropertySpace(10)]
        //[Button("$OKText", ButtonSizes.Large)]

        private void closeWindow()
        {
            if (m_window != null)
            {
                m_window.Close();
            }

        }


        private void Submit()
        {
            if (EditorUtility.DisplayDialog("messageBox", EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_是否要保存修改), EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_确定), EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_取消)))
            {
                if (!string.IsNullOrEmpty(currentName))
                {
                    var labelSetting = JsonAssetManager.GetAssets<WidgetLabelsSettings>();
                    var labelList = labelSetting.labelList;

                    if (!labelList.Contains(currentName))
                    {
                        labelSetting.RemoveLabel(OriginalLabelName);
                        labelSetting.AddNewLabel(currentName);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("messageBox", EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_组件类型重复), EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_确定), EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_取消));
                        return;
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("messageBox", EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_请输入组件类型名称), EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_确定), EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_取消));
                    return;
                }


                if (currentName.Equals(OriginalLabelName))
                {
                    m_window.Close();
                    return;
                }

                m_window.Close();
            }
        }
    }
}
#endif