using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;


public class CellBasedMapUpdate : IRuntimeMap
{
    private readonly Cell[] _map;

    private readonly int _mapSize;
    private readonly int _mapSizeSquared;

    public PhysicData Physic { get; }

    public CellBasedMapUpdate(int heightMapSize, PhysicData physicData, IReadOnlyList<float> stoneHeightMap,
        IReadOnlyList<float> waterMap)
    {
        Assert.AreEqual(stoneHeightMap.Count, (heightMapSize + 1) * (heightMapSize + 1));
        Assert.AreEqual(stoneHeightMap.Count, waterMap.Count);

        Physic = physicData; //new PhysicData {SandSlopeRatio = 0.001f, SandStiffness = 0.1f};
        _mapSize = heightMapSize;
        _mapSizeSquared = stoneHeightMap.Count;

        _map = new Cell[stoneHeightMap.Count];
        for (var i = 0; i < stoneHeightMap.Count; i++)
        {
            _map[i] = new Cell {Stone = stoneHeightMap[i], Water = waterMap[i], Sand = 0f, Lava = 0f};
        }
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float WholeAt(int x, int y)
    {
        return CellAt(x, y).WholeHeight;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                cell.Stone = Mathf.Max(cell.Stone + amount, 0);
                break;
            case Cell.Type.Sand:
                cell.Sand = Mathf.Max(cell.Sand + amount, 0);
                break;
            case Cell.Type.Water:
                cell.Water = Mathf.Max(cell.Water + amount, 0);
                break;
            case Cell.Type.Lava:
                cell.Lava = Mathf.Max(cell.Lava + amount, 0);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public void MapUpdate()
    {
        var kernel = Physic.GETKernel();
        var a = Physic.Sand_dt * Physic.SandViscosity * _mapSizeSquared;

        void HandleWater(Cell centerCell, int x, int y)
        {
            if (centerCell.Water <= 0.0f)
            {
                centerCell.Water = 0.0f;
                return;
            }

            var currentTotalHeight = centerCell.LithoHeight + centerCell.Water;
            var lowest = currentTotalHeight;

            var smallerCell = centerCell;
            var index = -1;
            for (var i = 0; i < kernel.Length; i++)
            {
                var otherIndex = (y + kernel[i].Item1) * _mapSize + (x + kernel[i].Item2);
                var otherCell = _map[otherIndex];

                var totalNew = otherCell.LithoHeight + otherCell.Water;
                if (totalNew > lowest) continue;
                index = i;
                lowest = totalNew;
                smallerCell = otherCell;
            }

            centerCell.WaterFlow.x = 0;
            centerCell.WaterFlow.y = 0;

            //found smaller one
            if (index == -1) return;

            //var lithoDiff = centerCell.LithoHeight - smallerCell.LithoHeight;
            //var sandDiff = centerCell.Stone - smallerCell.Water;
            var heightDiff = currentTotalHeight - lowest;
            var w = centerCell.Water;

            var (xFlow, yFlow) = kernel[index];
            centerCell.WaterFlow.x = yFlow * heightDiff;
            centerCell.WaterFlow.y = xFlow * heightDiff;

            if (centerCell.LithoHeight > lowest)
            {
                //splash randomly?
                centerCell.Water -= w * Physic.WaterSplashRatio;
                smallerCell.Water += w * ( /*1.0f - */ Physic.WaterSplashRatio);
            }
            else
            {
                centerCell.Water -= heightDiff * Physic.WaterViscosity;
                smallerCell.Water += heightDiff * ( /*1.0f - */ Physic.WaterViscosity);
            }
        }

        void HandleLava(Cell centerCell, int x, int y)
        {
            centerCell.Lava -= Physic.LavaCooling;

            if (centerCell.Lava <= 0.0f)
            {
                centerCell.Lava = 0.0f;
                return;
            }

            if (centerCell.Water > 0.0f && centerCell.Lava > 0.0f)
            {
                var diff = centerCell.Water - centerCell.Lava;
                if (centerCell.Water > centerCell.Lava)
                {
                    centerCell.Water -= diff;
                    centerCell.Lava = 0.0f;
                    centerCell.Stone += diff;
                    return;
                }
                else
                {
                    centerCell.Water = 0.0f;
                    centerCell.Lava -= -diff;
                    centerCell.Stone += -diff;
                }
            }


            centerCell.Stone += centerCell.Sand;
            centerCell.Sand = 0;

            var currentTotalHeight = centerCell.LithoHeight + centerCell.Lava;
            var lowest = currentTotalHeight;

            for (var k = 0; k < 2; k++)
            {
                var smallerCell = centerCell;
                var index = -1;
                for (var i = 0; i < kernel.Length; i++)
                {
                    var otherIndex = (y + kernel[i].Item1) * _mapSize + (x + kernel[i].Item2);
                    var otherCell = _map[otherIndex];

                    var totalNew = otherCell.LithoHeight + otherCell.Lava;
                    if (totalNew > lowest) continue;
                    index = i;
                    lowest = totalNew;
                    smallerCell = otherCell;
                }

                //found smaller one
                if (index == -1) return;
                var hdiff = currentTotalHeight - lowest;
                var w = centerCell.Lava;

                if (centerCell.LithoHeight > lowest)
                {
                    //splash randomly?
                    centerCell.Lava = w * Physic.LavaSplashRatio;
                    smallerCell.Lava += w * (1.0f - Physic.LavaSplashRatio);
                }
                else
                {
                    centerCell.Lava -= hdiff * Physic.LavaViscosity;
                    smallerCell.Lava += hdiff * ( /*1.0f - */ Physic.LavaViscosity);
                }
            }
        }

        void HandleSand(Cell centerCell, int x, int y)
        {
            if (centerCell.Sand < 0.0f)
            {
                centerCell.Sand = 0.0f;
                return;
            }

            if (centerCell.Sand < Physic.SandCreationHeight)
            {
                //centerCell.Sand += Physic.SandCreationSpeed;
            }

            if (centerCell.Sand < Physic.SandStiffness) return;

            var ratio = Physic.SandSlopeRatio;
            for (var i = 0; i < kernel.Length; i++)
            {
                if (centerCell.Sand < Physic.SandStiffness) continue;
                var otherIndex = (y + kernel[i].Item1) * _mapSize + (x + kernel[i].Item2);
                var otherCell = _map[otherIndex];

                var centerHeight = centerCell.Stone + centerCell.Sand;
                var otherHeight = otherCell.Stone + otherCell.Sand;

                if (otherHeight >= centerHeight) continue;
                var hdiff = centerHeight - otherHeight;
                centerCell.Sand -= hdiff * ratio;
                otherCell.Sand += hdiff * ratio;
            }
        }

        for (var x = 1; x < _mapSize - 1; x++)
        {
            for (var y = 1; y < _mapSize - 1; y++)
            {
                var middleIndex = y * _mapSize + x;
                var centerCell = _map[middleIndex];

                HandleSand(centerCell, x, y);
                HandleWater(centerCell, x, y);
                HandleLava(centerCell, x, y);
            }
        }
    }
}