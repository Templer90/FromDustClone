using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Assertions;
using Random = System.Random;


public class RuntimeMap : IRuntimeMap
{
    private Cell[] _map;
    private Cell[] _previousMap;

    private readonly int _mapSize;
    private readonly int _mapSizeSquared;

    public readonly PhysicData physic = new PhysicData();

    public RuntimeMap(IReadOnlyList<float> heightmap, int heightMapSize)
    {
        Assert.AreEqual(heightmap.Count, (heightMapSize + 1) * (heightMapSize + 1));

        _mapSize = heightMapSize;
        _mapSizeSquared = heightmap.Count;

        _map = new Cell[heightmap.Count];
        _previousMap = new Cell[heightmap.Count];
        for (var i = 0; i < heightmap.Count; i++)
        {
            _map[i] = new Cell {Stone = heightmap[i], Water = 0f, Sand = 0f, Lava = 0f};
            _previousMap[i] = new Cell {Stone = heightmap[i], Water = 0f, Sand = 0f, Lava = 0f};
        }
    }

    private void Swap()
    {
        var tmp = _map;
        _map = _previousMap;
        _previousMap = tmp;
    }

    public bool ValidCoord(int x, int y)
    {
        var pos = y * _mapSize + x;
        return pos >= 0 && pos < _map.Length;
    }

    public Cell CellAt(int x, int y)
    {
        return _map[y * _mapSize + x];
    }

    public float WholeAt(int x, int y)
    {
        return CellAt(x, y).WholeHeight;
    }

    public float ValueAt(int x, int y, Cell.Type type)
    {
        var cell = CellAt(x, y);
        return type switch
        {
            Cell.Type.Stone => cell.Stone,
            Cell.Type.Sand => cell.Sand,
            Cell.Type.Water => cell.Water,
            Cell.Type.Lava => cell.Lava,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public void Add(int x, int y, Cell.Type type, float amount)
    {
        var cell = CellAt(x, y);
        switch (type)
        {
            case Cell.Type.Stone:
                cell.Stone += amount;
                break;
            case Cell.Type.Sand:
                cell.Sand += amount;
                break;
            case Cell.Type.Water:
                cell.Water += amount;
                break;
            case Cell.Type.Lava:
                cell.Lava += amount;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public void SimpleSmooth()
    {
        var kernel = new[]
        {
            (-1, -1), (-1, 0), (-1, +1),
            (0, -1), (0, +1),
            (+1, -1), (+1, 0), (+1, +1)
        };
        const float materialStiffness = 0.001f;

        for (var x = 1; x < _mapSize - 1; x++)
        {
            for (var y = 1; y < _mapSize - 1; y++)
            {
                var middle = y * _mapSize + x;

                for (var i = 0; i < 8; i++)
                {
                    var other = (y + kernel[i].Item1) * _mapSize + (x + kernel[i].Item2);
                    var diff = _map[middle].Water - _map[other].Water;
                    var delta = diff * materialStiffness;
                    _map[middle].Water -= delta;
                    _map[other].Water += delta;
                }
            }
        }
    }


    public void MapUpdate()
    {
        var kernel = physic.GETKernel();
        var a = physic.Sand_dt * physic.SandViscosity * _mapSizeSquared;

        void HandleWater(Cell centerCell, Cell prevCenterCell, int x, int y)
        {
            for (var i = 0; i < kernel.Length; i++)
            {
                var otherIndex = (y + kernel[i].Item1) * _mapSize + (x + kernel[i].Item2);
                var otherCell = _map[otherIndex];

                if (centerCell.Water < physic.EvaporationThreshold)
                {
                    centerCell.Water = 0;
                    continue;
                }

                var waterHeight = centerCell.Stone + centerCell.Water;
                var otherWaterHeight = otherCell.Stone + otherCell.Water;
                if (otherCell.Stone > waterHeight) continue;
                if (otherWaterHeight > waterHeight) continue;
                var waterDiff = waterHeight - otherWaterHeight;
                centerCell.Water -= waterDiff / 2 * physic.WaterViscosity;
                otherCell.Water += waterDiff / 2 * physic.WaterViscosity;
            }
        }

        void HandleSand(Cell centerCell, Cell prevCenterCell, int x, int y)
        {
            if (centerCell.Sand < physic.SandStiffness) return;

            var sandAcc = 0.0f;
            var foundAcc = 0;
            var indices = new int[kernel.Length];
            for (var i = 0; i < kernel.Length; i++)
            {
                var otherIndex = (y + kernel[i].Item1) * _mapSize + (x + kernel[i].Item2);
                var otherCell = _map[otherIndex];

               // if ((prevCenterCell.Stone + prevCenterCell.Sand) > (otherCell.Stone + otherCell.Sand)) continue;
                sandAcc += otherCell.Sand;
                indices[foundAcc] = otherIndex;
                foundAcc++;
            }

            centerCell.Sand = (prevCenterCell.Sand + a * (sandAcc)) / (1 + foundAcc * a);
            for (var i = 0; i < foundAcc; i++)
            {
                var otherCell = _map[indices[i]];
                var previousOtherCell = _previousMap[indices[i]];
                otherCell.Sand = previousOtherCell.Sand - a * (sandAcc / foundAcc) / (1 +  a);
            }
        }

        void HandleSandDumb(Cell centerCell, Cell prevCenterCell, int x, int y)
        {
            if (centerCell.Sand < physic.SandStiffness) return;
            for (var i = 0; i < kernel.Length; i++)
            {
                var otherIndex = (y + kernel[i].Item1) * _mapSize + (x + kernel[i].Item2);
                var otherCell = _map[otherIndex];
                var sandDiff = (centerCell.Stone + centerCell.Sand) - (otherCell.Stone + otherCell.Sand);
                var delta = sandDiff * physic.SandHardness;

                centerCell.Sand -= delta;
                otherCell.Sand += delta;
            }
        }


        for (var x = 1; x < _mapSize - 1; x++)
        {
            for (var y = 1; y < _mapSize - 1; y++)
            {
                var middleIndex = y * _mapSize + x;
                var centerCell = _map[middleIndex];
                var prevCenterCell = _previousMap[y * _mapSize + x];

                HandleSand(centerCell, prevCenterCell, x, y);
                //HandleWater(centerCell, x, y);
            }
        }

        Swap();
    }
}