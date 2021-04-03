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
    private static int[] _lodStepIncrements;

    public MeshData[] meshes =
    {
        new MeshData {Type = Cell.Type.Stone},
        new MeshData {Type = Cell.Type.Sand},
        new MeshData {Type = Cell.Type.Water},
        new MeshData {Type = Cell.Type.Lava}
    };

    private struct InternalData
    {
        public int numChunks;
        public int xSize;
        public int ySize;
        public int maxSize;
        public int offsetX;
        public int offsetY;
    }

    // Internal
    private IRuntimeMap _map;
    private int _mainSize; //Side length of the whole Map
    private InternalData _internalData;
    private float _counter;
    private const float StoneNormalUpdate = 0.0f;//In seconds

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
        
        //Set InternalData
        _internalData = new InternalData
        {
            numChunks = (_mainSize / mapSize),
            maxSize = mapSize + 1,
            offsetX = coords.x * mapSize,
            offsetY = coords.y * mapSize
        };
        _internalData.xSize = mapSize + ((coords.x < _internalData.numChunks) ? 1 : 0);
        _internalData.ySize = mapSize + ((coords.y < _internalData.numChunks) ? 1 : 0);

        _lodStepIncrements = new int[3];
        _lodStepIncrements[(int) LODTriangles.LOD.LOD0] = 1;
        _lodStepIncrements[(int) LODTriangles.LOD.LOD1] = 2;
        _lodStepIncrements[(int) LODTriangles.LOD.LOD2] = 4;
    }

    public void Start()
    {
        AssignMeshComponents(meshes[(int) Cell.Type.Stone]);
        AssignMeshComponents(meshes[(int) Cell.Type.Water]);
        AssignMeshComponents(meshes[(int) Cell.Type.Lava]);

        meshes[(int) Cell.Type.Stone].FromOwnMeshFilter();
        meshes[(int) Cell.Type.Water].FromOwnMeshFilter();
        meshes[(int) Cell.Type.Lava].FromOwnMeshFilter();
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
        }
    }

    private (float, bool) StoneFunc(Cell currentCell)
    {
        return (currentCell.LithoHeight * elevationScale, true);
    }

    private (float, bool) WaterFunc(Cell currentCell)
    {
        return currentCell.Water <= 0.0001f
            ? (0.0f, false)
            : ((currentCell.LithoHeight + currentCell.Water) * elevationScale, true);
    }

    private (float, bool) LavaFunc(Cell currentCell)
    {
        return currentCell.Lava <= 0.0001f
            ? (0.0f, false)
            : ((currentCell.LithoHeight + currentCell.Lava) * elevationScale, true);
    }

    private void UpdateMeshes()
    {
        _counter += Time.deltaTime;

        if (!gameObject.activeSelf) return;

        var stoneMeshData = meshes[(int) Cell.Type.Stone];
        var waterMeshData = meshes[(int) Cell.Type.Water];
        var lavaMeshData = meshes[(int) Cell.Type.Lava];

        stoneMeshData.lod.SwitchTriangles(meshes[(int) Cell.Type.Stone].meshFilter.mesh, LOD);
        waterMeshData.lod.SwitchTriangles(meshes[(int) Cell.Type.Water].meshFilter.mesh, LOD);

        var waterVisibility = false;
        var lavaVisibility = false;
        var step = _lodStepIncrements[(int) LOD];

        for (var x = 0; x < _internalData.xSize; x += step)
        {
            for (var y = 0; y < _internalData.ySize; y += step)
            {
                var meshMapIndex = y * _internalData.maxSize + x;
                var cellIndex = (_internalData.offsetY + y + 1) * _mainSize + (_internalData.offsetX + x + 1);

                if (!_map.ValidCoord(cellIndex)) continue;
                var currentCell = _map.CellAt(cellIndex);

                stoneMeshData.color[meshMapIndex].r = currentCell.Sand;
                var (stoneHeight, _) = StoneFunc(currentCell);
                stoneMeshData.vertices[meshMapIndex].y = stoneHeight;


                waterMeshData.color[meshMapIndex].r = currentCell.Water;
                var (waterHeight, visibleWater) = WaterFunc(currentCell);
                waterMeshData.vertices[meshMapIndex].y = waterHeight;
                waterMeshData.uv3[meshMapIndex] = currentCell.WaterFlow;
                waterVisibility |= visibleWater;


                lavaMeshData.color[meshMapIndex].r = currentCell.Stone;
                var (lavaHeight, visibleLava) = LavaFunc(currentCell);
                lavaMeshData.vertices[meshMapIndex].y = lavaHeight;
                lavaVisibility |= visibleLava;
            }
        }

        stoneMeshData.RefreshMesh();
        if (_counter > StoneNormalUpdate)
        {
            stoneMeshData.RecalculateNormals(_map, StoneFunc);
            _counter = 0;
        }

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
            meshes[(int) Cell.Type.Lava].RefreshMesh();
        }
        else
        {
            meshes[(int) Cell.Type.Lava].holder.SetActive(false);
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void ConstructMesh_Internal(MeshData meshData)
    {
        Func<Cell, (float, bool)> heightFunc = meshData.Type switch
        {
            Cell.Type.Stone => StoneFunc,
            Cell.Type.Sand => StoneFunc,
            Cell.Type.Water => WaterFunc,
            Cell.Type.Lava => LavaFunc,
            _ => throw new ArgumentOutOfRangeException()
        };

        (float, int) GETFunc(int x, int y)
        {
            var pos = (coords.y * mapSize + (y < 0 ? 0 : y)) * _mainSize + (coords.x * mapSize + (x < 0 ? 0 : x));
            pos = _map.ValidCoord(pos) ? pos : 0;

            var (height, _) = heightFunc(_map.CellAt(pos));
            return (height, pos);
        }

        var generatedRawMeshData = MeshGenerator.GenerateTerrainMesh(GETFunc, mapSize, scale);

        AssignMeshComponents(meshData);
        var mesh = generatedRawMeshData.CreateMesh();
        mesh.name = coords.x + " " + coords.y;
        meshData.meshFilter.sharedMesh = mesh;
        meshData.lod = generatedRawMeshData.GenLODTriangles();

        if (Application.isPlaying)
        {
            meshData.RecalculateNormals(_map, heightFunc);
        }
        else
        {
            meshData.RecalculateNormalsSharedMesh(_map, heightFunc);
        }

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