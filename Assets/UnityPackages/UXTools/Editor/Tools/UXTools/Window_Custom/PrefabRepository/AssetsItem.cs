#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;


using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Object = System.Object;

namespace ThunderFireUITool
{
    public class AssetsItem : VisualElement
    {
        // public static int size_h = 192;//资产块初始大小
        public static int size_w = 156;

        public GameObject assetsObj;
        public string path;
        //public int size = ThunderFireUIToolConfig.DefaultAssetsItemSize;

        public static int maxCharactersNum = 20;
        public Image thumbnail;
        //public TextElement nameLabel;
        public Label nameLabel;
        private VisualElement upContainer;
        private VisualElement imageDiv;
        public bool Selected;
        private Action refreshWindow = null;
        private string assetName;
        public bool isPack;
        public string label;

        public AssetsItem(WidgetData info, bool isPrefabRecent, Action refresh = null, float scale = 1)
        {
            //Debug.Log("AssetsItem");
            style.width = scale == 0 ? Length.Percent(100) : size_w * scale;
            // style.height = size_h;
            // style.marginTop = 5;
            style.marginRight = 12;
            // style.marginLeft = 5;
            style.marginBottom = scale == 0 ? 5 : 12;

            label = info.Label;
            isPack = info.IsPack;
            path = AssetDatabase.GUIDToAssetPath(info.GUID);
            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            assetName = obj.name;
            assetsObj = obj;
            var row = UXBuilder.Row(this,
                new UXBuilderRowStruct()
                {
                    style = new UXStyle()
                    {
                        width = Length.Percent(100),
                        // height = Length.Percent(100),

                    }
                });

            upContainer = UXBuilder.Div(row, new UXBuilderDivStruct()
            {
                style = new UXStyle()
                {
                    alignItems = Align.Center,
                    backgroundColor = Color.clear,
                    flexDirection = scale == 0 ? FlexDirection.Row : FlexDirection.Column
                }
            });

            int borderWidth = 1;
            upContainer.style.borderTopWidth = borderWidth;
            upContainer.style.borderBottomWidth = borderWidth;
            upContainer.style.borderLeftWidth = borderWidth;
            upContainer.style.borderRightWidth = borderWidth;

            imageDiv = UXBuilder.Div(upContainer, new UXBuilderDivStruct()
            {
                style = new UXStyle()
                {
                    height = scale == 0 ? 20 : 154 * scale,
                    width = scale == 0 ? 20 : 154 * scale,
                    backgroundColor = new Color(63f / 255f, 63f / 255f, 63f / 255f),
                }
            });

            thumbnail = new Image();
            // thumbnail.style.position = Position.Absolute;
            string guid = AssetDatabase.AssetPathToGUID(path);
            Texture previewTex = Utils.GetAssetsPreviewTexture(guid, 144);
            thumbnail.image = previewTex;
            imageDiv.Add(thumbnail);

            nameLabel = UXBuilder.Text(upContainer, new UXBuilderTextStruct()
            {
                style = new UXStyle()
                {
                    // height = 36,
                    alignSelf = Align.Center,
                    fontSize = 14,
                    color = Color.white,
                    // width = Length.Percent(60),
                    unityTextAlign = TextAnchor.MiddleCenter,
                    overflow = Overflow.Hidden,
                    // whiteSpace = WhiteSpace.NoWrap,
                },
                text = assetName,
            });
            nameLabel.tooltip = assetName;

            // Debug.Log(evt.oldRect + " " + evt.newRect);
            // UIElementUtils.TextOverflowWithEllipsis(nameLabel);
            upContainer.RegisterCallback<MouseEnterEvent>(OnHoverStateChange);
            upContainer.RegisterCallback<MouseLeaveEvent>(OnHoverStateChange);

            upContainer.RegisterCallback<MouseDownEvent, bool>(OnClick, isPrefabRecent);


            refreshWindow = refresh;
        }

        private void OnHoverStateChange(EventBase e)
        {
            if (e.eventTypeId == MouseEnterEvent.TypeId())
            {
                upContainer.style.backgroundColor = new Color(36f / 255f, 99f / 255f, 193f / 255f, 0.5f);
                imageDiv.style.backgroundColor = Color.clear;
            }
            else if (e.eventTypeId == MouseLeaveEvent.TypeId())
            {
                upContainer.style.backgroundColor = Color.clear;
                imageDiv.style.backgroundColor = new Color(63f / 255f, 63f / 255f, 63f / 255f);
            }
        }

        public void SetSelected(bool selected)
        {
            Selected = selected;
            ChangeColor();
        }

        private void ChangeColor()
        {
            if (Selected)
            {
                Color mycolor = new Color(27f / 255f, 150f / 255f, 233f / 255f, 1f);
                upContainer.style.borderTopColor = mycolor;
                upContainer.style.borderBottomColor = mycolor;
                upContainer.style.borderLeftColor = mycolor;
                upContainer.style.borderRightColor = mycolor;
                // upContainer.Q<CustomTextElement>("NameOfContainer").schedule.Execute(() => upContainer.Q<VisualElement>("NameOfContainer").SetWithFullText());
            }
            else
            {
                Color mycolor = Color.clear;
                upContainer.style.borderTopColor = mycolor;
                upContainer.style.borderBottomColor = mycolor;
                upContainer.style.borderLeftColor = mycolor;
                upContainer.style.borderRightColor = mycolor;
                // upContainer.Q<CustomTextElement>("NameOfContainer").schedule.Execute(() => upContainer.Q<VisualElement>("NameOfContainer").SetTextWithEllipsis(CustomTextElement.CountCharacterMode.WithNumOfPixels));
            }
        }

        private void OnClick(MouseDownEvent e, bool y)
        {
            if (e.button == 0)
            {
                if (y) PrefabRecentWindow.clickFlag = true;
                else WidgetRepositoryWindow.clickFlag = true;

                if (e.clickCount == 2)
                {
                    Utils.OpenPrefab(path);
                    return;
                }
            }

            else if (e.button == 1)
            {
                //Right Mouse Button
                var menu = new GenericMenu();

                if (y)
                {
                    menu.AddItem(new GUIContent(EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_删除)),
                        false, OpenDeleteRecent);
                }
                else
                {
                    // menu.AddItem(new GUIContent(EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_修改信息)), false, () =>
                    // {
                    //     PrefabModifyWindow.OpenWindow(assetsObj, refreshWindow);
                    // });
                    menu.AddItem(new GUIContent(EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_删除)),
                        false, OpenDeleteSettled);

                }
                menu.ShowAsContext();
            }
        }

        public void OpenDeleteRecent()
        {
            var message = new DeleteWindowStruct();
            message.MessageTitle = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_删除);
            message.MessageDelete = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_确认从列表中删除);
            message.MessageDeleteLocal = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_同时删除本地文件);
            DeleteWindow.OpenWindow(message,
                () =>
                {
                    var RecentOpened = JsonAssetManager.GetAssets<PrefabOpenedSetting>();
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    var RecentList = RecentOpened.List;
                    if (RecentList.Contains(guid))
                    {
                        RecentOpened.Remove(guid);
                        //PrefabRecentWindow.GetInstance().RefreshWindow();
                        Debug.Log("Delete Successfully");
                    }
                },
                () =>
                {
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.Refresh();
                    Debug.Log("Delete Successfully");
                });
        }
        private void OpenDeleteSettled()
        {
            var message = new DeleteWindowStruct();
            message.MessageTitle = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_删除);
            message.MessageDelete = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_确认从列表中删除);
            message.MessageDeleteLocal = EditorLocalization.GetLocalization(EditorLocalizationStorage.Def_同时删除本地文件);
            DeleteWindow.OpenWindow(message,
                () =>
                {
                    var widgetListSetting = JsonAssetManager.GetAssets<WidgetListSetting>();
                    if(widgetListSetting.Remove(AssetDatabase.AssetPathToGUID(path)))
                    {
                        Debug.Log("Delete Successfully");
                    }
                },
                () =>
                {
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.Refresh();
                    Debug.Log("Delete Successfully");
                });
        }
    }
}
#endif
