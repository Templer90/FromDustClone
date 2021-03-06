using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Assertions;
using Random = System.Random;

public class RuntimeMap
{
    private readonly Cell[] _map;
    private readonly int _mapSize;

    public readonly PhysicData physic = new PhysicData();

    public RuntimeMap(IReadOnlyList<float> heightmap, int heightMapSize)
    {
        Assert.AreEqual(heightmap.Count, (heightMapSize + 1) * (heightMapSize + 1));
        _mapSize = heightMapSize;
        _map = new Cell[heightmap.Count];
        for (var i = 0; i < heightmap.Count; i++)
        {
            _map[i] = new Cell {Stone = heightmap[i], Water = 0f, Sand = 0f, Lava = 0f};
        }
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
  
        void HandleWater(Cell centerCell, int x, int y)
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

        void HandleSand(Cell centerCell, int x, int y)
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

                HandleSand(centerCell, x, y);
                //HandleWater(centerCell, x, y);
            }
        }
    }
}