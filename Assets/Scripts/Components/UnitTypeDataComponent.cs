using Unity.Entities;
using Unity.Mathematics;

[System.Serializable]
public struct UnitTypeData : ISharedComponentData
{
    public float separationWeight;
    public float alignmentWeight;
    public float targetWeight;
    public int width;

    public float rotateSpeed;
    public float speed;
    public float zOffset;

    public int2 bound;
}


class UnitTypeDataComponent : SharedComponentDataWrapper<UnitTypeData>
{
}