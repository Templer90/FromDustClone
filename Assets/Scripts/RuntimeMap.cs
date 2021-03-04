using System;
using UnityEngine;
using UnityEngine.Assertions;
using Random = System.Random;

public class RuntimeMap
{
    private readonly Cell[] _map;
    private readonly int _mapSize;

    public RuntimeMap(float[] heightmap, int mapsize)
    {
        Assert.AreEqual(heightmap.Length, (mapsize+1)*(mapsize+1));
        _mapSize = mapsize;
        _map = new Cell[heightmap.Length];
        for (var i = 0; i < heightmap.Length; i++)
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

    public float ValueAt(int x, int y)
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
        var kernel = new[]
        {
            (-1, -1), (-1, 0), (-1, +1),
            (0, -1), (0, +1),
            (+1, -1), (+1, 0), (+1, +1)
        };

        var offset = 0.01f;

        for (var x = 1; x < _mapSize - 1; x++)
        {
            for (var y = 1; y < _mapSize - 1; y++)
            {
                var middle = y * _mapSize + x;
                var center = _map[middle];
                for (var i = 0; i < 8; i++)
                {
                    var other = _map[(y + kernel[i].Item1) * _mapSize + (x + kernel[i].Item2)];
                    var waterHeight = center.Stone + center.Water;
                    var otherWaterHeight = other.Stone + other.Water;

                    if (center.Water < 0.001f)
                    {
                        center.Water = 0;
                        continue;
                    }

                    if (other.Stone > waterHeight) continue;
                    if (otherWaterHeight > waterHeight) continue;

                    var diff = waterHeight - otherWaterHeight;
                    center.Water -= diff / 2 * offset;
                    other.Water += diff / 2 * offset;
                }
            }
        }
    }
}