using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UT.RS;
using UT.Toolkit;
using Object = UnityEngine.Object;

namespace Game
{
    /// <summary>
    /// 给Text Mesh Pro附加AB包内的资源
    /// 支持Text组件直接切换样式材质(Material Preset)
    /// 目的：富文本标签里，动态设置的样式信息（字体、材质），如果不在Resources里，需要主动注册到TMP的管理器中
    /// </summary>
    public class TMPAttacher : MonoSingleton<TMPAttacher>
    {
        [Header("资源包名")]
        public string [] BundleName;

        protected TMP_SpriteAsset[] SpriteAssets;
        protected TMP_ColorGradient[] ColorGradients;
        protected Dictionary<string, Material> Materials = new();
        protected Dictionary<string, TMP_FontAsset> Fonts = new();

        protected int WaitCnt = 0;

        /// <summary>
        /// 查找可用的样式材质
        /// </summary>
        public Material FindMat(string matName)
        {
            if (!Materials.TryGetValue(matName, out Material mat))
            {
                Debug.LogError($"[TMPAttacher] Can`t find font material named:{matName}");
            }

            return mat;
        }

        public TMP_FontAsset FindFont(string fontName)
        {
            if (!Fonts.TryGetValue(fontName, out TMP_FontAsset font))
            {
                Debug.LogError($"[TMPAttacher] Can`t find font asset named:{fontName}");
            }

            return font;
        }

        /// <summary>
        /// 执行资源附加
        /// 实现思路：在字体AB包里，直接加载出全部相关资源，注册给TMP管理器
        /// </summary>
        public IEnumerator Execute()
        {
            WaitCnt = 4 * BundleName.Length;
            for (int i = 0; i < BundleName.Length; i++)
            {   //为了兼容配置不影响代码，所以这里不区分ab包内容的类型
                ResSys.LoadBundleAssets(BundleName[i], typeof(TMP_FontAsset), OnFontAssetLoaded);
                ResSys.LoadBundleAssets(BundleName[i], typeof(Material), OnMaterialLoaded);
                ResSys.LoadBundleAssets(BundleName[i], typeof(TMP_SpriteAsset), OnSpriteAssetLoaded);
                ResSys.LoadBundleAssets(BundleName[i], typeof(TMP_ColorGradient), OnColorLoaded);
            }
            yield return new WaitUntil(() => WaitCnt <= 0);
        }

        protected override void OverrideAwake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void RemoveAll()
        {
            // 取消所有注册的资源
            foreach (TMP_FontAsset fontAsset in Fonts.Values)
                MaterialReferenceManager.RemoveFontAsset(fontAsset.hashCode, fontAsset.materialHashCode);
            Fonts.Clear();

            foreach (Material mat in Materials.Values)
                MaterialReferenceManager.RemoveFontMaterial(CalculateStringHashcode(mat.name));
            Materials.Clear();

            if (null != SpriteAssets)
            {
                foreach (TMP_SpriteAsset asset in SpriteAssets)
                    MaterialReferenceManager.RemoveSpriteAsset(asset.hashCode);
            }

            if (null != ColorGradients)
            {
                foreach (TMP_ColorGradient asset in ColorGradients)
                    MaterialReferenceManager.RemoveColorGradientPreset(TMP_TextUtilities.GetSimpleHashCode(asset.name));
            }
        }

        void OnDestroy()
        {
            //RemoveAll();
        }

        protected virtual void OnFontAssetLoaded(Object[] assets)
        {
            --WaitCnt;
            if (null == assets) return; // 模拟模式不返回资源

            foreach (Object asset in assets)
            {
                TMP_FontAsset fontAsset = (TMP_FontAsset)asset;
                Fonts.Add(fontAsset.name, fontAsset);
                MaterialReferenceManager.AddFontAsset(fontAsset);
            }
        }

        protected virtual void OnMaterialLoaded(Object[] assets)
        {
            --WaitCnt;
            if (null == assets) return;
            foreach (Object asset in assets)
            {
                string matName = asset.name;
                // 字体文件内的材质（不是TMP Asset），不需要注册
                if (matName == "Font Material") continue;

                Material mat = (Material)asset;
                Materials.Add(matName, mat);
                int hashCode = CalculateStringHashcode(matName);
                MaterialReferenceManager.AddFontMaterial(hashCode, mat);
            }
        }

        protected virtual void OnSpriteAssetLoaded(Object[] assets)
        {
            --WaitCnt;
            if (null == assets) return;
            SpriteAssets = CollectionExt.Convert<Object, TMP_SpriteAsset>(assets);
            foreach (TMP_SpriteAsset asset in SpriteAssets)
            {
                MaterialReferenceManager.AddSpriteAsset(asset);
            }
        }

        protected virtual void OnColorLoaded(Object[] assets)
        {
            --WaitCnt;
            if (null == assets) return;
            ColorGradients = CollectionExt.Convert<Object, TMP_ColorGradient>(assets);
            foreach (TMP_ColorGradient asset in ColorGradients)
            {
                MaterialReferenceManager.AddColorGradientPreset(
                    TMP_TextUtilities.GetSimpleHashCode(asset.name), asset
                );
            }
        }

        //计算材质名字的hashCode，该算法与TextMeshPro所使用的算法一致
        static int CalculateStringHashcode(string str)
        {
            if (string.IsNullOrEmpty(str)) return 0;
            int length = str.Length;
            int hashCode = 0;
            for (int i = 0; i < length; i++)
            {
                char cha = str[i];
                hashCode = (hashCode << 5) + hashCode ^ cha;
            }

            return hashCode;
        }
    }
}