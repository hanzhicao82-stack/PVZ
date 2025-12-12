using Unity.Entities;
using Common;
using Framework;

namespace PVZ
{
    /// <summary>
    /// 关卡管理系统 - 根据关卡配置管理波次和僵尸生成，继承自通用 LevelManagementSystemBase
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class LevelManagementSystem : LevelManagementSystemBase
    {
        protected override void InitializeLevel()
        {
            // 获取关卡配置
            if (SystemAPI.TryGetSingleton<LevelConfigComponent>(out var levelConfig))
            {
                GameLogger.Log("LevelManagementSystem", 
                    $"初始化关卡管理：LevelId={levelConfig.LevelId} Type={levelConfig.Type} " +
                    $"Difficulty={levelConfig.Difficulty} TotalWaves={levelConfig.TotalWaves}");
            }
        }

        protected override void UpdateLevel(float currentTime)
        {
            // 获取关卡配置
            if (!SystemAPI.TryGetSingleton<LevelConfigComponent>(out var levelConfig))
                return;

            // 检查是否需要开始新波次
            if (ShouldStartNewWave(currentTime) && CurrentWave < levelConfig.TotalWaves)
            {
                CurrentWave++;
                LastWaveTime = currentTime;

                // 更新游戏状态中的波次
                if (SystemAPI.TryGetSingletonRW<GameStateComponent>(out var gameStateRW))
                {
                    gameStateRW.ValueRW.CurrentWave = CurrentWave;
                    GameLogger.Log("LevelManagementSystem", $"开始第 {CurrentWave}/{levelConfig.TotalWaves} 波");
                }

                // 触发波次开始事件
                SpawnWaveZombies(CurrentWave, levelConfig);
            }
        }

        protected override float GetWaveInterval()
        {
            // 可以从配置读取，这里使用默认值
            return 30f;
        }

        private void SpawnWaveZombies(int waveNumber, LevelConfigComponent levelConfig)
        {
            // 查找关卡配置实体并读取波次配置
            var query = SystemAPI.QueryBuilder().WithAll<LevelConfigComponent>().Build();
            if (query.IsEmpty)
                return;

            var levelEntity = query.GetSingletonEntity();
            if (!EntityManager.HasBuffer<WaveConfigElement>(levelEntity))
            {
                return;
            }

            var waveBuffer = EntityManager.GetBuffer<WaveConfigElement>(levelEntity);

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

            GameLogger.Log("LevelManagementSystem", $"第{waveNumber}波将生成 {zombieCount} 个僵尸");
        }
    }
}
