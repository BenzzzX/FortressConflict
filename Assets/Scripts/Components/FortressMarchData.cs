using Unity.Mathematics;
using Unity.Entities;

[System.Serializable]
public struct FortressMarchData : IComponentData
{
    public int withTroops; //此次进军使用多少兵力
    public Entity targetEntity;
    public Entity entity; //发起进军的堡垒
}