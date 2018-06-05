using Unity.Mathematics;
using Unity.Entities;

[System.Serializable]
public struct RagdollData : IComponentData
{
    public float3 velocity;
    public float3 angularVelocity;
}