using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Experimental.AI;
using UnityEditor;

[System.Serializable]
public struct UnitAgentData : IComponentData
{
    public NavMeshLocation location;
}