using Unity.Entities;
using Unity.Collections;
using PVZ.DOTS.Components;

namespace PVZ.DOTS.Systems
{
    /// <summary>
    /// 关卡管理系统 - 根据关卡配置管理波次和僵尸生成
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct LevelManagementSystem : ISystem
    {
        private bool _initialized;
        private int _currentWave;
        private float _lastWaveTime;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LevelConfigComponent>();
            _initialized = false;
            _currentWave = 0;
            _lastWaveTime = 0f;
        }

        public void OnUpdate(ref SystemState state)
        {
            // 检查游戏状态
            if (!SystemAPI.TryGetSingleton<GameStateComponent>(out var gameState))
                return;

            if (gameState.CurrentState != GameState.Playing)
                return;

            // 获取关卡配置
            if (!SystemAPI.TryGetSingleton<LevelConfigComponent>(out var levelConfig))
                return;

            if (!_initialized)
            {
                UnityEngine.Debug.Log($"LevelManagementSystem: 初始化关卡管理 LevelId={levelConfig.LevelId} Type={levelConfig.Type} " +
                    $"Difficulty={levelConfig.Difficulty} TotalWaves={levelConfig.TotalWaves}");
                _initialized = true;
            }

            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // 检查是否需要开始新波次
            // 简单实现：每30秒一波，或根据配置调整
            float waveInterval = 30f; // 可以从配置读取
            
            if (currentTime - _lastWaveTime >= waveInterval && _currentWave < levelConfig.TotalWaves)
            {
                _currentWave++;
                _lastWaveTime = currentTime;

                // 更新游戏状态中的波次
                if (SystemAPI.TryGetSingletonRW<GameStateComponent>(out var gameStateRW))
                {
                    gameStateRW.ValueRW.CurrentWave = _currentWave;
                    UnityEngine.Debug.Log($"LevelManagementSystem: 开始第 {_currentWave}/{levelConfig.TotalWaves} 波");
                }

                // 触发波次开始事件（可以在这里生成特定波次的僵尸）
                SpawnWaveZombies(ref state, _currentWave, levelConfig);
            }
        }

        private void SpawnWaveZombies(ref SystemState state, int waveNumber, LevelConfigComponent levelConfig)
        {
            // 查找关卡配置实体并读取波次配置
            var query = SystemAPI.QueryBuilder().WithAll<LevelConfigComponent>().Build();
            if (query.IsEmpty)
                return;

            var levelEntity = query.GetSingletonEntity();
            if (!state.EntityManager.HasBuffer<WaveConfigElement>(levelEntity))
            {
                query.Dispose();
                return;
            }

            var waveBuffer = state.EntityManager.GetBuffer<WaveConfigElement>(levelEntity);

            int zombieCount = 0;
            foreach (var wave in waveBuffer)
            {
                if (wave.WaveNumber == waveNumber)
                {
                    zombieCount += wave.Count;
                    // 这里可以根据wave.SpawnDelay设置延迟生成
                    // 实际生成逻辑由ZombieSpawnSystem处理
                }
            }

            UnityEngine.Debug.Log($"LevelManagementSystem: 第{waveNumber}波将生成 {zombieCount} 个僵尸");
            query.Dispose();
        }
    }
}
