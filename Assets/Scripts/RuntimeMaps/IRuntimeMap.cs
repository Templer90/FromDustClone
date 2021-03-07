public interface IRuntimeMap
{
    bool ValidCoord(int x, int y);
    Cell CellAt(int x, int y);
    float WholeAt(int x, int y);
    float ValueAt(int x, int y, Cell.Type type);
    void Add(int x, int y, Cell.Type type, float amount);
    void SimpleSmooth();
    void MapUpdate();
    PhysicData physic { get; }
}