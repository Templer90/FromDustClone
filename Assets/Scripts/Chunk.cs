using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public partial class Chunk : MonoBehaviour
{
    [Header("Mesh Settings")] public Vector2Int coords = new Vector2Int(0, 0);
    public int mapSize;
    public float scale;
    public float elevationScale = 10;
    public Bounds bounds;
    // ReSharper disable once InconsistentNaming
    public LODTriangles.LOD LOD = LODTriangles.LOD.LOD0;

    public MeshData[] meshes =
    {
        new MeshData {Type = Cell.Type.Stone},
        new MeshData {Type = Cell.Type.Sand},
        new MeshData {Type = Cell.Type.Water},
        new MeshData {Type = Cell.Type.Lava}
    };

    // Internal
    private IRuntimeMap _map;
    private int _mainSize; //Side length of the whole Map


    public void Initialize(int x, int y, IRuntimeMap mainMap, int mainMapSize, int chunkSize, float scaling,
        float elevationScaling)
    {
        scale = scaling;
        coords.x = x;
        coords.y = y;
        _map = mainMap;
        _mainSize = mainMapSize;
        mapSize = chunkSize;
        elevationScale = elevationScaling;

        var pos = new Vector3(x * scale, 0, y * scale);
        var halfScale = scale / 2.0f;
        var transform1 = transform;
        transform1.name = "Chunk(" + x + "," + y + ")";
        transform1.position = pos;

        bounds = new Bounds(pos + new Vector3(halfScale, 0, halfScale), new Vector3(halfScale, 50, halfScale));


        //Ensure that meshes is right
        var tmpMeshes = new MeshData[4];
        foreach (var t in meshes)
        {
            tmpMeshes[(int) t.Type] = t;
        }

        meshes = tmpMeshes;
    }

    public void Start()
    {
        AssignMeshComponents(meshes[(int) Cell.Type.Stone]);
        AssignMeshComponents(meshes[(int) Cell.Type.Water]);
        AssignMeshComponents(meshes[(int) Cell.Type.Lava]);

        var meshStone = meshes[(int) Cell.Type.Stone].meshFilter.mesh;
        meshes[(int) Cell.Type.Stone].vertices = meshStone.vertices;
        meshes[(int) Cell.Type.Stone].color = meshStone.colors;
        meshes[(int) Cell.Type.Stone].uv5 = meshStone.uv5;

        var meshWater = meshes[(int) Cell.Type.Water].meshFilter.mesh;
        meshes[(int) Cell.Type.Water].vertices = meshWater.vertices;
        meshes[(int) Cell.Type.Water].color = meshWater.colors;
        meshes[(int) Cell.Type.Water].uv5 = meshWater.uv5;

        var meshLava = meshes[(int) Cell.Type.Lava].meshFilter.mesh;
        meshes[(int) Cell.Type.Lava].vertices = meshLava.vertices;
        meshes[(int) Cell.Type.Lava].color = meshLava.colors;
        meshes[(int) Cell.Type.Lava].uv5 = meshLava.uv5;
    }

    public void ConstructMeshes()
    {
        if (mapSize % 12 != 0)
        {
            Debug.Log($"MeshSize ({mapSize}) not divisible by 3 and 4");
        }

        ConstructMesh_Internal(meshes[(int) Cell.Type.Stone]);
        ConstructMesh_Internal(meshes[(int) Cell.Type.Water]);
        ConstructMesh_Internal(meshes[(int) Cell.Type.Lava]);
    }

    public void Hide()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
        else
        {
            //Stuff
        }
    }

    public void Show()
    {
        if (gameObject.activeSelf)
        {
            UpdateMeshes();
        }
        else
        {
            ConstructMesh_Internal(meshes[(int) Cell.Type.Stone]);
            ConstructMesh_Internal(meshes[(int) Cell.Type.Water]);
            ConstructMesh_Internal(meshes[(int) Cell.Type.Lava]);

            AssignMeshComponents(meshes[(int) Cell.Type.Stone]);
            AssignMeshComponents(meshes[(int) Cell.Type.Water]);
            AssignMeshComponents(meshes[(int) Cell.Type.Lava]);
            gameObject.SetActive(true);

            /*UpdateMeshes();
            var meshData = meshes[(int) Cell.Type.Stone];
            float GETFunc(int x, int y)
            {
                return GETCell(x, y, meshData);
            }
            meshData.lod.RecalculateNormals(meshData.meshFilter.mesh, GETFunc);
            gameObject.SetActive(true);*/
        }
    }

    private (float, bool) StoneFunc(Cell currentCell)
    {
        return (currentCell.LithoHeight * elevationScale, true);
    }

    private (float, bool) WaterFunc(Cell currentCell)
    {
        return currentCell.Water <= 0.0001f ? (0.0f, false) : ((currentCell.LithoHeight + currentCell.Water) * elevationScale, true);
    }

    private (float, bool) LavaFunc(Cell currentCell)
    {
        return currentCell.Lava <= 0.0001f ? (0.0f, false) : ((currentCell.LithoHeight + currentCell.Lava) * elevationScale, true);
    }

    public void UpdateMeshes()
    {
        if (!gameObject.activeSelf) return;

        var stoneMeshData = meshes[(int) Cell.Type.Stone];
        var waterMeshData = meshes[(int) Cell.Type.Water];
        var lavaMeshData = meshes[(int) Cell.Type.Lava];

        stoneMeshData.lod.SwitchTriangles(meshes[(int) Cell.Type.Stone].meshFilter.mesh, LOD);

        var numChunks = (_mainSize / mapSize);
        var xSize = mapSize + ((coords.x < numChunks) ? 1 : 0);
        var ySize = mapSize + ((coords.y < numChunks) ? 1 : 0);
        var maxSize = mapSize + 1;

        var offsetX = coords.x * mapSize;
        var offsetY = coords.y * mapSize;

        var step = LOD switch
        {
            LODTriangles.LOD.LOD0 => 1,
            LODTriangles.LOD.LOD1 => 2,
            LODTriangles.LOD.LOD2 => 4,
            _ => throw new ArgumentOutOfRangeException()
        };

        var waterVisibility = false;
        var lavaVisibility = false;
        for (var x = 0; x < xSize; x += step)
        {
            for (var y = 0; y < ySize; y += step)
            {
                var meshMapIndex = y * maxSize + x;
                var currentCell = _map.CellAt(offsetX + x, offsetY + y);

                stoneMeshData.color[meshMapIndex].r = currentCell.Sand;
                var (stoneHeight, _) = StoneFunc(currentCell);
                stoneMeshData.vertices[meshMapIndex].y = stoneHeight;


                waterMeshData.color[meshMapIndex].r = currentCell.Water;
                var (waterHeight, visibleWater) = WaterFunc(currentCell);
                waterMeshData.vertices[meshMapIndex].y = waterHeight;
                waterVisibility |= visibleWater;


                lavaMeshData.color[meshMapIndex].r = currentCell.Stone;
                var (lavaHeight, visibleLava) = LavaFunc(currentCell);
                lavaMeshData.vertices[meshMapIndex].y = lavaHeight;
                lavaVisibility |= visibleLava;
            }
        }
        
        stoneMeshData.RecalculateAndRefresh(_map, StoneFunc);

        if (waterVisibility)
        {
            waterMeshData.holder.SetActive(true);
            waterMeshData.RecalculateAndRefresh(_map, WaterFunc);
        }
        else
        {
            waterMeshData.holder.SetActive(false);
        }

        if (lavaVisibility)
        {
            meshes[(int) Cell.Type.Lava].holder.SetActive(true);
            var meshLava = meshes[(int) Cell.Type.Lava].meshFilter.mesh;
            meshLava.vertices = meshes[(int) Cell.Type.Lava].vertices;
            meshLava.colors = meshes[(int) Cell.Type.Lava].color;
            meshLava.uv5 = meshes[(int) Cell.Type.Lava].uv5;
        }
        else
        {
            meshes[(int) Cell.Type.Lava].holder.SetActive(false);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float GETVal(int x, int y, Cell.Type type)
    {
        return _map.ValueAt((int) coords.x * mapSize + x, (int) coords.y * mapSize + y, type);
    }

    private float GETCell(int x, int y, Cell.Type type)
    {
        var xPos = (int) coords.x * mapSize + (x == -1 ? 0 : x);
        var yPos = (int) coords.y * mapSize + (y == -1 ? 0 : y);
        try
        {
            return _map.ValidCoord(xPos, yPos) ? _map.ValueAt(xPos, yPos, type) : float.NaN;
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return float.NaN;
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void ConstructMesh_Internal(MeshData meshData)
    {
        (float, int) GETFunc(int x, int y)
        {
            var pos = ((int) coords.y * mapSize + (y == -1 ? 0 : y)) * _mainSize + (int) coords.x * mapSize +
                      (x == -1 ? 0 : x);

            return (GETCell(x, y, meshData.Type) * elevationScale, _map.ValidCoord(pos) ? pos : 0);
        }

        var generatedRawMeshData = MeshGenerator.GenerateTerrainMesh(GETFunc, mapSize, scale);

        AssignMeshComponents(meshData);
        var mesh = generatedRawMeshData.CreateMesh();
        mesh.name = coords.x + " " + coords.y;
        meshData.meshFilter.sharedMesh = mesh;
        meshData.lod = generatedRawMeshData.GenLODTriangles();
        meshData.RecalculateNormalsSharedMesh(_map, StoneFunc);

        if (!meshData.holder.transform.GetComponent<MeshCollider>()) return;
        var coll = meshData.holder.transform.gameObject.GetComponent<MeshCollider>();
        coll.sharedMesh = mesh;
    }

    private void AssignMeshComponents(MeshData meshData)
    {
        var holder = meshData.holder;
        // Find/creator mesh holder object in children
        var meshHolder = holder.transform;
        if (meshHolder == null)
        {
            meshHolder = new GameObject("Mesh Holder").transform;
            var transform1 = meshHolder.transform;
            transform1.parent = transform;
            transform1.localPosition = Vector3.zero;
            transform1.localRotation = Quaternion.identity;
        }

        // Ensure mesh renderer and filter components are assigned
        if (!meshHolder.gameObject.GetComponent<MeshFilter>())
        {
            meshHolder.gameObject.AddComponent<MeshFilter>();
        }

        if (!meshHolder.GetComponent<MeshRenderer>())
        {
            meshHolder.gameObject.AddComponent<MeshRenderer>();
        }

        meshData.meshFilter = meshHolder.GetComponent<MeshFilter>();
    }
}