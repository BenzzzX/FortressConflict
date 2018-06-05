using Unity.Rendering;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class FortressPrefabs : ScriptableObject {

    public GameObject FortressPrefab;

    private static FortressPrefabs instance;

    public static FortressPrefabs Instance
    {
        get
        {
            if (instance == null) instance = Resources.Load<FortressPrefabs>("FortressPrefabs");
            return instance;
        }
    }
}
