using Unity.Entities;
using Unity.Mathematics;

[System.Serializable]
public struct InFormationData : IComponentData
{
    public int index;
    public Entity formationEntity;
}

