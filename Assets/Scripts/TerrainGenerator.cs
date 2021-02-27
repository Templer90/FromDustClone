using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Random = System.Random;

public class TerrainGenerator : MonoBehaviour
{
    [Header("Mesh Settings")] public int mapSize = 255;
    public int chunksize = 10;
    public float scale = 20;
    public float elevationScale = 10;
    public GameObject chunkPrefab;

    [Header("Erosion Settings")] public ComputeShader erosion;
    public int numErosionIterations = 50000;
    public int erosionBrushRadius = 3;

    // Internal
    public float[] _map;
    private Chunk[] _chunks;
    public RuntimeMap RuntimeMap;


    public void GenerateHeightMap()
    {
        _map = FindObjectOfType<HeightMapGenerator>().GenerateHeightMap(mapSize + 1);
    }

    public void Start()
    {
        var numChunks = mapSize / chunksize;
        _chunks = new Chunk[numChunks * numChunks];

        RuntimeMap = new RuntimeMap(_map, mapSize);

        for (var i = 0; i < transform.childCount; i++)
        {
            var chunk = transform.GetChild(i).gameObject.GetComponent<Chunk>();
            chunk.Initialize((int) chunk.coords.x, (int) chunk.coords.y, RuntimeMap, mapSize, chunksize, scale,
                elevationScale);
            _chunks[i] = chunk;
        }
    }

    public Vector2Int WorldCoordinatesToCell(Vector3 world)
    {
        var ret = new Vector2Int(0, 0);
        world.y = 0;
        ret.x = (int) (world.x);
        ret.y = (int) (world.z);

        if (ret.x > mapSize) ret.x = mapSize;
        if (ret.x <= 0) ret.x = 0;

        if (ret.y > mapSize) ret.y = mapSize;
        if (ret.y <= 0) ret.y = 0;

        return ret;
    }

    public float getValueAt(int x, int y)
    {
        return RuntimeMap.CellAt(x, y).WholeHeight;
    }

    public void Add(int x, int y, float amount)
    {
        RuntimeMap.Add(x, y, amount, Cell.Type.Stone);
    }


    public void ConstructMesh()
    {
        var numChunks = mapSize / chunksize;
        _chunks = new Chunk[numChunks * numChunks];

        RuntimeMap = new RuntimeMap(_map, mapSize);

        for (var x = 0; x < numChunks; x++)
        {
            for (var y = 0; y < numChunks; y++)
            {
                var pos = new Vector3(x * scale * 2, 0, y * scale * 2);
                var child = Instantiate(chunkPrefab, transform);
                child.name = "Chunk(" + x + "," + y + ")";
                child.transform.position = pos;

                var chunk = child.GetComponent<Chunk>();
                _chunks[y * numChunks + x] = chunk;
                chunk.Initialize(x, y, RuntimeMap, mapSize, chunksize, scale, elevationScale);
            }
        }

        foreach (var chunk in _chunks)
        {
            chunk.ConstructMeshes();
        }
    }

    public void Update()
    {
        foreach (var chunk in _chunks)
        {
            chunk.UpdateMesh();
        }
    }

    public void EmptyChildren()
    {
        for (var i = transform.childCount; i-- > 0;)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }
}