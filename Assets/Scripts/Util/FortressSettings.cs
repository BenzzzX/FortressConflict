using Unity.Rendering;
using UnityEngine;

public class FortressSettings : ScriptableObject {

    [SerializeField]
    public Material baseMaterial;

    [SerializeField]
    public Material selectedMaterial;

    [SerializeField]
    public GameObject lineRenderer;

    [SerializeField]
    public GameObject formationPrefab;

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
