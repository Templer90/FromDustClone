using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RuntimeMaps;
using UnityEngine;


public class NavierStokes : AbstractMap
{
    private float[] Vx;
    private float[] Vy;
    private float[] Vx0;
    private float[] Vy0;
    private float[] s;
    private float[] density;
    
    public NavierStokes(int heightMapSize, PhysicData physicData,
        IReadOnlyList<float> stoneHeightMap,
        IReadOnlyList<float> waterMap): base(heightMapSize,physicData,stoneHeightMap,waterMap)
    {
        s = new float[MapSizeSquared];
        density = new float[MapSizeSquared];

        Vx = new float[MapSizeSquared];
        Vy = new float[MapSizeSquared];

        Vx0 = new float[MapSizeSquared];
        Vy0 = new float[MapSizeSquared];

        var kernel = Physic.GETKernel();

        for (var i = 0; i < stoneHeightMap.Count; i++)
        {
            Map[i] = new Cell {Stone = stoneHeightMap[i], Water = 0f, Sand = 0f, Lava = 0f};
        }

        for (var x = 1; x < MapSize - 1; x++)
        {
            for (var y = 1; y < MapSize - 1; y++)
            {
                var i = y * MapSize + x;
                density[i] = 0.0f;
                s[i] = 0.0f;

                var lowest = stoneHeightMap[i];
                var dir = (0f, 0f);
                for (var j = 0; j < kernel.Length; j++)
                {
                    var otherIndex = (y + kernel[j].Item1) * MapSize + (x + kernel[j].Item2);
                    var otherStone = stoneHeightMap[otherIndex];

                    if (otherStone < lowest) continue;
                    dir = kernel[j];
                    lowest = otherStone;
                }

                Vx[i] = dir.Item2;
                Vy[i] = dir.Item1;
            }
        }
    }
    
    public new Cell CellAt(int x, int y)
    {
        var i = y * MapSize + x;
        var c = Map[i];
        //c.Stone = c.Stone;
        c.Water = density[i];
        //c.Sand = s[i];
        //c.Lava = c.Lava;
        return c;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IX(int x, int y)
    {
        x = Mathf.Clamp(x, 0, MapSize - 1);
        y = Mathf.Clamp(y, 0, MapSize - 1);
        return x + (y * MapSize);
    }

    private void Diffuse(int b, IList<float> x, IReadOnlyList<float> x0, float diff, float dt)
    {
        var a = dt * diff * (MapSize - 2) * (MapSize - 2);
        lin_solve(b, x, x0, a, 1 + 4 * a);
    }

    private void lin_solve(int b, IList<float> x, IReadOnlyList<float> x0, float a, float c)
    {
        var cRecip = 1.0f / c;
        for (var k = 0; k < Physic.iter; k++)
        {
            for (var j = 1; j < MapSize - 1; j++)
            {
                for (var i = 1; i < MapSize - 1; i++)
                {
                    x[IX(i, j)] = (x0[IX(i, j)]
                                   + a * (x[IX(i + 1, j)]
                                          + x[IX(i - 1, j)]
                                          + x[IX(i, j + 1)]
                                          + x[IX(i, j - 1)]
                                   )) * cRecip;
                }
            }

            set_bnd(b, x);
        }
    }

    private void Project(IList<float> velocX, IList<float> velocY, IList<float> p, float[] div)
    {
        for (var j = 1; j < MapSize - 1; j++)
        {
            for (var i = 1; i < MapSize - 1; i++)
            {
                div[IX(i, j)] = -0.5f * (
                    velocX[IX(i + 1, j)]
                    - velocX[IX(i - 1, j)]
                    + velocY[IX(i, j + 1)]
                    - velocY[IX(i, j - 1)]
                ) / MapSize;
                p[IX(i, j)] = 0;
            }
        }

        set_bnd(0, div);
        set_bnd(0, p);
        lin_solve(0, p, div, 1, 4);

        for (var y = 1; y < MapSize - 1; y++)
        {
            for (var x = 1; x < MapSize - 1; x++)
            {
                velocX[IX(x, y)] -= 0.5f * (p[IX(x + 1, y)] - p[IX(x - 1, y)]) * MapSize;
                velocY[IX(x, y)] -= 0.5f * (p[IX(x, y + 1)] - p[IX(x, y - 1)]) * MapSize;
            }
        }

        set_bnd(1, velocX);
        set_bnd(2, velocY);
    }


    private void Advect(int b, IList<float> d, IReadOnlyList<float> d0, IReadOnlyList<float> velocX,
        IReadOnlyList<float> velocY, float dt)
    {
        float i0, i1, j0, j1;

        var dtx = dt * (MapSize - 2);
        var dty = dt * (MapSize - 2);

        float s0, s1, t0, t1;
        float tmp1, tmp2, x, y;

        float Nfloat = MapSize;
        float ifloat, jfloat;
        int i, j;

        for (j = 1, jfloat = 1; j < MapSize - 1; j++, jfloat++)
        {
            for (i = 1, ifloat = 1; i < MapSize - 1; i++, ifloat++)
            {
                tmp1 = dtx * velocX[IX(i, j)];
                tmp2 = dty * velocY[IX(i, j)];
                x = ifloat - tmp1;
                y = jfloat - tmp2;

                if (x < 0.5f) x = 0.5f;
                if (x > Nfloat + 0.5f) x = Nfloat + 0.5f;
                i0 = Mathf.Floor(x);
                i1 = i0 + 1.0f;

                if (y < 0.5f) y = 0.5f;
                if (y > Nfloat + 0.5f) y = Nfloat + 0.5f;
                j0 = Mathf.Floor(y);
                j1 = j0 + 1.0f;

                s1 = x - i0;
                s0 = 1.0f - s1;
                t1 = y - j0;
                t0 = 1.0f - t1;

                var i0i = (int) i0;
                var i1i = (int) i1;
                var j0i = (int) j0;
                var j1i = (int) j1;

                // DOUBLE CHECK THIS!!!
                d[IX(i, j)] =
                    s0 * (t0 * d0[IX(i0i, j0i)] + t1 * d0[IX(i0i, j1i)]) +
                    s1 * (t0 * d0[IX(i1i, j0i)] + t1 * d0[IX(i1i, j1i)]);
            }
        }

        set_bnd(b, d);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void set_bnd(int b, IList<float> x)
    {
        for (var i = 1; i < MapSize - 1; i++)
        {
            x[IX(i, 0)] = b == 2 ? -x[IX(i, 1)] : x[IX(i, 1)];
            x[IX(i, MapSize - 1)] = b == 2 ? -x[IX(i, MapSize - 2)] : x[IX(i, MapSize - 2)];

            x[IX(0, i)] = b == 1 ? -x[IX(1, i)] : x[IX(1, i)];
            x[IX(MapSize - 1, i)] = b == 1 ? -x[IX(MapSize - 2, i)] : x[IX(MapSize - 2, i)];
        }

        x[IX(0, 0)] = 0.5f * (x[IX(1, 0)] + x[IX(0, 1)]);
        x[IX(0, MapSize - 1)] = 0.5f * (x[IX(1, MapSize - 1)] + x[IX(0, MapSize - 2)]);
        x[IX(MapSize - 1, 0)] = 0.5f * (x[IX(MapSize - 2, 0)] + x[IX(MapSize - 1, 1)]);
        x[IX(MapSize - 1, MapSize - 1)] =
            0.5f * (x[IX(MapSize - 2, MapSize - 1)] + x[IX(MapSize - 1, MapSize - 2)]);
    }

    public override void MapUpdate()
    {
        Diffuse(1, Vx0, Vx, Physic.WaterViscosity, Physic.dt);
        Diffuse(2, Vy0, Vy, Physic.WaterViscosity, Physic.dt);

        Project(Vx0, Vy0, Vx, Vy);

        Advect(1, Vx, Vx0, Vx0, Vy0, Physic.dt);
        Advect(2, Vy, Vy0, Vx0, Vy0, Physic.dt);

        Project(Vx, Vy, Vx0, Vy0);

        Diffuse(0, s, density, Physic.WaterDiffusion, Physic.dt);
        Advect(0, density, s, Vx, Vy, Physic.dt);
    }
}