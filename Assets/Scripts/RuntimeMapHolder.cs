using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class RuntimeMapHolder : MonoBehaviour
{
    public IRuntimeMap runtimeMap;

    public MapTypes mapType = MapTypes.CellBased;
    public PhysicData data;
    [Range(-0.1f, 10)] public float timer = 1.0f;
    [Min(1)] public int chunkOffset = 1;
    [Min(1)] public float LOD2Size = 300;

    private float _counter = 0;
    private Camera _cam;
    private int _mapSize = 255;
    private int _chunksize = 10;
    private float _scale = 20;
    private Chunk[] _chunks;
    private readonly Plane[] _planes = new Plane[6];

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


    public float getValueAt(int x, int y)
    {
        return runtimeMap.CellAt(x, y).WholeHeight;
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

        GeometryUtility.CalculateFrustumPlanes(_cam, _planes);
        var startOffset = Time.frameCount % chunkOffset;
        for (var i = startOffset; i < _chunks.Length; i += chunkOffset)
        {
            if (GeometryUtility.TestPlanesAABB(_planes, _chunks[i].bounds))
            {
                _chunks[i].Show();
            }
            else
            {
                _chunks[i].Hide();
            }
        }

        // create projection matrix: 60 FOV, square aspect, near plane 1, far plane 1000
        var matrix = _cam.projectionMatrix;
        var near = LOD2Size;
        var farClipPlane = _cam.farClipPlane;
        var c = -(farClipPlane + near) / (farClipPlane - near);
        var d = -(2.0F * farClipPlane * near) / (farClipPlane - near);
        matrix[2, 2] = c;
        matrix[2, 3] = d;
        GeometryUtility.CalculateFrustumPlanes(matrix * _cam.worldToCameraMatrix, _planes);
        for (var i = startOffset; i < _chunks.Length; i += chunkOffset)
        {
            if (GeometryUtility.TestPlanesAABB(_planes, _chunks[i].bounds))
            {
                _chunks[i].LOD = LODTriangles.LOD.LOD2;
            }
            else
            {
                _chunks[i].LOD = LODTriangles.LOD.LOD0;
            }
        }
    }

    public Matrix4x4 test;

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