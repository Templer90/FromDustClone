using System.Collections;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
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
    private RuntimeMapHolder _runtimeMap;

    public void Start()
    {
        _cam = Camera.main;
        _runtimeMap = FindObjectOfType<RuntimeMapHolder>();
    }

    public void OnGUI()
    {
        if (!Physics.Raycast(_cam.ScreenPointToRay(Input.mousePosition), out var hit))
            return;

        var meshCollider = hit.collider as MeshCollider;
        if (meshCollider == null || meshCollider.sharedMesh == null)
            return;

        worldPosition = hit.point;
        test = _runtimeMap.WorldCoordinatesToCell(hit.point);

        if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1)) return;
        var quantity = amount * (Input.GetMouseButton(0) ? 1 : Input.GetMouseButton(1) ? -1 : 0);
        var size = quantity / 9;
        for (var x = -2; x < 1; x++)
        {
            for (var y = -2; y < 1; y++)
            {
                _runtimeMap.Add(test.x + x, test.y + y, type, size);
            }
        }
    }
}