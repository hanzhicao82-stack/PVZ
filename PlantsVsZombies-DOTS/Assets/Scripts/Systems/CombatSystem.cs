using Unity.Entities;
using Unity.Collections;

public struct CombatComponent : IComponentData
{
    public int Damage;
}

public class CombatSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref HealthComponent health, in CombatComponent combat) =>
        {
            // Example logic for combat interaction
            if (health.CurrentHealth > 0)
            {
                health.CurrentHealth -= combat.Damage;
            }
        }).Schedule();
    }
}