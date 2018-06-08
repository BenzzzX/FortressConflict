using Unity.Entities;

[System.Serializable]
public struct SpawnData : IComponentData
{
    public float frequency;
    public float remain;
}

public class SpawnDataComponent : ComponentDataWrapper<SpawnData>
{

}
