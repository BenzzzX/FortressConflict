using Unity.Entities;

[System.Serializable]
public struct TextureAnimatorData : IComponentData
{
    public float animationNormalizedTime;

    public int currentAnimationId;
    public int newAnimationId;

    public int unitType;

    public float animationSpeedVariation;
}