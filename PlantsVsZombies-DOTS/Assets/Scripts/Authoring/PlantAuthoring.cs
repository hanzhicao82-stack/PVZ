using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class PlantAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public string PlantType;
    public int Damage;
    public float Range;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new PlantComponent
        {
            PlantType = PlantType,
            Damage = Damage,
            Range = Range
        });
    }
}