using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;

//@TODO: find anothor way to render UI
public class FortressUIRenderer : MonoBehaviour
{
    public GUIStyle style;

    public NativeArray<FortressData> fortressDatas;
    public NativeArray<Position> positions;
    public int length;


    private void Start()
    {
        style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 20;
    }

    private void Update()
    {
        
    }

    private void OnGUI()
    {
        var cam = Camera.main;
        if (cam == null) return;

        if (!fortressDatas.IsCreated) return;
        if (!positions.IsCreated) return;

        for (var i = 0; i < length; ++i)
        {
            var screenPos = cam.WorldToScreenPoint(positions[i].Value + new float3(0, 1.3f, 0));
            var viewportPos = cam.ScreenToViewportPoint(screenPos);
            screenPos.y = Screen.height - screenPos.y;
            if (viewportPos.x < 0 || viewportPos.x > 1 || viewportPos.y < 0 || viewportPos.y > 1 || viewportPos.z < 0) //not valid
                continue;
            var screenRect = new Rect(screenPos.x - 50, screenPos.y - 10, 100, 20);
            var down = GUI.TextArea(screenRect, fortressDatas[i].troops.ToString(), style);
        }

    }

    private void OnDestroy()
    {
    }
}
