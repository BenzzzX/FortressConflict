using Unity.Entities;

[System.Serializable]
public struct DispatchData : IComponentData
{
    public Entity target;
    public int troops;
}



public class DispatchDataComponent : ComponentDataWrapper<DispatchData>
{
}