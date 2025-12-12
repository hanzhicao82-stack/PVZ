using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace Framework
{
    /// <summary>
    /// 音频服务实现 - 使用Unity AudioSource
    /// </summary>
    public class AudioService : IAudioService
    {
        private readonly Dictionary<string, AudioClip> _soundClips = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, AudioSource> _soundSources = new Dictionary<string, AudioSource>();
        
        private AudioSource _musicSource;
        private GameObject _audioRoot;

        private float _soundVolume = 1f;
        private float _musicVolume = 1f;
        private float _masterVolume = 1f;

        public AudioService()
        {
            InitializeAudioRoot();
        }

        private void InitializeAudioRoot()
        {
            _audioRoot = new GameObject("AudioService");
            Object.DontDestroyOnLoad(_audioRoot);

            // 创建音乐�?
            _musicSource = _audioRoot.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
        }

        public void PlaySound(string soundId, float volume = 1f, float3? position = null)
        {
            var clip = LoadSoundClip(soundId);
            if (clip == null)
            {
                UnityEngine.Debug.LogWarning($"音效不存�? {soundId}");
                return;
            }

            if (position.HasValue)
            {
                // 3D音效
                AudioSource.PlayClipAtPoint(clip, new Vector3(position.Value.x, position.Value.y, position.Value.z), 
                    volume * _soundVolume * _masterVolume);
            }
            else
            {
                // 2D音效
                var source = GetOrCreateSoundSource(soundId);
                source.clip = clip;
                source.volume = volume * _soundVolume * _masterVolume;
                source.Play();
            }
        }

        public void StopSound(string soundId)
        {
            if (_soundSources.TryGetValue(soundId, out var source))
            {
                source.Stop();
            }
        }

        public void PlayMusic(string musicId, bool fadeIn = true, float fadeDuration = 1f)
        {
            var clip = LoadSoundClip(musicId);
            if (clip == null)
            {
                UnityEngine.Debug.LogWarning($"音乐不存�? {musicId}");
                return;
            }

            if (fadeIn && _musicSource.isPlaying)
            {
                // TODO: 实现淡入淡出
                _musicSource.Stop();
            }

            _musicSource.clip = clip;
            _musicSource.volume = _musicVolume * _masterVolume;
            _musicSource.Play();
        }

        public void StopMusic(bool fadeOut = true, float fadeDuration = 1f)
        {
            if (fadeOut)
            {
                // TODO: 实现淡出
            }
            _musicSource.Stop();
        }

        public void PauseMusic(bool pause)
        {
            if (pause)
                _musicSource.Pause();
            else
                _musicSource.UnPause();
        }

        public void SetSoundVolume(float volume)
        {
            _soundVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            _musicSource.volume = _musicVolume * _masterVolume;
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
        }

        private AudioClip LoadSoundClip(string soundId)
        {
            if (_soundClips.TryGetValue(soundId, out var clip))
                return clip;

            // 从Resources加载
            clip = Resources.Load<AudioClip>($"Audio/{soundId}");
            if (clip != null)
            {
                _soundClips[soundId] = clip;
            }

            return clip;
        }

        private AudioSource GetOrCreateSoundSource(string soundId)
        {
            if (_soundSources.TryGetValue(soundId, out var source))
                return source;

            var sourceObj = new GameObject($"Sound_{soundId}");
            sourceObj.transform.SetParent(_audioRoot.transform);
            source = sourceObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            
            _soundSources[soundId] = source;
            return source;
        }

        private void UpdateAllVolumes()
        {
            _musicSource.volume = _musicVolume * _masterVolume;
            
            foreach (var source in _soundSources.Values)
            {
                if (source.isPlaying)
                {
                    source.volume = _soundVolume * _masterVolume;
                }
            }
        }
    }

    /// <summary>
    /// 资源加载服务实现 - 使用Unity Resources
    /// </summary>
    public class ResourceService : IResourceService
    {
        private readonly Dictionary<string, Object> _cachedResources = new Dictionary<string, Object>();

        public T Load<T>(string path) where T : Object
        {
            if (_cachedResources.TryGetValue(path, out var cached))
            {
                return cached as T;
            }

            var resource = Resources.Load<T>(path);
            if (resource != null)
            {
                _cachedResources[path] = resource;
            }
            else
            {
                UnityEngine.Debug.LogWarning($"资源加载失败: {path}");
            }

            return resource;
        }

        public void LoadAsync<T>(string path, System.Action<T> onComplete) where T : Object
        {
            var request = Resources.LoadAsync<T>(path);
            request.completed += (op) =>
            {
                var resource = request.asset as T;
                if (resource != null)
                {
                    _cachedResources[path] = resource;
                }
                onComplete?.Invoke(resource);
            };
        }

        public GameObject Instantiate(string path, float3 position, quaternion rotation)
        {
            var prefab = Load<GameObject>(path);
            if (prefab == null)
                return null;

            return Object.Instantiate(prefab, 
                new Vector3(position.x, position.y, position.z),
                new Quaternion(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w));
        }

        public void Unload(string path)
        {
            if (_cachedResources.Remove(path, out var resource))
            {
                if (resource != null)
                {
                    Resources.UnloadAsset(resource);
                }
            }
        }

        public void UnloadUnusedAssets()
        {
            Resources.UnloadUnusedAssets();
            _cachedResources.Clear();
        }

        public void PreloadAssets(string[] paths, System.Action onComplete = null)
        {
            int loadedCount = 0;
            int totalCount = paths.Length;

            foreach (var path in paths)
            {
                LoadAsync<Object>(path, (resource) =>
                {
                    loadedCount++;
                    if (loadedCount >= totalCount)
                    {
                        onComplete?.Invoke();
                    }
                });
            }
        }
    }

    /// <summary>
    /// 存档服务实现 - 使用Unity PlayerPrefs
    /// </summary>
    public class SaveService : ISaveService
    {
        public void Save<T>(string key, T data)
        {
            var json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(key, json);
        }

        public T Load<T>(string key, T defaultValue = default)
        {
            if (!PlayerPrefs.HasKey(key))
                return defaultValue;

            var json = PlayerPrefs.GetString(key);
            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch
            {
                UnityEngine.Debug.LogWarning($"加载数据失败: {key}");
                return defaultValue;
            }
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }

        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public void ClearAll()
        {
            PlayerPrefs.DeleteAll();
        }

        public void SaveAll()
        {
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// 简单的对象池实�?
    /// </summary>
    public class PoolService : IPoolService
    {
        private class Pool
        {
            public GameObject Prefab;
            public Queue<GameObject> Available = new Queue<GameObject>();
            public List<GameObject> InUse = new List<GameObject>();
            public int MaxSize;
            public GameObject Container;
        }

        private readonly Dictionary<string, Pool> _pools = new Dictionary<string, Pool>();
        private GameObject _poolRoot;

        public PoolService()
        {
            _poolRoot = new GameObject("ObjectPools");
            Object.DontDestroyOnLoad(_poolRoot);
        }

        public void CreatePool(string poolId, GameObject prefab, int initialSize = 10, int maxSize = 100)
        {
            if (_pools.ContainsKey(poolId))
            {
                UnityEngine.Debug.LogWarning($"对象池已存在: {poolId}");
                return;
            }

            var container = new GameObject($"Pool_{poolId}");
            container.transform.SetParent(_poolRoot.transform);

            var pool = new Pool
            {
                Prefab = prefab,
                MaxSize = maxSize,
                Container = container
            };

            _pools[poolId] = pool;

            // 预创建对�?
            WarmPool(poolId, initialSize);
        }

        public void WarmPool(string poolId, int count)
        {
            if (!_pools.TryGetValue(poolId, out var pool))
                return;

            for (int i = 0; i < count; i++)
            {
                var obj = Object.Instantiate(pool.Prefab, pool.Container.transform);
                obj.SetActive(false);
                pool.Available.Enqueue(obj);
            }
        }

        public GameObject Get(string poolId)
        {
            if (!_pools.TryGetValue(poolId, out var pool))
            {
                UnityEngine.Debug.LogError($"对象池不存在: {poolId}");
                return null;
            }

            GameObject obj;

            if (pool.Available.Count > 0)
            {
                obj = pool.Available.Dequeue();
            }
            else if (pool.InUse.Count < pool.MaxSize)
            {
                obj = Object.Instantiate(pool.Prefab, pool.Container.transform);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"对象池已�? {poolId}");
                return null;
            }

            obj.SetActive(true);
            pool.InUse.Add(obj);
            return obj;
        }

        public void Return(string poolId, GameObject obj)
        {
            if (!_pools.TryGetValue(poolId, out var pool))
                return;

            if (!pool.InUse.Remove(obj))
                return;

            obj.SetActive(false);
            obj.transform.SetParent(pool.Container.transform);
            pool.Available.Enqueue(obj);
        }

        public void ClearPool(string poolId)
        {
            if (!_pools.TryGetValue(poolId, out var pool))
                return;

            foreach (var obj in pool.Available)
            {
                if (obj != null)
                    Object.Destroy(obj);
            }
            pool.Available.Clear();

            foreach (var obj in pool.InUse)
            {
                if (obj != null)
                    Object.Destroy(obj);
            }
            pool.InUse.Clear();
        }

        public void ClearAllPools()
        {
            foreach (var poolId in _pools.Keys)
            {
                ClearPool(poolId);
            }
            _pools.Clear();
        }
    }
}
