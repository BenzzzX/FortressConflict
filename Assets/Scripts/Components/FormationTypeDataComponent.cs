using Unity.Entities;

[System.Serializable]
public struct FormationTypeData : ISharedComponentData
{
    public int maxTroops;
    public float attackRange;
    public int senceRange;
    public float fov;

    public float speed;
    public float rotateSpeed;
    public UnitTypeData unitType;
}


class FormationTypeDataComponent : SharedComponentDataWrapper<FormationTypeData>
{
}