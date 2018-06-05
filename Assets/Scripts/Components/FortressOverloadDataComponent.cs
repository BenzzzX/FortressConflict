using Unity.Entities;

[System.Serializable]
public struct FortressOverloadData : IComponentData
{
    public int overload;
    public float frequency;
    public float remain;
}

public class FortressOverloadDataComponent : ComponentDataWrapper<FortressOverloadData>
{

}