﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

    public float panSpeed = 20f;
    public float panBorder = 10f;

    public float zoom = 15f;
    public Vector2 zoomRange = new Vector2(5f, 100f);
    public float zoomSpeed = 10000f;

	// Use this for initialization
	void Start () {
		
	}

    private void OnMoveInput()
    {
        var pos = transform.position;

        var zoomDelta = Input.GetAxis("Zoom") * Time.deltaTime * zoomSpeed;
        var preZoom = zoom;
        zoom -= zoomDelta;
        zoom = Mathf.Clamp(zoom, zoomRange.x, zoomRange.y);
        zoomDelta = preZoom - zoom;
        pos += transform.forward * zoomDelta;

        var offset = panSpeed * Time.deltaTime * (zoom / 15f);

        var deltaX = Input.GetAxis("MoveRight");
        if (Input.mousePosition.x >= Screen.width - panBorder && Input.mousePosition.x < Screen.width) deltaX = 1;
        else if (Input.mousePosition.x <= panBorder && Input.mousePosition.x > 0) deltaX = -1;

        var deltaZ = Input.GetAxis("MoveForward");
        if (Input.mousePosition.y >= Screen.height - panBorder && Input.mousePosition.y < Screen.height) deltaZ = 1;
        else if (Input.mousePosition.y <= panBorder && Input.mousePosition.y > 0) deltaZ = -1;

        pos.z += deltaZ * offset;
        pos.x += deltaX * offset;

        transform.position = pos;
    }
	
	// Update is called once per frame
	void Update () {
        OnMoveInput();
    }
}
