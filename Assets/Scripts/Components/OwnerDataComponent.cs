using Unity.Entities;

[System.Serializable]
public struct OwnerData : IComponentData
{
    public int alliance;
}

public class OwnerDataComponent : ComponentDataWrapper<OwnerData>
{

}