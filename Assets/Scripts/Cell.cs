public class Cell
{
    public float Stone;
    public float Sand;
    public float Water;
    public float Lava;
    
    public float WholeHeight => Stone+Sand+Water+Lava;
    public float LithoHeight => Stone+Sand;

    public enum Type
    {
        Stone,
        Sand,
        Water,
        Lava,
    }
}

public struct AdditionalVertexData
{
    public float Sand;
    public float Water;
    public float Lava;
    public float a;
}