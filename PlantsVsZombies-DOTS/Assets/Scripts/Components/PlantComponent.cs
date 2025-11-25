using Unity.Entities;

[GenerateAuthoringComponent]
public struct PlantComponent : IComponentData
{
    public PlantType PlantType;
    public int Damage;
    public float Range;
}

public enum PlantType
{
    Peashooter,
    Sunflower,
    CherryBomb,
    WallNut
}