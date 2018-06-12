using Unity.Entities;
using Unity.Mathematics;

[System.Serializable]
public struct UnitAgentTypeData : IComponentData
{
    public int formationWidth;
    public float maxSpeed;
    public float zOffset;


    public float radius;
    public float timeHorizon;
    public float neighborDist;
}