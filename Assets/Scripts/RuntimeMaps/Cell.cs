using System;
using System.Numerics;

public class Cell
{
    public float Stone;
    public float Sand;
    public float Water;
    public float Lava;

    public Vector2 StoneFlow;
    public Vector2 SandFlow;
    public Vector2 WaterFlow;
    public Vector2 LavaFlow;

    public float WholeHeight => Stone + Sand + Water + Lava;
    public float LithoHeight => Stone + Sand;

    public float getValue(Type type)
    {
        return type switch
        {
            Type.Stone => Stone,
            Type.Sand => Sand,
            Type.Water => Water,
            Type.Lava => Lava,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public Vector2 getFlowDir(Type type)
    {
        return type switch
        {
            Type.Stone => StoneFlow,
            Type.Sand => SandFlow,
            Type.Water => WaterFlow,
            Type.Lava => LavaFlow,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public enum Type
    {
        Stone,
        Sand,
        Water,
        Lava,
    }
}