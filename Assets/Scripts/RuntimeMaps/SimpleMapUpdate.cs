using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RuntimeMaps;
using UnityEngine.Assertions;

public class SimpleMapUpdate : AbstractMap
{
    private Cell[] _previousMap;
    private Cell[] _map;

    public SimpleMapUpdate(int heightMapSize, PhysicData physicData, 
        IReadOnlyList<float> stoneHeightMap,
        IReadOnlyList<float> sandHeightMap,
        IReadOnlyList<float> waterMap) : base(heightMapSize, physicData, stoneHeightMap, sandHeightMap, waterMap)
    {
        Assert.AreEqual(stoneHeightMap.Count, heightMapSize  * heightMapSize, "Ensure that the the Stone Heightmap size is the square of the heightMapSize");
        Assert.AreEqual(stoneHeightMap.Count, waterMap.Count ,"Ensure that the Water and the Stone Heightmaps are in equal Size");
        
        _map = new Cell[stoneHeightMap.Count];
        _previousMap = new Cell[stoneHeightMap.Count];
        for (var i = 0; i < stoneHeightMap.Count; i++)
        {
            Map[i] = new Cell {Stone = stoneHeightMap[i], Water = waterMap[i], Sand = sandHeightMap[i], Lava = 0f};
            _previousMap[i] = new Cell {Stone = stoneHeightMap[i], Water = waterMap[i], Sand = sandHeightMap[i], Lava = 0f};
        }
    }

    private void Swap()
    {
        var tmp = Map;
        _map = _previousMap;
        _previousMap = tmp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new bool ValidCoord(int x, int y)
    {
        var pos = y * MapSize + x;
        return ValidCoord(pos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new bool ValidCoord(int index)
    {
        return index >= 0 && index < _map.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new Cell CellAt(int x, int y)
    {
        return _map[y * MapSize + x];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new Cell CellAt(int index)
    {
        return _map[index];
    }


    public override void MapUpdate()
    {
        var kernel = Physic.GETKernel();
        var a = Physic.Sand_dt * Physic.SandViscosity * MapSizeSquared;

        void HandleWater(Cell centerCell, Cell prevCenterCell, int x, int y)
        {
            for (var i = 0; i < kernel.Length; i++)
            {
                var otherIndex = (y + kernel[i].Item1) * MapSize + (x + kernel[i].Item2);
                var otherCell = Map[otherIndex];

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
                var otherIndex = (y + kernel[i].Item1) * MapSize + (x + kernel[i].Item2);
                var otherCell = Map[otherIndex];

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
                var otherIndex = (y + kernel[i].Item1) * MapSize + (x + kernel[i].Item2);
                var otherCell = Map[otherIndex];
                var sandDiff = (centerCell.Stone + centerCell.Sand) - (otherCell.Stone + otherCell.Sand);
                var delta = sandDiff * Physic.SandHardness;

                centerCell.Sand -= delta;
                otherCell.Sand += delta;
            }
        }

        for (var x = 1; x < MapSize - 1; x++)
        {
            for (var y = 1; y < MapSize - 1; y++)
            {
                var middleIndex = y * MapSize + x;
                var centerCell = Map[middleIndex];
                var prevCenterCell = _previousMap[y * MapSize + x];

                HandleSand(centerCell, prevCenterCell, x, y);
                HandleWater(centerCell, prevCenterCell, x, y);
            }
        }

        Swap();
    }
}