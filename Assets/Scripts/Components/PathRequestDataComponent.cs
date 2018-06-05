using Unity.Entities;
using Unity.Mathematics;

using UnityEngine.Experimental.AI;

public enum PathRequestStatus
{
    NewRequest,
    InQueue,
    InProgress,
    Done,
    Failure,
    Idle,
}

[System.Serializable]
public struct PathRequestData : IComponentData
{
    public float3 start;
    public float3 end;
    public int agentType;
    public int mask;

    public int pathSize;
    public PathRequestStatus status;


}

public class PathRequestDataComponent : ComponentDataWrapper<PathRequestData>
{

}