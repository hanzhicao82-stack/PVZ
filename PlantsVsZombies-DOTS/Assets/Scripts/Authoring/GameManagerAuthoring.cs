using Unity.Entities;
using UnityEngine;

public class GameManagerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public GameObject plantPrefab;
    public GameObject zombiePrefab;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        // Initialize game state and systems here
        // Example: dstManager.AddComponentData(entity, new GameState { ... });
    }

    private void Start()
    {
        // Optionally, initialize game systems or state here
    }
}