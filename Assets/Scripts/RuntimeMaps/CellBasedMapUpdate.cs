using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Assertions;
using Random = System.Random;


public class CellBasedMapUpdate : IRuntimeMap
{
    private Cell[] _map;

    private readonly int _mapSize;
    private readonly int _mapSizeSquared;

    public PhysicData physic { get; }

    public CellBasedMapUpdate(IReadOnlyList<float> heightmap, int heightMapSize, PhysicData physicData)
    {
        Assert.AreEqual(heightmap.Count, (heightMapSize + 1) * (heightMapSize + 1));
        physic = physicData;
        _mapSize = heightMapSize;
        _mapSizeSquared = heightmap.Count;

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
        var a = physic.Sand_dt * physic.SandViscosity * _mapSizeSquared;

        void HandleWater(Cell centerCell, int x, int y)
        {
            if (centerCell.Water < 0.0f)
            {
                centerCell.Water = 0.0f;
                return;
            }

            var currentTotalHeight = centerCell.Stone + centerCell.Water;
            var lowest=currentTotalHeight;

            var smallerCell=centerCell;
            var index = -1;
            for (var i = 0; i < kernel.Length; i++)
            {
                var otherIndex = (y + kernel[i].Item1) * _mapSize + (x + kernel[i].Item2);
                var otherCell = _map[otherIndex];

                var totalNew = otherCell.Stone + otherCell.Water;
                if (totalNew > lowest) continue;
                index = i;
                lowest = totalNew;
                smallerCell = otherCell;
            }
            
            //found smaller one
            if (index == -1) return;
            var m=kernel[index];
            var stoneDiff=centerCell.Stone-smallerCell.Stone;
            
            var sandDiff=centerCell.Water-smallerCell.Water;
            var hdiff=currentTotalHeight-lowest;
            var w = centerCell.Water;


            var ratio=0.5f;
            if (centerCell.Stone > lowest)
            {
                //splash randomly?
                var splashRatio = 0.1f;
                centerCell.Water = w * splashRatio;
                smallerCell.Water += w * (1.0f - splashRatio);
            }
            else
            {
                centerCell.Water -= hdiff * ratio;
                smallerCell.Water += hdiff * (1.0f - ratio);
            }

            //Coded for water
            /*float hardness=envo.stone.getHardness(x, y);
            float cutoff=1;
            if (hdiff>cutoff) {
                float s=envo.stone.get(x, y);
                envo.stone.sub(x, y, (stoneDiff*hardness));
                envo.stone.add(x+m[0], y+m[1], (stoneDiff*hardness));
            }*/
        }
        
        void HandleSand(Cell centerCell, int x, int y)
        {
            if (centerCell.Sand < 0.0f)
            {
                centerCell.Sand = 0.0f;
                return;
            }
            if (centerCell.Sand < physic.SandStiffness) return;

            var currentTotalHeight = centerCell.Stone + centerCell.Sand;
            var lowest=currentTotalHeight;

            var smallerCell=centerCell;
            var index = -1;
            for (var i = 0; i < kernel.Length; i++)
            {
                var otherIndex = (y + kernel[i].Item1) * _mapSize + (x + kernel[i].Item2);
                var otherCell = _map[otherIndex];

                var totalNew = otherCell.Stone + otherCell.Sand;
                if (!(totalNew <= lowest)) continue;
                index = i;
                lowest = totalNew;
                smallerCell = otherCell;
            }
            
            //found smaller one
            if (index == -1) return;
            var m=kernel[index];
            var stoneDiff=centerCell.Stone-smallerCell.Stone;
            
            var sandDiff=centerCell.Sand-smallerCell.Sand;
            var hdiff=currentTotalHeight-lowest;
            var w = centerCell.Sand;


            var ratio=physic.SandSlopeRatio;
            if (centerCell.Stone>lowest) {
                //splash randomly?
                var splashRatio=0.1f;
                centerCell.Sand = w * splashRatio;
                smallerCell.Sand = w * (1.0f - splashRatio);
            } else {
                centerCell.Sand -= hdiff * ratio;
                smallerCell.Sand += hdiff*(1.0f-ratio);
            }

            //Coded for water
            /*float hardness=envo.stone.getHardness(x, y);
            float cutoff=1;
            if (hdiff>cutoff) {
                float s=envo.stone.get(x, y);
                envo.stone.sub(x, y, (stoneDiff*hardness));
                envo.stone.add(x+m[0], y+m[1], (stoneDiff*hardness));
            }*/
        }

        for (var x = 1; x < _mapSize - 1; x++)
        {
            for (var y = 1; y < _mapSize - 1; y++)
            {
                var middleIndex = y * _mapSize + x;
                var centerCell = _map[middleIndex];

                //HandleSand(centerCell, x, y);
                HandleWater(centerCell,x, y);
            }
        }
    }
}