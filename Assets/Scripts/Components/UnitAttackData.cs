using Unity.Mathematics;
using Unity.Entities;

[System.Serializable]
public struct UnitAttackData : IComponentData
{
    public const float frequency = 1.0f;
    public const float hitTimt = 0.7f;
    public float remain; //攻击周期
    public Entity targetEntity;
    public float3 targetPosition;
}
