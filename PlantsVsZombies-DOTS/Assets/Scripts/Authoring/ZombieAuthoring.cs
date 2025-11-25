using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class ZombieAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public string ZombieType;
    public int Health;
    public float Speed;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new ZombieComponent
        {
            ZombieType = ZombieType,
            Health = Health,
            Speed = Speed
        });
    }
}