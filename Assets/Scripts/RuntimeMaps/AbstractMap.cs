using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;

namespace RuntimeMaps
{
    public abstract class AbstractMap
    {
        public PhysicData Physic { get; }

        protected readonly Cell[] Map;

        protected readonly int MapSize;
        protected readonly int MapSizeSquared;

        public static PhysicData DefaultPhysics()
        {
            return new PhysicData();
        }

        protected AbstractMap(int heightMapSize, PhysicData physicData,
            IReadOnlyCollection<float> stoneHeightMap,
            IReadOnlyCollection<float> waterMap)
        {
            Assert.AreEqual(stoneHeightMap.Count, heightMapSize * heightMapSize,
                "Ensure that the the Stone Heightmap size is the square of the heightMapSize");
            Assert.AreEqual(stoneHeightMap.Count, waterMap.Count,
                "Ensure that the Water and the Stone Heightmaps are in equal Size");

            Physic = physicData; //new PhysicData {SandSlopeRatio = 0.001f, SandStiffness = 0.1f};
            MapSize = heightMapSize;
            MapSizeSquared = stoneHeightMap.Count;
            Map = new Cell[stoneHeightMap.Count];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ValidCoord(int x, int y)
        {
            var pos = y * MapSize + x;
            return ValidCoord(pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ValidCoord(int index)
        {
            return index >= 0 && index < Map.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Cell CellAt(int x, int y)
        {
            return Map[y * MapSize + x];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Cell CellAt(int index)
        {
            return Map[index];
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

        public abstract void MapUpdate();
    }
}