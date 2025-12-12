using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Framework
{
    /// <summary>
    /// 简易子弹视图对象池，按资源路径分组复用子弹 GameObject�?
    /// </summary>
    public static class ProjectileViewPool
    {
        private class PoolBucket
        {
            public readonly Queue<GameObject> Queue = new Queue<GameObject>();
            public GameObject Prefab;
        }

        private static readonly Dictionary<string, PoolBucket> Buckets = new Dictionary<string, PoolBucket>();
        private static GameObject _poolRoot;

        private static Transform PoolRoot
        {
            get
            {
                if (_poolRoot == null)
                {
                    _poolRoot = new GameObject("ProjectileViewPool");
                    Object.DontDestroyOnLoad(_poolRoot);
                }

                return _poolRoot.transform;
            }
        }

        /// <summary>
        /// 从池中获取一个子弹视图实例，不存在则加载并实例化�?
        /// </summary>
        public static GameObject Acquire(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath))
                return null;

            if (!Buckets.TryGetValue(prefabPath, out var bucket))
            {
                bucket = new PoolBucket
                {
                    Prefab = LoadPrefab(prefabPath)
                };
                Buckets[prefabPath] = bucket;
            }

            if (bucket.Prefab == null)
                return null;

            if (bucket.Queue.Count > 0)
            {
                var instance = bucket.Queue.Dequeue();
                if (instance != null)
                {
                    instance.transform.SetParent(null, false);
                    instance.SetActive(true);
                    return instance;
                }
            }

            var created = Object.Instantiate(bucket.Prefab);
            created.name = $"{bucket.Prefab.name}_Pooled";
            created.transform.SetParent(null, false);
            created.SetActive(true);
            return created;
        }

        /// <summary>
        /// 将实例归还到池中�?
        /// </summary>
        public static void Release(string prefabPath, GameObject instance)
        {
            if (instance == null)
                return;

            if (string.IsNullOrEmpty(prefabPath))
            {
                Object.Destroy(instance);
                return;
            }

            if (!Buckets.TryGetValue(prefabPath, out var bucket) || bucket.Prefab == null)
            {
                Object.Destroy(instance);
                return;
            }

            instance.SetActive(false);
            instance.transform.SetParent(PoolRoot, false);
            bucket.Queue.Enqueue(instance);
        }

        private static GameObject LoadPrefab(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath))
                return null;

            var prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab != null)
                return prefab;

#if UNITY_EDITOR
            string assetPath = prefabPath;
            if (!assetPath.StartsWith("Assets/"))
            {
                assetPath = $"Assets/{assetPath}";
            }

            if (!assetPath.EndsWith(".prefab"))
            {
                assetPath += ".prefab";
            }

            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
                return prefab;
#endif

            GameLogger.LogWarning("ProjectileViewPool", $"无法加载子弹预制�? {prefabPath}");
            return null;
        }
    }
}
