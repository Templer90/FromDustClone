using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;
using Random = System.Random;


public class SimpleMapUpdate : IRuntimeMap
{
    private Cell[] _map;
    private Cell[] _previousMap;

    private readonly int _mapSize;
    private readonly int _mapSizeSquared;

    public PhysicData Physic { get; }

    public SimpleMapUpdate(int heightMapSize, PhysicData physicData, IReadOnlyList<float> stoneHeightMap,
        IReadOnlyList<float> waterMap)
    {
        Assert.AreEqual(stoneHeightMap.Count, (heightMapSize + 1) * (heightMapSize + 1));
        Assert.AreEqual(stoneHeightMap.Count, waterMap.Count);
        
        Physic = physicData;
        _mapSize = heightMapSize;
        _mapSizeSquared = stoneHeightMap.Count;

        _map = new Cell[stoneHeightMap.Count];
        _previousMap = new Cell[stoneHeightMap.Count];
        for (var i = 0; i < stoneHeightMap.Count; i++)
        {
            _map[i] = new Cell {Stone = stoneHeightMap[i], Water = waterMap[i], Sand = 0f, Lava = 0f};
            _previousMap[i] = new Cell {Stone = stoneHeightMap[i], Water = waterMap[i], Sand = 0f, Lava = 0f};
        }
    }

    private void Swap()
    {
        var tmp = _map;
        _map = _previousMap;
        _previousMap = tmp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ValidCoord(int x, int y)
    {
        var pos = y * _mapSize + x;
        return ValidCoord(pos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ValidCoord(int index)
    {
        return index >= 0 && index < _map.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Cell CellAt(int x, int y)
    {
        return _map[y * _mapSize + x];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Cell CellAt(int index)
    {
        return _map[index];
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

    public void MapUpdate()
    {
        var kernel = Physic.GETKernel();
        var a = Physic.Sand_dt * Physic.SandViscosity * _mapSizeSquared;

        void HandleWater(Cell centerCell, Cell prevCenterCell, int x, int y)
        {
            for (var i = 0; i < kernel.Length; i++)
            {
                var otherIndex = (y + kernel[i].Item1) * _mapSize + (x + kernel[i].Item2);
                var otherCell = _map[otherIndex];

                if (centerCell.Water < Physic.EvaporationThreshold)
                {
                    centerCell.Water = 0;
                    continue;
                }

                var waterHeight = centerCell.Stone + centerCell.Water;
                var otherWaterHeight = otherCell.Stone + otherCell.Water;
                if (otherCell.Stone > waterHeight) continue;
                if (otherWaterHeight > waterHeight) continue;
                var waterDiff = waterHeight - otherWaterHeight;
                centerCell.Water -= waterDiff / 2 * Physic.WaterViscosity;
                otherCell.Water += waterDiff / 2 * Physic.WaterViscosity;
            }
        }

        void HandleSand(Cell centerCell, Cell prevCenterCell, int x, int y)
        {
            if (centerCell.Sand < Physic.SandStiffness) return;

            var sandAcc = 0.0f;
            var foundAcc = 0;
            var indices = new int[kernel.Length];
            for (var i = 0; i < kernel.Length; i++)
            {
                var otherIndex = (y + kernel[i].Item1) * _mapSize + (x + kernel[i].Item2);
                var otherCell = _map[otherIndex];

                sandAcc += otherCell.Sand;
                indices[foundAcc] = otherIndex;
                foundAcc++;
            }

            var shiftedSand = (prevCenterCell.Sand + a * (sandAcc)) / (1 + foundAcc * a);
            centerCell.Sand = shiftedSand;
        }

        void HandleSandDumb(Cell centerCell, Cell prevCenterCell, int x, int y)
        {
            if (centerCell.Sand < Physic.SandStiffness) return;
            for (var i = 0; i < kernel.Length; i++)
            {
                var otherIndex = (y + kernel[i].Item1) * _mapSize + (x + kernel[i].Item2);
                var otherCell = _map[otherIndex];
                var sandDiff = (centerCell.Stone + centerCell.Sand) - (otherCell.Stone + otherCell.Sand);
                var delta = sandDiff * Physic.SandHardness;

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
                HandleWater(centerCell, prevCenterCell, x, y);
            }
        }

        Swap();
    }
}