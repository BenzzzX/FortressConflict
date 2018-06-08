using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Experimental.AI;
using UnityEditor;

public enum CrowdState
{
    Waiting = 1,
    Moving = 2,
    Reached = 4,
}

[System.Serializable]
public struct CrowdAgentData : IComponentData
{
    public int pathId;

    public float speed;

    public float rotateSpeed;

    public PathPoint fromPoint;

    public PathPoint steerTarget;

    public NavMeshLocation location;

    [MixedEnum]
    public CrowdState state;

}

public class CrowdAgent : ComponentDataWrapper<CrowdAgentData>
{

}

