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
    public Cell.Type type = Cell.Type.Water;

    private Camera _cam;
    private TerrainGenerator _terrainGenerator;

    public void Start()
    {
        _cam = Camera.main;
        _terrainGenerator = FindObjectOfType<TerrainGenerator>();
    }

    public void OnGUI()
    {
        RaycastHit hit;
        if (!Physics.Raycast(_cam.ScreenPointToRay(Input.mousePosition), out hit))
            return;

        MeshCollider meshCollider = hit.collider as MeshCollider;
        if (meshCollider == null || meshCollider.sharedMesh == null)
            return;

        worldPosition = hit.point;
        test = _terrainGenerator.WorldCoordinatesToCell(hit.point);
        
        worldPosition.y = _terrainGenerator.getValueAt(test.x, test.y) * _terrainGenerator.elevationScale;
        worldPosition.x = test.x * _terrainGenerator.scale;
        worldPosition.z = test.y *_terrainGenerator.scale;

        if (Input.GetMouseButton(0))
        {
            for (var x = -2; x < 1; x++)
            {
                for (var y = -2; y < 1; y++)
                {
                    _terrainGenerator.Add(test.x + x, test.y + y, type, amount);
                }
            }
        }
    }

    // Update is called once per frame
    public void Update()
    {
    }
}