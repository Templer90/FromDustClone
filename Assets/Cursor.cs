using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Cursor : MonoBehaviour
{
    public Vector3 worldPosition;
    public Vector2Int test;
    public GameObject positionSphere;
    public float amount = 0.1f;
    
    private Camera _cam;
    private TerrainGenerator _terrainGenerator;

    public void Start()
    {
        _cam = Camera.main;
        _terrainGenerator = FindObjectOfType<TerrainGenerator>();
    }

    public void OnGUI()
    {
        var plane = new Plane(Vector3.up, Vector3.zero);

        var ray = _cam.ScreenPointToRay(Input.mousePosition);
        if (!plane.Raycast(ray, out var distance)) return;

        worldPosition = ray.GetPoint(distance);

        test = _terrainGenerator.WorldCoordinatesToCell(worldPosition);
        worldPosition.y = _terrainGenerator.getValueAt(test.x, test.y) * _terrainGenerator.elevationScale;

        positionSphere.transform.position = worldPosition;

        if (Input.GetMouseButton(0))
        {
            _terrainGenerator.Add(test.x, test.y, amount);
        }
    }

    // Update is called once per frame
    public void Update()
    {
    }
}