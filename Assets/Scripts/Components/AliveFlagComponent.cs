using Unity.Entities;


[System.Serializable]
public struct AliveFlag : IComponentData
{

}

public class AliveFlagComponent : ComponentDataWrapper<AliveFlag>
{
}


public struct DyingData : IComponentData
{
    public float timeToExpire;
    public float startingYCoord;
}