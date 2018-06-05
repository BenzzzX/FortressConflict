using System.Runtime.InteropServices;
using Unity.Entities;

[StructLayout(LayoutKind.Sequential)]
[System.Serializable]
public struct ConvertData : IComponentData
{
    public UnitType typeTo;

}

public class ConvertDataComponent : ComponentDataWrapper<ConvertData>
{

}
