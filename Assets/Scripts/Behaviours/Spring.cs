using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Spring : MonoBehaviour
{
    public float amount = 0.1f;
    public int radius = 1;
    public Cell.Type type = Cell.Type.Water;
    public Vector2Int mapCoordinates;

    private TerrainGenerator _terrainGenerator;

    // Start is called before the first frame update
    public void Start()
    {
        _terrainGenerator = FindObjectOfType<TerrainGenerator>();
    }

    // Update is called once per frame
    public void Update()
    {
        if (transform.hasChanged)
        {
            if (!Physics.Raycast(new Ray(transform.position, Vector3.down), out var hit))
                return;

            var meshCollider = hit.collider as MeshCollider;
            if (meshCollider == null || meshCollider.sharedMesh == null)
                return;
            if (Application.isPlaying)
            {
                mapCoordinates = _terrainGenerator.WorldCoordinatesToCell(hit.point);
            }

            Debug.DrawRay(transform.position, Vector3.down * 2048);
            transform.hasChanged = false;
        }

        if (Application.isPlaying)
        {
            for (var x = -radius; x < radius; x++)
            {
                for (var y = -radius; y < radius; y++)
                {
                    _terrainGenerator.Add(mapCoordinates.x + x, mapCoordinates.y + y, type, amount);
                }
            }
        }
    }
}