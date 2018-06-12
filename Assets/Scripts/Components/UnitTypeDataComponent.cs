using Unity.Entities;
using Unity.Mathematics;

[System.Serializable]
public struct UnitTypeData : ISharedComponentData
{
    public int formationWidth;
    
    public float maxSpeed;

    public float zOffset;


    public float radius;
    public float timeHorizon;
    public float neighborDist;
}


class UnitTypeDataComponent : SharedComponentDataWrapper<UnitTypeData>
{
}