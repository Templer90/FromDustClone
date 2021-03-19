using System;
using UnityEngine;

[Serializable]
public class PhysicData
{
    [Header("Stone Settings")] public float StoneHardness = 1f;

    [Header("Sand Settings")] public float SandHardness = 1f;
    public float SandStiffness = 0.001f;
    public float SandViscosity = 0.0000001f;
    public float Sand_dt = 0.2f;
    public float SandCreationHeight = 0.1f;
    public float SandCreationSpeed = 0.0001f;

    [Range(0.0f, 1.0f)] public float SandSlopeRatio = 0.5f;

    [Header("Water Settings")] public float WaterViscosity = 0.0000001f;
    public float EvaporationThreshold = 0.0001f;
    public float WaterSplashRatio = 0.1f;
    public float WaterDiffusion = 0.0f;

    [Header("NavierStokes Settings")] [Min(0)]
    public float dt = 0.2f;

    [Min(0)] public int iter = 1;


    public Kernels UsedKernel = Kernels.VonNeumann;

    public enum Kernels
    {
        VonNeumann,
        Moore,
        VonNeumannRotated
    }

    public readonly (int, int)[] kernelVonNeumann =
    {
        (-1, 0),
        (0, -1), (0, +1),
        (+1, 0)
    };

    public readonly (int, int)[] kernelMoore =
    {
        (-1, -1), (-1, 0), (-1, +1),
        (0, -1), (0, +1),
        (+1, -1), (+1, 0), (+1, +1)
    };

    public readonly (int, int)[] kernelVonNeumannRotated =
    {
        (-1, -1), (-1, +1),

        (+1, -1), (+1, +1)
    };

    public (int, int)[] GETKernel()
    {
        return GETKernel(UsedKernel);
    }

    public (int, int)[] GETKernel(Kernels used)
    {
        return used switch
        {
            Kernels.VonNeumann => kernelVonNeumann,
            Kernels.Moore => kernelMoore,
            Kernels.VonNeumannRotated => kernelVonNeumannRotated,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}