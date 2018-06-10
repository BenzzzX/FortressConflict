using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Experimental.AI;
using UnityEditor;

public enum AgentState
{
    Waiting = 1,
    Moving = 2,
    Reached = 4,
}

[System.Serializable]
public struct FormationAgentData : IComponentData
{
    public int pathId;

    public PathPoint fromPoint;

    public PathPoint steerTarget;

    public NavMeshLocation location;

    [MixedEnum]
    public AgentState state;

}

public class FormationAgent : ComponentDataWrapper<FormationAgentData>
{

}

