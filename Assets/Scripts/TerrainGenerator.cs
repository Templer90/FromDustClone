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
    [SerializeField]
    private float[] stoneHeightMap;
    private Chunk[] _chunks;
    public IRuntimeMap runtimeMap;

    public void GenerateHeightMap()
    {
        stoneHeightMap = FindObjectOfType<HeightMapGenerator>().GenerateHeightMap(mapSize + 1);
    }

    public void Start()
    {
        var numChunks = mapSize / chunksize;
        _chunks = new Chunk[numChunks * numChunks];
        
        runtimeMap = FindObjectOfType<RuntimeMapHolder>().MakeNewRuntimeMap(stoneHeightMap, mapSize);

        for (var i = 0; i < transform.childCount; i++)
        {
            var chunk = transform.GetChild(i).gameObject.GetComponent<Chunk>();
            chunk.Initialize((int) chunk.coords.x, (int) chunk.coords.y, runtimeMap, mapSize, chunksize, scale,
                elevationScale);
            _chunks[i] = chunk;
        }
    }

    public Vector2Int WorldCoordinatesToCell(Vector3 world)
    {
        var ret = new Vector2Int(0, 0);
        world.y = 0;
        ret.x = (int) ((world.x * chunksize / (scale)));
        ret.y = (int) ((world.z * chunksize / (scale)));

        if (ret.x > mapSize) ret.x = mapSize;
        if (ret.x <= 0) ret.x = 0;

        if (ret.y > mapSize) ret.y = mapSize;
        if (ret.y <= 0) ret.y = 0;

        return ret;
    }

    public float getValueAt(int x, int y)
    {
        return runtimeMap.CellAt(x, y).WholeHeight;
    }

    public void Add(int x, int y, Cell.Type type, float amount)
    {
        if (!runtimeMap.ValidCoord(x, y)) return;
        runtimeMap.Add(x, y, type, amount);
    }


    public void ConstructMesh()
    {
        var numChunks = mapSize / chunksize;
        _chunks = new Chunk[numChunks * numChunks];

        runtimeMap = FindObjectOfType<RuntimeMapHolder>().MakeNewRuntimeMap(stoneHeightMap, mapSize);

        for (var x = 0; x < numChunks; x++)
        {
            for (var y = 0; y < numChunks; y++)
            {
                var pos = new Vector3(x * scale, 0, y * scale);
                var child = Instantiate(chunkPrefab, transform);
                child.name = "Chunk(" + x + "," + y + ")";
                child.transform.position = pos;

                var chunk = child.GetComponent<Chunk>();
                _chunks[y * numChunks + x] = chunk;
                chunk.Initialize(x, y, runtimeMap, mapSize, chunksize, scale, elevationScale);
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
            chunk.UpdateMeshes();
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