using UnityEngine;
using Unity.Mathematics;
using Framework;

namespace Samples
{
    /// <summary>
    /// 服务层使用示�?
    /// 展示如何使用各种服务
    /// </summary>
    public class ServiceExamples
    {
        private IModuleContext _context;
        private IAudioService _audioService;
        private IResourceService _resourceService;
        private ISaveService _saveService;
        private IPoolService _poolService;

        public void Initialize(IModuleContext context)
        {
            _context = context;
            _audioService = context.GetService<IAudioService>();
            _resourceService = context.GetService<IResourceService>();
            _saveService = context.GetService<ISaveService>();
            _poolService = context.GetService<IPoolService>();
        }

        // ==================== 音频服务示例 ====================

        /// <summary>
        /// 播放音效示例
        /// </summary>
        public void PlaySoundExample()
        {
            // 2D音效
            _audioService.PlaySound("plant_shoot", volume: 0.8f);
            
            // 3D音效（带位置�?
            float3 zombiePosition = new float3(10, 0, 5);
            _audioService.PlaySound("zombie_groan", volume: 1f, position: zombiePosition);
        }

        /// <summary>
        /// 音乐控制示例
        /// </summary>
        public void MusicControlExample()
        {
            // 播放背景音乐
            _audioService.PlayMusic("grasswalk", fadeIn: true, fadeDuration: 2f);
            
            // 暂停音乐
            _audioService.PauseMusic(true);
            
            // 恢复音乐
            _audioService.PauseMusic(false);
            
            // 停止音乐
            _audioService.StopMusic(fadeOut: true, fadeDuration: 1f);
        }

        /// <summary>
        /// 音量设置示例
        /// </summary>
        public void VolumeControlExample()
        {
            // 设置音效音量
            _audioService.SetSoundVolume(0.7f);
            
            // 设置音乐音量
            _audioService.SetMusicVolume(0.5f);
            
            // 设置主音�?
            _audioService.SetMasterVolume(0.9f);
        }

        // ==================== 资源服务示例 ====================

        /// <summary>
        /// 同步加载资源示例
        /// </summary>
        public void LoadResourceExample()
        {
            // 加载预制�?
            var zombiePrefab = _resourceService.Load<GameObject>("Prefabs/Zombies/NormalZombie");
            
            // 加载材质
            var material = _resourceService.Load<Material>("Materials/ZombieSkin");
            
            // 加载音频
            var audioClip = _resourceService.Load<AudioClip>("Audio/zombie_death");
        }

        /// <summary>
        /// 异步加载资源示例
        /// </summary>
        public void LoadResourceAsyncExample()
        {
            _resourceService.LoadAsync<GameObject>("Prefabs/Plants/Sunflower", (prefab) =>
            {
                if (prefab != null)
                {
                    UnityEngine.Debug.Log("向日葵预制体加载完成");
                    // 实例化预制体
                    var instance = Object.Instantiate(prefab);
                }
            });
        }

        /// <summary>
        /// 实例化预制体示例
        /// </summary>
        public void InstantiatePrefabExample()
        {
            float3 position = new float3(5, 0, 3);
            quaternion rotation = quaternion.identity;
            
            var zombie = _resourceService.Instantiate("Prefabs/Zombies/NormalZombie", position, rotation);
            if (zombie != null)
            {
                UnityEngine.Debug.Log("僵尸实例化成功");
            }
        }

        /// <summary>
        /// 预加载资源示�?
        /// </summary>
        public void PreloadResourcesExample()
        {
            string[] assetsToLoad = new[]
            {
                "Prefabs/Zombies/NormalZombie",
                "Prefabs/Zombies/ConeheadZombie",
                "Prefabs/Plants/Peashooter",
                "Prefabs/Plants/Sunflower"
            };

            _resourceService.PreloadAssets(assetsToLoad, () =>
            {
                UnityEngine.Debug.Log("所有资源预加载完成");
            });
        }

        // ==================== 存档服务示例 ====================

        /// <summary>
        /// 保存玩家数据示例
        /// </summary>
        public void SavePlayerDataExample()
        {
            var playerData = new PlayerData
            {
                PlayerName = "Player1",
                Level = 5,
                TotalScore = 10000,
                UnlockedPlants = new[] { "Peashooter", "Sunflower", "WallNut" }
            };

            _saveService.Save("PlayerData", playerData);
            _saveService.SaveAll(); // 确保保存到磁�?
            
            UnityEngine.Debug.Log("玩家数据已保存");
        }

        /// <summary>
        /// 加载玩家数据示例
        /// </summary>
        public void LoadPlayerDataExample()
        {
            var defaultData = new PlayerData { PlayerName = "NewPlayer", Level = 1 };
            var playerData = _saveService.Load("PlayerData", defaultData);
            
            UnityEngine.Debug.Log($"玩家: {playerData.PlayerName}, 等级: {playerData.Level}");
        }

        /// <summary>
        /// 保存游戏设置示例
        /// </summary>
        public void SaveSettingsExample()
        {
            var settings = new GameSettings
            {
                SoundVolume = 0.8f,
                MusicVolume = 0.6f,
                Difficulty = "Normal",
                Language = "zh-CN"
            };

            _saveService.Save("GameSettings", settings);
        }

        // ==================== 对象池服务示�?====================

        /// <summary>
        /// 创建对象池示�?
        /// </summary>
        public void CreatePoolExample()
        {
            var peaPrefab = _resourceService.Load<GameObject>("Prefabs/Projectiles/Pea");
            
            // 创建豌豆子弹对象�?
            _poolService.CreatePool(
                poolId: "projectile.pea",
                prefab: peaPrefab,
                initialSize: 50,    // 初始创建50�?
                maxSize: 200        // 最�?00�?
            );

            // 预热对象池（提前创建对象�?
            _poolService.WarmPool("projectile.pea", 30);
        }

        /// <summary>
        /// 从对象池获取对象示例
        /// </summary>
        public void GetFromPoolExample()
        {
            // 从池中获取豌豆子�?
            var pea = _poolService.Get("projectile.pea");
            if (pea != null)
            {
                pea.transform.position = new Vector3(5, 0, 3);
                // 设置子弹属�?..
            }
        }

        /// <summary>
        /// 归还对象到池示例
        /// </summary>
        public void ReturnToPoolExample(GameObject pea)
        {
            // 子弹击中目标后，归还到池
            _poolService.Return("projectile.pea", pea);
        }

        // ==================== 服务配合事件总线示例 ====================

        /// <summary>
        /// 服务层响应事件示�?
        /// </summary>
        public class ServiceEventHandler
        {
            private IAudioService _audioService;
            private IEventBus _eventBus;

            public void Initialize(IModuleContext context)
            {
                _audioService = context.GetService<IAudioService>();
                _eventBus = context.GetService<IEventBus>();

                // 订阅事件
                _eventBus.Subscribe<ZombieDeathEvent>(OnZombieDeath);
                _eventBus.Subscribe<PlantPlacedEvent>(OnPlantPlaced);
                _eventBus.Subscribe<SunCollectedEvent>(OnSunCollected);
            }

            private void OnZombieDeath(ZombieDeathEvent evt)
            {
                // 播放僵尸死亡音效
                _audioService.PlaySound("zombie_death", volume: 1f, position: evt.Position);
            }

            private void OnPlantPlaced(PlantPlacedEvent evt)
            {
                // 播放种植音效
                _audioService.PlaySound("plant_placed", volume: 0.8f);
            }

            private void OnSunCollected(SunCollectedEvent evt)
            {
                // 播放阳光收集音效
                _audioService.PlaySound("sun_collect", volume: 0.7f, position: evt.CollectionPosition);
            }
        }

        // ==================== 完整使用场景示例 ====================

        /// <summary>
        /// 僵尸死亡完整处理流程
        /// </summary>
        public void ZombieDeathCompleteExample(float3 zombiePosition)
        {
            // 1. 播放死亡音效
            _audioService.PlaySound("zombie_death", volume: 1f, position: zombiePosition);
            
            // 2. 从对象池获取死亡特效
            var deathEffect = _poolService.Get("effect.zombie_death");
            if (deathEffect != null)
            {
                deathEffect.transform.position = new Vector3(zombiePosition.x, zombiePosition.y, zombiePosition.z);
                // 3秒后归还到池
                // TODO: 使用协程或定时器归还
            }
            
            // 3. 更新统计数据
            var stats = _saveService.Load<GameStatistics>("Statistics", new GameStatistics());
            stats.TotalZombiesKilled++;
            _saveService.Save("Statistics", stats);
            
            // 4. 发布事件通知其他系统
            var eventBus = _context.GetService<IEventBus>();
            eventBus.Publish(new ZombieDeathEvent
            {
                ZombieType = "Normal",
                Position = zombiePosition,
                ScoreValue = 100,
                WasKilledByPlant = true
            });
        }

        /// <summary>
        /// 关卡开始完整流�?
        /// </summary>
        public void LevelStartCompleteExample(int levelId)
        {
            // 1. 加载关卡资源
            string[] levelAssets = new[]
            {
                $"Prefabs/Levels/Level{levelId}",
                "Prefabs/Zombies/NormalZombie",
                "Prefabs/Plants/Peashooter"
            };

            _resourceService.PreloadAssets(levelAssets, () =>
            {
                // 2. 播放关卡音乐
                _audioService.PlayMusic($"level_{levelId}_music", fadeIn: true);
                
                // 3. 加载玩家数据
                var playerData = _saveService.Load<PlayerData>("PlayerData");
                
                // 4. 发布关卡开始事�?
                var eventBus = _context.GetService<IEventBus>();
                eventBus.Publish(new GameStartedEvent
                {
                    LevelId = levelId,
                    LevelName = $"关卡 {levelId}"
                });
                
                UnityEngine.Debug.Log("关卡初始化完成");
            });
        }

        // ==================== 数据结构定义 ====================

        [System.Serializable]
        public class PlayerData
        {
            public string PlayerName;
            public int Level;
            public int TotalScore;
            public string[] UnlockedPlants;
        }

        [System.Serializable]
        public class GameSettings
        {
            public float SoundVolume;
            public float MusicVolume;
            public string Difficulty;
            public string Language;
        }

        [System.Serializable]
        public class GameStatistics
        {
            public int TotalZombiesKilled;
            public int TotalPlantsPlaced;
            public int TotalSunCollected;
            public float TotalPlayTime;
        }
    }
}

