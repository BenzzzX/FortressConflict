using Unity.Rendering;
using UnityEngine;

public class FortressSettings : ScriptableObject {

    [SerializeField]
    public MeshInstanceRenderer baseRenderer;

    [SerializeField]
    public MeshInstanceRenderer selectedRenderer;

    [SerializeField]
    public GameObject lineRenderer;

    [SerializeField]
    public GameObject formationPrefab;

    [SerializeField]
    public int formationTroops = 250;

    private static FortressSettings instance;

    public static FortressSettings Instance
    {
        get
        {
            if (instance == null) instance = Resources.Load<FortressSettings>("Fortress Settings");
            return instance;
        }
    }
}
