using System;

[Serializable]
public class GameConfig
{
    public int initialPlantCount;
    public int initialZombieCount;
    public float plantSpawnInterval;
    public float zombieSpawnInterval;
    public float gameDuration;

    public GameConfig(int initialPlantCount, int initialZombieCount, float plantSpawnInterval, float zombieSpawnInterval, float gameDuration)
    {
        this.initialPlantCount = initialPlantCount;
        this.initialZombieCount = initialZombieCount;
        this.plantSpawnInterval = plantSpawnInterval;
        this.zombieSpawnInterval = zombieSpawnInterval;
        this.gameDuration = gameDuration;
    }
}