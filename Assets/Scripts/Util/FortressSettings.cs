using Unity.Rendering;
using UnityEngine;

[CreateAssetMenu(menuName = "Fortress/Create FortressSettings")]
public class FortressSettings : ScriptableObject {

    [SerializeField]
    public MeshInstanceRenderer baseRenderer;

    [SerializeField]
    public MeshInstanceRenderer selectedRenderer;

    [SerializeField]
    public GameObject lineRenderer;

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
