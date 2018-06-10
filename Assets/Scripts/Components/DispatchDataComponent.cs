using Unity.Entities;
using Unity.Mathematics;

[System.Serializable]
public struct DispatchData : IComponentData
{
    public Entity target;
    public int troops;

    public float3 offset;

    public Entity dispatching;
    public int doneDispatch;
    public float remain;
    public float frequency;
}



public class DispatchDataComponent : ComponentDataWrapper<DispatchData>
{
}