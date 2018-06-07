using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;

[System.Serializable]
public struct FormationData : IComponentData
{
    public int troops;

    public int width;
    public int unitCount;

    public float sideOffset;

    public bool isAttacking
    {
        get { return IsBit(0); }
        set { SetBit(0, value); }
    }

    public bool isMarching
    {
        get { return IsBit(1); }
        set { SetBit(1, value); }
    }

    public float3 GetUnitSteerTarget(Position position, Heading heading, int unitId)
    {
        float3 sideVector = heading.Value.zyx;
        sideVector.x = -sideVector.x;

        // required to hit the corners correctly
        var side = sideVector * width * sideOffset * 0.5f;

        var height = math.ceil((float)unitCount / width);
        return position.Value + sideVector * ((unitId % width) - (width * 0.5f)) + side +
                            heading.Value * (unitId / width - (height * 0.5f));
    }

    // bit 0    - isAttacking
    private int bitfield;

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
