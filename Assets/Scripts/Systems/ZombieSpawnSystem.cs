using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using PVZ.DOTS.Components;
using PVZ.DOTS.Utils;

namespace PVZ.DOTS.Systems
{
    /// <summary>
    /// 僵尸生成系统 - 定期生成僵尸
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ZombieSpawnSystem : ISystem
    {
        private float _lastSpawnTime;
        private Random _random;
        private bool _initialized;
        private float _startDelay;
        private int _rowCount;
        private int _columnCount;
        private float _spawnInterval;
        private float _cellWidth;
        private float _cellHeight;
        private float _zombieMovementSpeed;
        private float _zombieAttackDamage;
        private float _zombieAttackInterval;
        private float _zombieHealth;
        private FixedString128Bytes _zombieProjectilePrefabPath;

        public void OnCreate(ref SystemState state)
        {
            _lastSpawnTime = 0f;
            _random = new Random((uint)System.DateTime.Now.Ticks);
            _initialized = false; // 延迟到第一次 OnUpdate 从配置初始化
        }

        public void OnUpdate(ref SystemState state)
        {
            // 检查游戏状态，只在Playing时生成僵尸
            if (SystemAPI.TryGetSingleton<GameStateComponent>(out var gameState))
            {
                if (gameState.CurrentState != GameState.Playing)
                    return;
            }

            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // 初始化配置（优先从关卡配置读取，如果没有则使用游戏全局配置）
            if (!_initialized)
            {
                bool configLoaded = false;
                
                // 优先尝试从关卡配置读取
                if (SystemAPI.TryGetSingleton<LevelConfigComponent>(out var levelConfig))
                {
                    _spawnInterval = levelConfig.ZombieSpawnInterval;
                    _startDelay = levelConfig.ZombieSpawnStartDelay;
                    _rowCount = levelConfig.RowCount;  // 使用关卡的行数
                    _columnCount = levelConfig.ColumnCount; // 使用关卡的列数
                    _cellWidth = levelConfig.CellWidth;
                    _cellHeight = levelConfig.CellHeight;
                    
                    // 使用默认僵尸属性
                    _zombieMovementSpeed = 1.0f;
                    _zombieAttackDamage = 10.0f;
                    _zombieAttackInterval = 1.0f;
                    _zombieHealth = 100.0f;
                    
                    configLoaded = true;
                    GameLogger.Log("ZombieSpawnSystem", $"从关卡配置初始化。生成间隔={_spawnInterval}s, 延迟={_startDelay}s, 行数={_rowCount}, 列数={_columnCount}, 格子大小=({_cellWidth}, {_cellHeight})");
                }
                // 如果没有关卡配置，尝试使用游戏全局配置
                else if (SystemAPI.TryGetSingleton<PVZ.DOTS.Components.GameConfigComponent>(out var config))
                {
                    _spawnInterval = config.ZombieSpawnInterval;
                    _startDelay = config.ZombieSpawnStartDelay;
                    _rowCount = config.LaneCount;
                    _columnCount = 9; // 默认9列
                    _cellWidth = 1.0f;
                    _cellHeight = 1.0f;
                    
                    // 从配置中读取默认僵尸属性，如果为0则使用硬编码默认值
                    _zombieMovementSpeed = config.ZombieMovementSpeed > 0 ? config.ZombieMovementSpeed : 1.0f;
                    _zombieAttackDamage = config.ZombieAttackDamage > 0 ? config.ZombieAttackDamage : 10.0f;
                    _zombieAttackInterval = config.ZombieAttackInterval > 0 ? config.ZombieAttackInterval : 1.0f;
                    _zombieHealth = config.ZombieHealth > 0 ? config.ZombieHealth : 100.0f;
                    
                    configLoaded = true;
                    GameLogger.Log("ZombieSpawnSystem", $"从全局配置初始化。生成间隔={_spawnInterval}s, 延迟={_startDelay}s, 行数={_rowCount}");
                }
                
                if (configLoaded)
                {
                    ResolveZombieProjectilePath(ref state);
                    _initialized = true;
                }
                else
                {
                    // 未找到配置则直接返回，下一帧再试
                    return;
                }
            }

            // 起始延迟
            if (currentTime < _startDelay)
                return;

            // 检查是否到达生成时间
            if (currentTime - _lastSpawnTime < _spawnInterval)
                return;

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            // 随机选择一行，僵尸从地图最右侧外部生成
            int spawnRow = _random.NextInt(0, _rowCount);
            int spawnColumn = _columnCount; // 超出地图最右侧（地图外）
            
            // 计算世界坐标（左下角为原点0,0,0）
            float worldX = spawnColumn * _cellWidth;
            float worldZ = spawnRow * _cellWidth;

            // 创建僵尸实体
            Entity zombieEntity = ecb.CreateEntity();
            
            ecb.AddComponent(zombieEntity, new ZombieComponent
            {
                Type = ZombieType.Normal,
                MovementSpeed = _zombieMovementSpeed,
                AttackDamage = _zombieAttackDamage,
                AttackInterval = _zombieAttackInterval,
                LastAttackTime = 0f,
                Lane = spawnRow,
                ProjectilePrefabPath = _zombieProjectilePrefabPath
            });

            ecb.AddComponent(zombieEntity, new HealthComponent
            {
                CurrentHealth = _zombieHealth,
                MaxHealth = _zombieHealth,
                IsDead = false
            });

            ecb.AddComponent(zombieEntity, new GridPositionComponent
            {
                Row = spawnRow,
                Column = spawnColumn,
                WorldPosition = new float3(worldX, 0, worldZ)
            });

            ecb.AddComponent(zombieEntity, LocalTransform.FromPosition(new float3(worldX, 0, worldZ)));

            _lastSpawnTime = currentTime;

            GameLogger.Log("ZombieSpawnSystem", $"生成僵尸 Row={spawnRow} Column={spawnColumn} WorldPos=({worldX:F2}, 0, {worldZ:F2})");

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void ResolveZombieProjectilePath(ref SystemState state)
        {
            if (SystemAPI.TryGetSingletonBuffer<ZombieConfigElement>(out var zombieConfigs))
            {
                foreach (var cfg in zombieConfigs)
                {
                    if (cfg.Type == ZombieType.Normal)
                    {
                        _zombieProjectilePrefabPath = cfg.ProjectilePrefabPath;
                        break;
                    }
                }
            }
        }
    }
}
