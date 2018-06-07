using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Experimental.AI;
using UnityEditor;

[System.Serializable]
public enum CrowdState
{
    waiting = 1,
    moving = 2,
    reached = 4,
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

