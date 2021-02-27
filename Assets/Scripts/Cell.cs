public class Cell
{
    public float Stone;
    public float Sand;
    public float Water;
    public float Lava;
    
    public float WholeHeight => Stone+Sand+Water+Lava;

    public enum Type
    {
        Stone,
        Sand,
        Water,
        Lava,
    }
}