using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.TowerDefense
{
    /// <summary>
    /// 子弹视图预制体组�?- 指示需要使用的子弹视图资源路径�?
    /// </summary>
    public struct ProjectileViewPrefabComponent : IComponentData
    {
        public FixedString128Bytes PrefabPath;
    }

    /// <summary>
    /// 子弹视图实例组件 - 通过对象池管理的 GameObject 实例包装�?
    /// </summary>
    public class ProjectileViewComponent : ICleanupComponentData
    {
        public GameObject Instance;
        public string PrefabPath;
    }
}
