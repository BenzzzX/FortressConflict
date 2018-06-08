using Unity.Entities;

[System.Serializable]
public struct OverloadData : IComponentData
{
    public int overload;
    public float frequency;
    public float remain;
}

public class OverloadDataComponent : ComponentDataWrapper<OverloadData>
{

}