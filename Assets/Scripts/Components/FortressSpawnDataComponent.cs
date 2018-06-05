using Unity.Entities;

[System.Serializable]
public struct FortressSpawnData : IComponentData
{
    public float frequency;
    public float remain;
}

public class FortressSpawnDataComponent : ComponentDataWrapper<FortressSpawnData>
{

}
