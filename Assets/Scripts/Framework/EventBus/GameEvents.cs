using Unity.Entities;
using Unity.Mathematics;

namespace Framework
{
    // ==================== 游戏生命周期事件 ====================

    /// <summary>
    /// 游戏开始事�?
    /// </summary>
    public class GameStartedEvent : GameEventBase
    {
        public int LevelId;
        public string LevelName;
    }

    /// <summary>
    /// 游戏暂停事件
    /// </summary>
    public class GamePausedEvent : GameEventBase
    {
        public bool IsPaused;
    }

    /// <summary>
    /// 游戏结束事件
    /// </summary>
    public class GameEndedEvent : GameEventBase
    {
        public bool IsVictory;
        public int FinalScore;
        public float PlayTime;
    }

    /// <summary>
    /// 关卡完成事件
    /// </summary>
    public class LevelCompletedEvent : GameEventBase
    {
        public int LevelId;
        public int TotalScore;
        public int StarsEarned;
    }

    // ==================== 波次相关事件 ====================

    /// <summary>
    /// 波次开始事�?
    /// </summary>
    public class WaveStartedEvent : GameEventBase
    {
        public int WaveNumber;
        public int TotalWaves;
        public int ZombieCount;
    }

    /// <summary>
    /// 波次完成事件
    /// </summary>
    public class WaveCompletedEvent : GameEventBase
    {
        public int WaveNumber;
        public int TotalWaves;
        public bool IsFinalWave;
    }

    /// <summary>
    /// 大波僵尸来袭事件
    /// </summary>
    public class HugeWaveIncomingEvent : GameEventBase
    {
        public int WaveNumber;
        public float PrepareTime; // 准备时间（秒�?
    }

    // ==================== 僵尸相关事件 ====================

    /// <summary>
    /// 僵尸生成事件
    /// </summary>
    public class ZombieSpawnedEvent : GameEventBase
    {
        public Entity ZombieEntity;
        public string ZombieType;
        public int Row;
        public float3 Position;
    }

    /// <summary>
    /// 僵尸受伤事件
    /// </summary>
    public class ZombieDamagedEvent : GameEventBase
    {
        public Entity ZombieEntity;
        public Entity AttackerEntity;
        public float Damage;
        public float RemainingHealth;
        public float3 Position;
    }

    /// <summary>
    /// 僵尸死亡事件
    /// </summary>
    public class ZombieDeathEvent : GameEventBase
    {
        public Entity ZombieEntity;
        public string ZombieType;
        public float3 Position;
        public int ScoreValue;
        public bool WasKilledByPlant; // 是否被植物击杀（区分吃脑后死亡�?
    }

    /// <summary>
    /// 僵尸到达终点事件
    /// </summary>
    public class ZombieReachedEndEvent : GameEventBase
    {
        public Entity ZombieEntity;
        public int Row;
        public bool GameOverTriggered;
    }

    /// <summary>
    /// 僵尸进入攻击范围事件
    /// </summary>
    public class ZombieEnterAttackRangeEvent : GameEventBase
    {
        public Entity ZombieEntity;
        public Entity TargetPlantEntity;
        public float3 Position;
    }

    // ==================== 植物相关事件 ====================

    /// <summary>
    /// 植物种植事件
    /// </summary>
    public class PlantPlacedEvent : GameEventBase
    {
        public Entity PlantEntity;
        public string PlantType;
        public int Row;
        public int Column;
        public int SunCost;
    }

    /// <summary>
    /// 植物移除事件
    /// </summary>
    public class PlantRemovedEvent : GameEventBase
    {
        public Entity PlantEntity;
        public string PlantType;
        public int Row;
        public int Column;
        public bool WasDestroyed; // true=被僵尸摧�? false=主动铲除
    }

    /// <summary>
    /// 植物攻击事件
    /// </summary>
    public class PlantAttackEvent : GameEventBase
    {
        public Entity PlantEntity;
        public string PlantType;
        public Entity TargetEntity;
        public float3 AttackPosition;
    }

    /// <summary>
    /// 植物受伤事件
    /// </summary>
    public class PlantDamagedEvent : GameEventBase
    {
        public Entity PlantEntity;
        public Entity AttackerEntity;
        public float Damage;
        public float RemainingHealth;
    }

    /// <summary>
    /// 植物死亡事件
    /// </summary>
    public class PlantDeathEvent : GameEventBase
    {
        public Entity PlantEntity;
        public string PlantType;
        public int Row;
        public int Column;
    }

    // ==================== 投射物相关事�?====================

    /// <summary>
    /// 投射物发射事�?
    /// </summary>
    public class ProjectileFiredEvent : GameEventBase
    {
        public Entity ProjectileEntity;
        public Entity SourcePlantEntity;
        public string ProjectileType;
        public float3 StartPosition;
        public float3 Direction;
    }

    /// <summary>
    /// 投射物命中事�?
    /// </summary>
    public class ProjectileHitEvent : GameEventBase
    {
        public Entity ProjectileEntity;
        public Entity TargetEntity;
        public float Damage;
        public float3 HitPosition;
        public bool IsCritical; // 是否暴击
    }

    // ==================== 资源相关事件 ====================

    /// <summary>
    /// 阳光生产事件
    /// </summary>
    public class SunProducedEvent : GameEventBase
    {
        public Entity SourceEntity; // 可能是向日葵或天�?
        public int SunAmount;
        public float3 Position;
        public bool IsFromSky; // 是否来自天空
    }

    /// <summary>
    /// 阳光收集事件
    /// </summary>
    public class SunCollectedEvent : GameEventBase
    {
        public int SunAmount;
        public int TotalSun;
        public float3 CollectionPosition;
    }

    /// <summary>
    /// 阳光消耗事�?
    /// </summary>
    public class SunSpentEvent : GameEventBase
    {
        public int SunAmount;
        public int RemainingSun;
        public string SpentOn; // 花费用途（植物类型等）
    }

    // ==================== UI相关事件 ====================

    /// <summary>
    /// 卡片选择事件
    /// </summary>
    public class PlantCardSelectedEvent : GameEventBase
    {
        public string PlantType;
        public int SunCost;
        public bool CanAfford;
    }

    /// <summary>
    /// 卡片冷却完成事件
    /// </summary>
    public class PlantCardCooldownCompleteEvent : GameEventBase
    {
        public string PlantType;
    }

    /// <summary>
    /// 提示消息事件
    /// </summary>
    public class ShowMessageEvent : GameEventBase
    {
        public string MessageKey;
        public string MessageText;
        public float Duration; // 显示时长（秒�?
        public MessageType Type; // 消息类型
    }

    public enum MessageType
    {
        Info,
        Warning,
        Success,
        Error
    }

    // ==================== 音效相关事件 ====================

    /// <summary>
    /// 播放音效事件
    /// </summary>
    public class PlaySoundEvent : GameEventBase
    {
        public string SoundId;
        public float3 Position;
        public float Volume = 1f;
        public bool IsLooping = false;
    }

    /// <summary>
    /// 播放音乐事件
    /// </summary>
    public class PlayMusicEvent : GameEventBase
    {
        public string MusicId;
        public bool FadeIn = true;
        public float FadeDuration = 1f;
    }

    // ==================== 特效相关事件 ====================

    /// <summary>
    /// 播放特效事件
    /// </summary>
    public class PlayEffectEvent : GameEventBase
    {
        public string EffectId;
        public float3 Position;
        public quaternion Rotation;
        public float Scale = 1f;
        public bool AutoDestroy = true;
    }

    // ==================== 成就/统计事件 ====================

    /// <summary>
    /// 成就解锁事件
    /// </summary>
    public class AchievementUnlockedEvent : GameEventBase
    {
        public string AchievementId;
        public string AchievementName;
        public int RewardAmount;
    }

    /// <summary>
    /// 统计更新事件
    /// </summary>
    public class StatisticsUpdatedEvent : GameEventBase
    {
        public string StatKey;
        public int OldValue;
        public int NewValue;
    }

    // ==================== 调试相关事件 ====================

    /// <summary>
    /// 调试命令事件
    /// </summary>
    public class DebugCommandEvent : GameEventBase
    {
        public string Command;
        public string[] Arguments;
    }
}
