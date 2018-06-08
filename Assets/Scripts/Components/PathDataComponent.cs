using Unity.Mathematics;
using UnityEngine.Experimental.AI;

[System.Serializable]
public struct PathPoint
{
    public NavMeshLocation location;
    public float vertexSide;
    public StraightPathFlags flag;
}

public class PathDataComponent : FixedArrayWrapper<PathPoint>
{

}