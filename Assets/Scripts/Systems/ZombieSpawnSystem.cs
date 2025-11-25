using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using PVZ.DOTS.Components;
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
        private int _laneCount;
        private float _spawnInterval;
        private float _spawnX;
        private float _laneZSpacing;
        private float _laneZOffset;
        private float _zombieMovementSpeed;
        private float _zombieAttackDamage;
        private float _zombieAttackInterval;
        private float _zombieHealth;

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

            // 初始化配置（等待配置组件出现）
            if (!_initialized)
            {
                if (SystemAPI.TryGetSingleton<PVZ.DOTS.Components.GameConfigComponent>(out var config))
                {
                    _spawnInterval = config.ZombieSpawnInterval;
                    _startDelay = config.ZombieSpawnStartDelay;
                    _laneCount = config.LaneCount;
                    _spawnX = config.SpawnX;
                    _laneZSpacing = config.LaneZSpacing;
                    _laneZOffset = config.LaneZOffset;
                    
                    // 从配置中读取默认僵尸属性，如果为0则使用硬编码默认值
                    _zombieMovementSpeed = config.ZombieMovementSpeed > 0 ? config.ZombieMovementSpeed : 1.0f;
                    _zombieAttackDamage = config.ZombieAttackDamage > 0 ? config.ZombieAttackDamage : 10.0f;
                    _zombieAttackInterval = config.ZombieAttackInterval > 0 ? config.ZombieAttackInterval : 1.0f;
                    _zombieHealth = config.ZombieHealth > 0 ? config.ZombieHealth : 100.0f;
                    
                    _initialized = true;
                    UnityEngine.Debug.Log($"ZombieSpawnSystem: 初始化完成。生成间隔={_spawnInterval}s, 延迟={_startDelay}s, 行数={_laneCount}, 僵尸速度={_zombieMovementSpeed}");
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

            // 随机选择一行
            int lane = _random.NextInt(0, _laneCount);
            float spawnZ = lane * _laneZSpacing + _laneZOffset; // 根据行号计算Z坐标

            // 创建僵尸实体
            Entity zombieEntity = ecb.CreateEntity();
            
            ecb.AddComponent(zombieEntity, new ZombieComponent
            {
                Type = ZombieType.Normal,
                MovementSpeed = _zombieMovementSpeed,
                AttackDamage = _zombieAttackDamage,
                AttackInterval = _zombieAttackInterval,
                LastAttackTime = 0f,
                Lane = lane
            });

            ecb.AddComponent(zombieEntity, new HealthComponent
            {
                CurrentHealth = _zombieHealth,
                MaxHealth = _zombieHealth,
                IsDead = false
            });

            ecb.AddComponent(zombieEntity, new GridPositionComponent
            {
                Row = lane,
                Column = 9,
                WorldPosition = new float3(_spawnX, 0, spawnZ)
            });

            ecb.AddComponent(zombieEntity, LocalTransform.FromPosition(new float3(_spawnX, 0, spawnZ)));

            _lastSpawnTime = currentTime;

            UnityEngine.Debug.Log($"ZombieSpawnSystem: 生成僵尸 Lane={lane} Position=({_spawnX}, 0, {spawnZ})");

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
