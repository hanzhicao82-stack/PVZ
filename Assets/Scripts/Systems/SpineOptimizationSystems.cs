using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Systems
{
    /// <summary>
    /// Spine 对象池管理系统
    /// 复用 GameObject 实例以减少实例化和销毁开销
    /// </summary>
    public class SpineViewPoolManager : MonoBehaviour
    {
        private static SpineViewPoolManager _instance;
        public static SpineViewPoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SpineViewPoolManager");
                    _instance = go.AddComponent<SpineViewPoolManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // 对象池：Key = 预制体路径, Value = GameObject 队列
        private Dictionary<string, Queue<GameObject>> _poolDictionary = new Dictionary<string, Queue<GameObject>>();
        private Transform _poolRoot;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            _poolRoot = new GameObject("PooledObjects").transform;
            _poolRoot.SetParent(transform);
        }

        /// <summary>
        /// 从对象池获取或创建 GameObject
        /// </summary>
        public GameObject AcquireView(string prefabPath, GameObject prefab)
        {
            Queue<GameObject> pool;
            if (_poolDictionary.TryGetValue(prefabPath, out pool) && pool.Count > 0)
            {
                var pooledObject = pool.Dequeue();
                pooledObject.SetActive(true);
                return pooledObject;
            }

            // 池中无可用对象，创建新实例
            return Object.Instantiate(prefab);
        }

        /// <summary>
        /// 归还 GameObject 到对象池
        /// </summary>
        public void ReleaseView(string prefabPath, GameObject instance)
        {
            if (instance == null) return;

            instance.SetActive(false);
            instance.transform.SetParent(_poolRoot);

            if (!_poolDictionary.ContainsKey(prefabPath))
            {
                _poolDictionary[prefabPath] = new Queue<GameObject>();
            }

            _poolDictionary[prefabPath].Enqueue(instance);
        }

        /// <summary>
        /// 预热对象池
        /// </summary>
        public void WarmUp(string prefabPath, GameObject prefab, int count)
        {
            if (!_poolDictionary.ContainsKey(prefabPath))
            {
                _poolDictionary[prefabPath] = new Queue<GameObject>();
            }

            for (int i = 0; i < count; i++)
            {
                var instance = Object.Instantiate(prefab);
                instance.SetActive(false);
                instance.transform.SetParent(_poolRoot);
                _poolDictionary[prefabPath].Enqueue(instance);
            }
        }

        /// <summary>
        /// 清理指定路径的对象池
        /// </summary>
        public void ClearPool(string prefabPath)
        {
            if (_poolDictionary.TryGetValue(prefabPath, out var pool))
            {
                while (pool.Count > 0)
                {
                    var obj = pool.Dequeue();
                    if (obj != null) Destroy(obj);
                }
                _poolDictionary.Remove(prefabPath);
            }
        }

        /// <summary>
        /// 清理所有对象池
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in _poolDictionary.Values)
            {
                while (pool.Count > 0)
                {
                    var obj = pool.Dequeue();
                    if (obj != null) Destroy(obj);
                }
            }
            _poolDictionary.Clear();
        }

        void OnDestroy()
        {
            ClearAllPools();
        }
    }

    /// <summary>
    /// 视锥体剔除系统 - 检测哪些实体在相机视野内
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PresentationSystemGroup))]
    public partial class ViewCullingSystem : SystemBase
    {
        private Camera _mainCamera;
        private float _checkInterval = 0.1f; // 每 0.1 秒检查一次

        protected override void OnCreate()
        {
            RequireForUpdate<ViewCullingComponent>();
        }

        protected override void OnUpdate()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            var cameraPos = _mainCamera.transform.position;
            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);

            // 批量更新可见性
            foreach (var (transform, culling, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRW<ViewCullingComponent>>()
                .WithEntityAccess())
            {
                ref var cullingRef = ref culling.ValueRW;

                // 限制检查频率
                if (currentTime - cullingRef.LastCheckTime < _checkInterval)
                    continue;

                cullingRef.LastCheckTime = currentTime;

                // 简单的球形包围盒检测
                var bounds = new Bounds(transform.ValueRO.Position, Vector3.one * cullingRef.CullingRadius);
                cullingRef.IsVisible = GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);
            }
        }
    }

    /// <summary>
    /// LOD 系统 - 根据距离调整渲染质量
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PresentationSystemGroup))]
    public partial class LODSystem : SystemBase
    {
        private Camera _mainCamera;

        protected override void OnCreate()
        {
            RequireForUpdate<LODComponent>();
        }

        protected override void OnUpdate()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            float3 cameraPos = _mainCamera.transform.position;

            foreach (var (transform, lod, spineOpt) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRW<LODComponent>, RefRW<SpineOptimizationComponent>>())
            {
                ref var lodRef = ref lod.ValueRW;
                ref var optRef = ref spineOpt.ValueRW;

                // 计算距离平方（避免开方）
                float3 delta = transform.ValueRO.Position - cameraPos;
                lodRef.DistanceSquaredToCamera = math.lengthsq(delta);

                // 确定 LOD 级别
                int newLODLevel = 0;
                float distSq = lodRef.DistanceSquaredToCamera;
                float lod0Sq = lodRef.LODDistances.x * lodRef.LODDistances.x;
                float lod1Sq = lodRef.LODDistances.y * lodRef.LODDistances.y;
                float lod2Sq = lodRef.LODDistances.z * lodRef.LODDistances.z;

                if (distSq > lod2Sq)
                    newLODLevel = 3; // 极简
                else if (distSq > lod1Sq)
                    newLODLevel = 2; // 低质量
                else if (distSq > lod0Sq)
                    newLODLevel = 1; // 中等质量
                else
                    newLODLevel = 0; // 高质量

                // 根据 LOD 级别调整优化参数
                if (lodRef.CurrentLODLevel != newLODLevel)
                {
                    lodRef.CurrentLODLevel = newLODLevel;
                    ApplyLODSettings(ref optRef, newLODLevel);
                }
            }
        }

        private void ApplyLODSettings(ref SpineOptimizationComponent opt, int lodLevel)
        {
            switch (lodLevel)
            {
                case 0: // 高质量
                    opt.EnableAnimationUpdate = true;
                    opt.AnimationUpdateInterval = 1;
                    opt.EnableMeshUpdate = true;
                    break;
                case 1: // 中等质量
                    opt.EnableAnimationUpdate = true;
                    opt.AnimationUpdateInterval = 2; // 每2帧更新
                    opt.EnableMeshUpdate = true;
                    break;
                case 2: // 低质量
                    opt.EnableAnimationUpdate = true;
                    opt.AnimationUpdateInterval = 3; // 每3帧更新
                    opt.EnableMeshUpdate = false;
                    break;
                case 3: // 极简
                    opt.EnableAnimationUpdate = false;
                    opt.AnimationUpdateInterval = 0;
                    opt.EnableMeshUpdate = false;
                    break;
            }
        }
    }
}
