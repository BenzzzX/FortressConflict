using Unity.Entities;

public struct DispatchData : IComponentData
{
    public Entity target;
    public int troops;
}



public class DispatchDataComponent : ComponentDataWrapper<DispatchData>
{
}