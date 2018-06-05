using Unity.Entities;

public enum UnitType
{
    Lancer = 1,
    Archer = 2,
    Swordman = 3,
    Rider = 4,
    Caster = 5
}



[System.Serializable]
public struct LancerFlag : IComponentData
{
}

[System.Serializable]
public struct ArcherFlag : IComponentData
{
    public const float attackRange = 15;
}

[System.Serializable]
public struct SwordmanFlag : IComponentData
{
}

[System.Serializable]
public struct RiderFlag : IComponentData
{
}

[System.Serializable]
public struct CasterFlag : IComponentData
{
}