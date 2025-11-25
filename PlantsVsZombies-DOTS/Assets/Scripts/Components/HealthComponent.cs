using Unity.Entities;

[GenerateAuthoringComponent]
public struct HealthComponent : IComponentData
{
    public int CurrentHealth;
}