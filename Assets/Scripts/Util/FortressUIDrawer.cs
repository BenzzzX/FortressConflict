using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;

public class FortressUIDrawer : MonoBehaviour {

    public FortressUISystem system;
    public GUIStyle style;

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
        if (system == null) return;
        var length = system.fortresses.Length;
        var positions = system.fortresses.positions;
        var fortresses = system.fortresses.fortresses;
        var downs = system.downs;

        for (var i = 0; i < length; ++i)
        {
            var screenPos = cam.WorldToScreenPoint(positions[i].Value + new float3(0, 1.3f, 0));
            var viewportPos = cam.ScreenToViewportPoint(screenPos);
            screenPos.y = Screen.height - screenPos.y;
            if (viewportPos.x < 0 || viewportPos.x > 1 || viewportPos.y < 0 || viewportPos.y > 1 || viewportPos.z < 0) //not valid
                continue;
            var screenRect = new Rect(screenPos.x - 50, screenPos.y - 10, 100, 20);
            var down = GUI.Button(screenRect, fortresses[i].troops.ToString(), style);
            downs[i] = down?1:0;


        }

        var paths = system.pathfinders.paths;
        var pathRequests = system.pathfinders.pathRequests;
        length = system.pathfinders.Length;
        for (var i = 0; i < length; ++i)
        {
            var request = pathRequests[i];
            if (request.status == PathRequestStatus.Done)
            {
                var path = paths[i];
                for (var j = 1; j < request.pathSize; ++j)
                {
                    var start = path[j - 1];
                    var end = path[j];
                    var screenStart = cam.WorldToScreenPoint(start.position);
                    screenStart.y = Screen.height - screenStart.y;
                    var screenEnd = cam.WorldToScreenPoint(end.position);
                    screenEnd.y = Screen.height - screenEnd.y;
                    OnGUIExtentions.DrawLine(screenStart, screenEnd, Color.blue, 3f);
                    //Debug.DrawLine(start.position, end.position, Color.black);
                }
            }
        }


    }

    private void OnDestroy()
    {
    }
}
