using Unity.Entities;
using Unity.Mathematics;

[System.Serializable]
public struct FortressData : IComponentData
{
    public int troops;
    public int maxTroops;
}

public class FortressDataComponent : ComponentDataWrapper<FortressData>
{

}