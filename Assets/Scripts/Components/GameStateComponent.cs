using Unity.Entities;

namespace PVZ.DOTS.Components
{
    public enum GameState
    {
        Preparing,  // 准备阶段
        Playing,    // 游戏进行中
        Victory,    // 胜利
        Defeat      // 失败
    }

    public struct GameStateComponent : IComponentData
    {
        public GameState CurrentState;
        public float RemainingTime;     // 剩余时间（秒）
        public float TotalGameTime;     // 总游戏时间
        public int CurrentWave;         // 当前波次
        public int TotalWaves;          // 总波次数
        public int ZombiesKilled;       // 已击杀僵尸数
        public int ZombiesReachedEnd;   // 到达终点的僵尸数
    }
}
