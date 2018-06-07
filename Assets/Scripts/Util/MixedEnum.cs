using UnityEngine;
using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Enum)]
public class MixedEnumAttribute : PropertyAttribute
{
    public string enumName;

    public MixedEnumAttribute() { }

    public MixedEnumAttribute(string name)
    {
        enumName = name;
    }
}