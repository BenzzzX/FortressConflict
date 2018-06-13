using Unity.Entities;

[System.Serializable]
public struct FormationTypeData : IComponentData
{
    public float attackRange;
    public int senceRange;
    public float fov;

    public float speed;
    public float rotateSpeed;
    public UnitAgentTypeData unitType;
}


class FormationTypeDataComponent : ComponentDataWrapper<FormationTypeData>
{
}