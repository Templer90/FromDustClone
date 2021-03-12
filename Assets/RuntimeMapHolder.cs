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

    private float _counter = 0;
    private Camera _cam;
    private int _mapSize = 255;
    private int _chunksize = 10;
    private float _scale = 20;
    private Chunk[] _chunks;

    public enum MapTypes
    {
        Simple,
        CellBased
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

        var planes = GeometryUtility.CalculateFrustumPlanes(_cam);
        var offset = 1;
        var startOffset = Time.frameCount % offset;
        for (var i = startOffset; i < _chunks.Length; i += offset)
        {
            var chunk = _chunks[i];
            if (GeometryUtility.TestPlanesAABB(planes, chunk.Bounds))
            {
                chunk.gameObject.SetActive(true);
                chunk.UpdateMeshes();
            }
            else
            {
                chunk.gameObject.SetActive(false);
            }
        }
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