using Unity.Entities;
using Unity.Rendering;

[System.Serializable]
public struct UnitData : IComponentData
{
    public float health;
    
}



public class UnitDataComponent : ComponentDataWrapper<UnitData>
{

}