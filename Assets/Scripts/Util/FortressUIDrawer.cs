using UnityEngine;
using Unity.Mathematics;

public class FortressUIDrawer : MonoBehaviour {

    public FortressUISystem system;
    public GUIStyle style;

    public ComponentPool<LineRenderer> lineRenderers;

    private void Start()
    {
        style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 20;
        lineRenderers = new ComponentPool<LineRenderer>();
        lineRenderers.prefab = FortressSettings.Instance.lineRenderer;
    }

    private void Update()
    {
        var paths = system.pathfinders.paths;
        var pathRequests = system.pathfinders.pathRequests;
        var length = system.pathfinders.Length;
        for (var i = 0; i < length; ++i)
        {
            var request = pathRequests[i];
            if (request.status == PathRequestStatus.Done)
            {
                var path = paths[i];
                var renderer = lineRenderers.New();
                var points = new Vector3[request.pathSize];
                for (var j = 0; j < request.pathSize; ++j)
                    points[j] = path[j].position;
                renderer.positionCount = request.pathSize;
                renderer.SetPositions(points);
            }
        }

        lineRenderers.Present();
    }

    private void OnGUI()
    {
        var cam = Camera.main;
        if (cam == null) return;
        if (system == null) return;
        var length = system.fortresses.Length;
        var positions = system.fortresses.positions;
        var fortresses = system.fortresses.fortresses;

        for (var i = 0; i < length; ++i)
        {
            var screenPos = cam.WorldToScreenPoint(positions[i].Value + new float3(0, 1.3f, 0));
            var viewportPos = cam.ScreenToViewportPoint(screenPos);
            screenPos.y = Screen.height - screenPos.y;
            if (viewportPos.x < 0 || viewportPos.x > 1 || viewportPos.y < 0 || viewportPos.y > 1 || viewportPos.z < 0) //not valid
                continue;
            var screenRect = new Rect(screenPos.x - 50, screenPos.y - 10, 100, 20);
            var down = GUI.TextArea(screenRect, fortresses[i].troops.ToString(), style);
        }

    }

    private void OnDestroy()
    {
    }
}
