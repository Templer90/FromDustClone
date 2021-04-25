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
    public float stoneHeightScale = 10;
    public GameObject chunkPrefab;

    [Header("Erosion Settings")] [Range(0, 1)]
    public float waterTableHeight = 0.1f;

    [Range(0, 1)] public float topSoilHeight = 0.2f;

    public ComputeShader erosion;
    public int numErosionIterations = 50000;
    public int erosionBrushRadius = 3;

    // Internal
    [SerializeField] private float[] stoneHeightMap;
    [SerializeField] private float[] sandHeightMap;
    [SerializeField] private float[] waterHeightMap;

    public void GenerateHeightMap()
    {
        stoneHeightMap = FindObjectOfType<HeightMapGenerator>().GenerateHeightMap(mapSize);

        sandHeightMap = new float [stoneHeightMap.Length];
        for (var i = 0; i < sandHeightMap.Length; i++)
        {
            sandHeightMap[i] = topSoilHeight;
        }

        waterHeightMap = new float [stoneHeightMap.Length];
        for (var i = 0; i < waterHeightMap.Length; i++)
        {
            waterHeightMap[i] = (waterTableHeight > stoneHeightMap[i] + sandHeightMap[i]) ? waterTableHeight : 0;
        }
    }

    public void Start()
    {
        var numChunks = mapSize / chunksize;
        var chunks = new Chunk[numChunks * numChunks];

        var runtimeMap = FindObjectOfType<RuntimeMapHolder>()
            .MakeNewRuntimeMap(mapSize, stoneHeightMap, sandHeightMap, waterHeightMap);

        for (var i = 0; i < transform.childCount; i++)
        {
            var chunk = transform.GetChild(i).gameObject.GetComponent<Chunk>();
            chunk.PlayInitialize(chunk.coords.x, chunk.coords.y, runtimeMap, mapSize, chunksize, scale,
                elevationScale);
            chunks[i] = chunk;
        }

        FindObjectOfType<RuntimeMapHolder>().Initialize(Camera.main, mapSize, chunksize, scale, chunks, runtimeMap);
    }

    public void ConstructMesh()
    {
        var numChunks = mapSize / chunksize;
        var chunks = new Chunk[numChunks * numChunks];

        var runtimeMap = FindObjectOfType<RuntimeMapHolder>()
            .MakeNewRuntimeMap(mapSize, stoneHeightMap, sandHeightMap, waterHeightMap);

        for (var x = 0; x < numChunks; x++)
        {
            for (var y = 0; y < numChunks; y++)
            {
                var child = Instantiate(chunkPrefab, transform);
                var chunk = child.GetComponent<Chunk>();
                chunks[y * numChunks + x] = chunk;
                chunk.Initialize(x, y, runtimeMap, mapSize, chunksize, scale, elevationScale);
            }
        }

        foreach (var chunk in chunks)
        {
            chunk.ConstructMeshes();
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