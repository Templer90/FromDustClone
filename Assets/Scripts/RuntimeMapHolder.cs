using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class RuntimeMapHolder : MonoBehaviour
{
    public IRuntimeMap runtimeMap;

    public MapTypes mapType = MapTypes.CellBased;
    public PhysicData data;
    [Range(-0.1f, 10)] public float timer = 1.0f;
    [Min(1)] public int chunkOffset = 1;

    // ReSharper disable once InconsistentNaming
    [Min(1)] public float LOD1Size = 200;
    [Min(1)] public float LOD2Size = 300;

    private float _counter;
    private Camera _cam;
    private int _mapSize;
    private int _chunksize;
    private float _scale;
    private Chunk[] _chunks;
    
    private readonly Plane[] _LOD0Planes = new Plane[6];
    private readonly Plane[] _LOD1Planes = new Plane[6];
    private readonly Plane[] _LOD2Planes = new Plane[6];


    public enum MapTypes
    {
        Simple,
        CellBased,
        NavierStokes
    }

    public IRuntimeMap MakeNewRuntimeMap(int sideLength, IReadOnlyList<float> initialStoneHeightMap,
        IReadOnlyList<float> initialWaterMap)
    {
        IRuntimeMap newMapUpdate;
        switch (mapType)
        {
            case MapTypes.Simple:
                newMapUpdate = new SimpleMapUpdate(sideLength, data, initialStoneHeightMap, initialWaterMap);
                break;
            case MapTypes.CellBased:
                newMapUpdate = new CellBasedMapUpdate(sideLength, data, initialStoneHeightMap, initialWaterMap);
                break;
            case MapTypes.NavierStokes:
                newMapUpdate = new NavierStokes(sideLength, data, initialStoneHeightMap, initialWaterMap);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        data = newMapUpdate.Physic;
        return newMapUpdate;
    }

    public void Add(int x, int y, Cell.Type type, float amount)
    {
        if (!runtimeMap.ValidCoord(x, y)) return;
        runtimeMap.Add(x, y, type, amount);
    }

    public void Update()
    {
        _counter += Time.deltaTime;
        if (_counter > 0 && _counter < timer) return;
        _counter -= timer;

        
        GeometryUtility.CalculateFrustumPlanes(_cam, _LOD0Planes);
        UpdateLODPlanes(_LOD1Planes, LOD1Size, LOD2Size);
        UpdateLODPlanes(_LOD2Planes, LOD2Size, _cam.farClipPlane);

        var startOffset = Time.frameCount % chunkOffset;
        for (var i = startOffset; i < _chunks.Length; i += chunkOffset)
        {
            if (GeometryUtility.TestPlanesAABB(_LOD0Planes, _chunks[i].bounds))
            {
                EnsureLOD(_chunks[i]);
                _chunks[i].Show();
            }
            else
            {
                _chunks[i].Hide();
            }
        }
    }

    private void EnsureLOD(Chunk chunk)
    {
        chunk.LOD = LODTriangles.LOD.LOD0;
        if (GeometryUtility.TestPlanesAABB(_LOD1Planes, chunk.bounds))
        {
            chunk.LOD = LODTriangles.LOD.LOD1;
          
        }
        if (GeometryUtility.TestPlanesAABB(_LOD2Planes, chunk.bounds))
        {
            chunk.LOD = LODTriangles.LOD.LOD2;
           
        }
        if (GeometryUtility.TestPlanesAABB(_LOD0Planes, chunk.bounds))
        {
           // chunk.LOD = LODTriangles.LOD.LOD0;
            return;
        }
        Debug.Log("Chunk outside and inside of frustum?");
    }

    private void UpdateLODPlanes(Plane[] planes, float near, float farClipPlane)
    {
        // create projection matrix: 60 FOV, square aspect, near plane 1, far plane 1000
        var matrix = _cam.projectionMatrix;
        var c = -(farClipPlane + near) / (farClipPlane - near);
        var d = -(2.0F * farClipPlane * near) / (farClipPlane - near);
        matrix[2, 2] = c;
        matrix[2, 3] = d;
        GeometryUtility.CalculateFrustumPlanes(matrix * _cam.worldToCameraMatrix, planes);
    }

    public Vector2Int WorldCoordinatesToCell(Vector3 world)
    {
        var ret = new Vector2Int(0, 0);
        world.y = 0;
        ret.x = (int) ((world.x * _chunksize / (_scale)));
        ret.y = (int) ((world.z * _chunksize / (_scale)));

        if (ret.x > _mapSize) ret.x = _mapSize;
        if (ret.x <= 0) ret.x = 0;

        if (ret.y > _mapSize) ret.y = _mapSize;
        if (ret.y <= 0) ret.y = 0;

        return ret;
    }

    public void Initialize(Camera main, int mapsize, int chunkSize, float scaling, Chunk[] chunks, IRuntimeMap map)
    {
        _cam = main;
        _chunksize = chunkSize;
        _mapSize = mapsize;
        _scale = scaling;
        _chunks = chunks;
        runtimeMap = map;
    }
}