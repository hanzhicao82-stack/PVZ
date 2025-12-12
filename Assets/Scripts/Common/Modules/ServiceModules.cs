using UnityEngine;
using Framework;

namespace Common
{
    /// <summary>
    /// 音频服务模块
    /// </summary>
    public class AudioServiceModule : GameModuleBase
    {
        public override string ModuleId => "service.audio";
        public override string DisplayName => "音频服务";
        public override int Priority => 20;
        public override string[] Dependencies => System.Array.Empty<string>();

        private AudioService _audioService;

        protected override void OnInitialize()
        {
            _audioService = new AudioService();
            Context.RegisterService<IAudioService>(_audioService);
            
            // 从配置读取默认音�?
            float soundVolume = Context.GetConfigParameter("audio.sound.volume", 1f);
            float musicVolume = Context.GetConfigParameter("audio.music.volume", 0.8f);
            float masterVolume = Context.GetConfigParameter("audio.master.volume", 1f);
            
            _audioService.SetSoundVolume(soundVolume);
            _audioService.SetMusicVolume(musicVolume);
            _audioService.SetMasterVolume(masterVolume);
            
            UnityEngine.Debug.Log($"音频服务已初始化 - 音效:{soundVolume:F1} 音乐:{musicVolume:F1} 主音�?{masterVolume:F1}");
        }
    }

    /// <summary>
    /// 资源服务模块
    /// </summary>
    public class ResourceServiceModule : GameModuleBase
    {
        public override string ModuleId => "service.resource";
        public override string DisplayName => "资源服务";
        public override int Priority => 15;

        private ResourceService _resourceService;

        protected override void OnInitialize()
        {
            _resourceService = new ResourceService();
            Context.RegisterService<IResourceService>(_resourceService);
            
            UnityEngine.Debug.Log("资源服务已初始化");
        }

        protected override void OnShutdown()
        {
            _resourceService?.UnloadUnusedAssets();
        }
    }

    /// <summary>
    /// 存档服务模块
    /// </summary>
    public class SaveServiceModule : GameModuleBase
    {
        public override string ModuleId => "service.save";
        public override string DisplayName => "存档服务";
        public override int Priority => 25;

        private SaveService _saveService;

        protected override void OnInitialize()
        {
            _saveService = new SaveService();
            Context.RegisterService<ISaveService>(_saveService);
            
            UnityEngine.Debug.Log("存档服务已初始化");
        }

        protected override void OnShutdown()
        {
            _saveService?.SaveAll();
        }
    }

    /// <summary>
    /// 对象池服务模�?
    /// </summary>
    public class PoolServiceModule : GameModuleBase
    {
        public override string ModuleId => "service.pool";
        public override string DisplayName => "对象池服";
        public override int Priority => 30;
        public override string[] Dependencies => new[] { "service.resource" };

        private PoolService _poolService;

        protected override void OnInitialize()
        {
            _poolService = new PoolService();
            Context.RegisterService<IPoolService>(_poolService);
            
            // 预创建常用对象池
            bool autoCreatePools = Context.GetConfigParameter("pool.auto-create", true);
            if (autoCreatePools)
            {
                CreateCommonPools();
            }
            
            UnityEngine.Debug.Log("对象池服务已初始");
        }

        private void CreateCommonPools()
        {
            var resourceService = Context.GetService<IResourceService>();
            
            // 子弹对象�?
            var projectilePrefab = resourceService.Load<GameObject>("Prefabs/Projectiles/Pea");
            if (projectilePrefab != null)
            {
                _poolService.CreatePool("projectile.pea", projectilePrefab, 50, 200);
            }
            
            // 特效对象�?
            var hitEffectPrefab = resourceService.Load<GameObject>("Prefabs/Effects/HitEffect");
            if (hitEffectPrefab != null)
            {
                _poolService.CreatePool("effect.hit", hitEffectPrefab, 20, 100);
            }
            
            UnityEngine.Debug.Log("已创建常用对象池");
        }

        protected override void OnShutdown()
        {
            _poolService?.ClearAllPools();
        }
    }
}
