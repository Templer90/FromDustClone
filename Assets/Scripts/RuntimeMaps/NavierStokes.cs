using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Assertions;
using Random = System.Random;


public class NavierStokes : IRuntimeMap
{
    private float visc;
    private float diff;
    private float dt;
    private float[] Vx;
    private float[] Vy;
    private float[] Vx0;
    private float[] Vy0;
    private float[] s;
    private float[] density;
    private int iter = 16;

    private readonly int _mapSize;
    private readonly int _mapSizeSquared;

    public PhysicData Physic { get; }

    public NavierStokes(int heightMapSize, PhysicData physicData,
        IReadOnlyList<float> stoneHeightMap,
        IReadOnlyList<float> waterMap)
    {
        Assert.AreEqual(stoneHeightMap.Count, (heightMapSize + 1) * (heightMapSize + 1));
        Assert.AreEqual(stoneHeightMap.Count, waterMap.Count);

        Physic = physicData;
        _mapSize = heightMapSize;
        _mapSizeSquared = stoneHeightMap.Count;

        dt = physicData.dt;
        diff = physicData.WaterDiffusion;
        visc = physicData.WaterViscosity;

        s = new float[_mapSizeSquared];
        density = new float[_mapSizeSquared];

        Vx = new float[_mapSizeSquared];
        Vy = new float[_mapSizeSquared];

        Vx0 = new float[_mapSizeSquared];
        Vy0 = new float[_mapSizeSquared];

        for (var i = 0; i < stoneHeightMap.Count; i++)
        {
            density[i] = stoneHeightMap[i];
            //_previousMap[i] = new Cell {Stone = stoneHeightMap[i], Water = waterMap[i], Sand = 0f, Lava = 0f};
        }
    }

    public bool ValidCoord(int x, int y)
    {
        var pos = y * _mapSize + x;
        return pos >= 0 && pos < _mapSizeSquared;
    }

    public Cell CellAt(int x, int y)
    {
        var i = y * _mapSize + x;
        return new Cell {Stone = density[i], Water = 0f, Sand = 0f, Lava = 0f};
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
        var i = y * _mapSize + x;
        switch (type)
        {
            case Cell.Type.Stone:
                density[i]+=amount;
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

    private int IX(int x, int y)
    {
        x = Mathf.Clamp(x, 0, _mapSize - 1);
        y = Mathf.Clamp(y, 0, _mapSize - 1);
        return x + (y * _mapSize);
    }

    private void diffuse(int b, float[] x, float[] x0, float diff, float dt)
    {
        float a = dt * diff * (_mapSize - 2) * (_mapSize - 2);
        lin_solve(b, x, x0, a, 1 + 4 * a);
    }

    private void lin_solve(int b, float[] x, float[] x0, float a, float c)
    {
        float cRecip = 1.0f / c;
        for (int k = 0; k < iter; k++)
        {
            for (int j = 1; j < _mapSize - 1; j++)
            {
                for (int i = 1; i < _mapSize - 1; i++)
                {
                    x[IX(i, j)] =
                        (x0[IX(i, j)]
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

    private void project(float[] velocX, float[] velocY, float[] p, float[] div)
    {
        for (int j = 1; j < _mapSize - 1; j++)
        {
            for (int i = 1; i < _mapSize - 1; i++)
            {
                div[IX(i, j)] = -0.5f * (
                    velocX[IX(i + 1, j)]
                    - velocX[IX(i - 1, j)]
                    + velocY[IX(i, j + 1)]
                    - velocY[IX(i, j - 1)]
                ) / _mapSize;
                p[IX(i, j)] = 0;
            }
        }

        set_bnd(0, div);
        set_bnd(0, p);
        lin_solve(0, p, div, 1, 4);

        for (int j = 1; j < _mapSize - 1; j++)
        {
            for (int i = 1; i < _mapSize - 1; i++)
            {
                velocX[IX(i, j)] -= 0.5f * (p[IX(i + 1, j)]
                                            - p[IX(i - 1, j)]) * _mapSize;
                velocY[IX(i, j)] -= 0.5f * (p[IX(i, j + 1)]
                                            - p[IX(i, j - 1)]) * _mapSize;
            }
        }

        set_bnd(1, velocX);
        set_bnd(2, velocY);
    }


    private void advect(int b, float[] d, float[] d0, float[] velocX, float[] velocY, float dt)
    {
        float i0, i1, j0, j1;

        float dtx = dt * (_mapSize - 2);
        float dty = dt * (_mapSize - 2);

        float s0, s1, t0, t1;
        float tmp1, tmp2, x, y;

        float Nfloat = _mapSize;
        float ifloat, jfloat;
        int i, j;

        for (j = 1, jfloat = 1; j < _mapSize - 1; j++, jfloat++)
        {
            for (i = 1, ifloat = 1; i < _mapSize - 1; i++, ifloat++)
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

                int i0i = (int) i0;
                int i1i = (int) i1;
                int j0i = (int) j0;
                int j1i = (int) j1;

                // DOUBLE CHECK THIS!!!
                d[IX(i, j)] =
                    s0 * (t0 * d0[IX(i0i, j0i)] + t1 * d0[IX(i0i, j1i)]) +
                    s1 * (t0 * d0[IX(i1i, j0i)] + t1 * d0[IX(i1i, j1i)]);
            }
        }

        set_bnd(b, d);
    }


    private void set_bnd(int b, float[] x)
    {
        for (int i = 1; i < _mapSize - 1; i++)
        {
            x[IX(i, 0)] = b == 2 ? -x[IX(i, 1)] : x[IX(i, 1)];
            x[IX(i, _mapSize - 1)] = b == 2 ? -x[IX(i, _mapSize - 2)] : x[IX(i, _mapSize - 2)];
        }

        for (int j = 1; j < _mapSize - 1; j++)
        {
            x[IX(0, j)] = b == 1 ? -x[IX(1, j)] : x[IX(1, j)];
            x[IX(_mapSize - 1, j)] = b == 1 ? -x[IX(_mapSize - 2, j)] : x[IX(_mapSize - 2, j)];
        }

        x[IX(0, 0)] = 0.5f * (x[IX(1, 0)] + x[IX(0, 1)]);
        x[IX(0, _mapSize - 1)] = 0.5f * (x[IX(1, _mapSize - 1)] + x[IX(0, _mapSize - 2)]);
        x[IX(_mapSize - 1, 0)] = 0.5f * (x[IX(_mapSize - 2, 0)] + x[IX(_mapSize - 1, 1)]);
        x[IX(_mapSize - 1, _mapSize - 1)] =
            0.5f * (x[IX(_mapSize - 2, _mapSize - 1)] + x[IX(_mapSize - 1, _mapSize - 2)]);
    }


    public void MapUpdate()
    {
        diffuse(1, Vx0, Vx, visc, dt);
        diffuse(2, Vy0, Vy, visc, dt);

        project(Vx0, Vy0, Vx, Vy);

        advect(1, Vx, Vx0, Vx0, Vy0, dt);
        advect(2, Vy, Vy0, Vx0, Vy0, dt);

        project(Vx, Vy, Vx0, Vy0);

        diffuse(0, s, density, diff, dt);
        advect(0, density, s, Vx, Vy, dt);
    }
}