using System.Collections.Generic;

public class CellBasedMapUpdate : AbstractMap
{
    public CellBasedMapUpdate(int heightMapSize, PhysicData physicData, IReadOnlyList<float> stoneHeightMap,
        IReadOnlyList<float> waterMap) : base(heightMapSize, physicData, stoneHeightMap, waterMap)
    {
        for (var i = 0; i < stoneHeightMap.Count; i++)
        {
            Map[i] = new Cell {Stone = stoneHeightMap[i], Water = waterMap[i], Sand = 0f, Lava = 0f};
        }
    }

    public override void MapUpdate()
    {
        var kernel = Physic.GETKernel();
        var a = Physic.Sand_dt * Physic.SandViscosity * MapSizeSquared;

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
                var otherIndex = (y + kernel[i].Item1) * MapSize + (x + kernel[i].Item2);
                var otherCell = Map[otherIndex];

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
                    var otherIndex = (y + kernel[i].Item1) * MapSize + (x + kernel[i].Item2);
                    var otherCell = Map[otherIndex];

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
                var otherIndex = (y + kernel[i].Item1) * MapSize + (x + kernel[i].Item2);
                var otherCell = Map[otherIndex];

                var centerHeight = centerCell.Stone + centerCell.Sand;
                var otherHeight = otherCell.Stone + otherCell.Sand;

                if (otherHeight >= centerHeight) continue;
                var hdiff = centerHeight - otherHeight;
                centerCell.Sand -= hdiff * ratio;
                otherCell.Sand += hdiff * ratio;
            }
        }

        for (var x = 1; x < MapSize - 1; x++)
        {
            for (var y = 1; y < MapSize - 1; y++)
            {
                var middleIndex = y * MapSize + x;
                var centerCell = Map[middleIndex];

                HandleSand(centerCell, x, y);
                HandleWater(centerCell, x, y);
                HandleLava(centerCell, x, y);
            }
        }
    }
}