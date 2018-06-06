using Unity.Mathematics;

[System.Serializable]
public struct PathPoint
{
    public float3 position;
    public float vertexSide;
    public StraightPathFlags flag;
}

public class PathDataComponent : FixedArrayWrapper<PathPoint>
{

}