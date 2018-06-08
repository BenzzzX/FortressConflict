using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;

public enum FormationState
{
    Attacking = 1
}

[System.Serializable]
public struct FormationData : IComponentData
{
    public int troops;

    public int width;

    public float sideOffset;

    [MixedEnum]
    public FormationState state;

    public float3 GetUnitSteerTarget(Position position, Heading heading, int unitId)
    {
        float3 sideVector = heading.Value.zyx;
        sideVector.x = -sideVector.x;

        // required to hit the corners correctly
        var side = sideVector * width * sideOffset * 0.5f;

        var height = math.ceil((float)troops / width);
        var offset = sideVector * ((unitId % width) - (width * 0.5f)) + side +
                            heading.Value * (unitId / width - (height * 0.5f));
        return position.Value + offset*0.2f;
    }
}


public class FormationDataComponent : ComponentDataWrapper<FormationData>
{

}

public struct FormationNavigationData : IComponentData
{
    public float3 TargetPosition;
}
