using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public partial class ZombieMovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        Entities.ForEach((ref Translation translation, in ZombieComponent zombie) =>
        {
            // Move the zombie towards the target position (e.g., a plant's position)
            float3 targetPosition = new float3(0, 0, 0); // Replace with actual target position logic
            float3 direction = math.normalize(targetPosition - translation.Value);
            translation.Value += direction * zombie.Speed * deltaTime;

        }).Schedule();
    }
}