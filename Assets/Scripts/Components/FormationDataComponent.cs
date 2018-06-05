using Unity.Mathematics;
using Unity.Entities;

[System.Serializable]
public struct FormationData : IComponentData
{
    public int troops;
    public float3 position;
    public float3 forward;

    public int width;
    public int unitCount;

    // bit 0    - isAttacking
    private int bitfield;

    public bool isAttacking
    {
        get { return IsBit(0); }
        set { SetBit(0, value); }
    }


    private bool IsBit(int bit)
    {
        return (bitfield & (1 << bit)) != 0;
    }

    private void SetBit(int bit, bool value)
    {
        bitfield = value ? (bitfield | (1 << bit)) : (bitfield & ~(1 << bit));
    }
}


public class FormationDataComponent : ComponentDataWrapper<FormationData>
{

}

public struct FormationNavigationData : IComponentData
{
    public float3 TargetPosition;
}
