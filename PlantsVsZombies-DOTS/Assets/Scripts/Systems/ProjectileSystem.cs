using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;

public struct ProjectileComponent : IComponentData
{
    public float Damage;
    public float Speed;
}

public class ProjectileSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref Translation translation, in ProjectileComponent projectile) =>
        {
            translation.Value += new float3(0, projectile.Speed * Time.DeltaTime, 0);
        }).Schedule();
    }
}