using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;

public enum FormationState
{
    Spawning = 1,
    Attacking = 2,
}

[System.Serializable]
public struct FormationData : IComponentData
{
    public int troops;

    public float sideOffset;

    [MixedEnum]
    public FormationState state;

    public float3 GetUnitAlignTarget(int unitId, Position position, Heading heading, int width, float radius)
    {
        float3 sideVector = heading.Value.zyx;
        sideVector.x = -sideVector.x;

        // required to hit the corners correctly
        var side = sideVector * width * sideOffset * 0.5f;

        var height = math.ceil((float)troops / width);
        var offset = sideVector * ((unitId % width) - (width * 0.5f)) + side +
                            heading.Value * (height - unitId / width);
        return position.Value + offset * (radius * 4 + 0.4f);
    }
}


public class FormationDataComponent : ComponentDataWrapper<FormationData>
{
}