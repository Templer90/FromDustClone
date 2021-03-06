using System;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    [Header("Mesh Settings")] public Vector2 coords = new Vector2(0, 0);
    public int mapSize = 0;
    public float scale = 0;
    public float elevationScale = 10;

    [Serializable]
    public class MeshData
    {
        public Cell.Type Type { get; protected internal set; }
        public GameObject holder;

        [NonSerialized] public MeshRenderer meshRenderer;
        [NonSerialized] public MeshFilter meshFilter;
    }

    public MeshData[] meshes =
    {
        new MeshData {Type = Cell.Type.Stone},
        new MeshData {Type = Cell.Type.Sand},
        new MeshData {Type = Cell.Type.Water},
        new MeshData {Type = Cell.Type.Lava}
    };

    // Internal
    private RuntimeMap _map;
    private int _mainSize;
    private static readonly int MaxHeight = Shader.PropertyToID("_MaxHeight");


    public void Initialize(int x, int y, RuntimeMap mainMap, int mainMapSize, int chunkSize, float scaling,
        float elevationScaling)
    {
        coords.x = x;
        coords.y = y;
        _map = mainMap;
        _mainSize = mainMapSize;

        mapSize = chunkSize;
        scale = scaling;
        elevationScale = elevationScaling;

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
    }

    public void ConstructMeshes()
    {
        ConstructMesh(meshes[(int) Cell.Type.Stone]);
        ConstructMesh(meshes[(int) Cell.Type.Water]);
    }

    public void UpdateMeshes()
    {
        UpdateMesh(meshes[(int) Cell.Type.Stone]);
        UpdateMesh(meshes[(int) Cell.Type.Water]);
    }

    private void UpdateMesh(MeshData meshData)
    {
        if (!gameObject.activeSelf) return;

        var numChunks = (_mainSize / mapSize);
        var xSize = mapSize + (((int) coords.x < numChunks) ? 1 : 0);
        var ySize = mapSize + (((int) coords.y < numChunks) ? 1 : 0);
        var maxSize = mapSize + 1;

        var mesh1 = meshData.meshFilter.mesh;
        var verts = mesh1.vertices;
        var color = mesh1.colors;

        void StoneUpdate(int x, int y, int meshMapIndex)
        {
            var stone = GETVal(x, y, Cell.Type.Stone);
            var sand = GETVal(x, y, Cell.Type.Sand);

            color[meshMapIndex].r = sand;
            verts[meshMapIndex].y = stone * elevationScale;
        }

        void WaterUpdate(int x, int y, int meshMapIndex)
        {
            var water = GETVal(x, y, Cell.Type.Water);
            if (water < 0.0001f)
            {
                verts[meshMapIndex].y = 0;
            }
            else
            {
                verts[meshMapIndex].y = (GETVal(x, y, Cell.Type.Stone) + water) * elevationScale;
            }
        }

        Action<int, int, int> updateFunc;
        if (meshData.Type == Cell.Type.Water)
        {
            updateFunc = WaterUpdate;
        }
        else
        {
            updateFunc = StoneUpdate;
        }

        for (var x = 0; x < xSize; x++)
        {
            for (var y = 0; y < ySize; y++)
            {
                var meshMapIndex = y * maxSize + x;
                updateFunc(x, y, meshMapIndex);
            }
        }

        var mesh = meshData.meshFilter.mesh;
        mesh.vertices = verts;
        mesh.colors = color;
        //mesh.RecalculateNormals();
        //mesh.normals = CalculateNormals(verts, mesh.triangles);
    }


    private float GETVal(int x, int y, Cell.Type type)
    {
        return _map.ValueAt((int) coords.x * mapSize + x, (int) coords.y * mapSize + y, type);
    }

    private void ConstructMesh(MeshData meshData)
    {
        ConstructMesh_Internal(meshData);
    }

    private void ConstructMesh_Internal(MeshData meshData)
    {
        float GETFunc(int x, int y)
        {
            var xPos = (int) coords.x * mapSize + (x == -1 ? 0 : x);
            var yPos = (int) coords.y * mapSize + (y == -1 ? 0 : y);
            try
            {
                return _map.ValidCoord(xPos, yPos) ? _map.ValueAt(xPos, yPos, meshData.Type) : float.NaN;
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return float.NaN;
            }
        }

        var genMesh = MeshGenerator.GenerateTerrainMesh(GETFunc, mapSize, elevationScale, scale);

        AssignMeshComponents(meshData);
        var mesh = genMesh.CreateMesh();
        mesh.name = coords.x + " " + coords.y;
        meshData.meshFilter.sharedMesh = mesh;

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

        meshData.meshRenderer = meshHolder.GetComponent<MeshRenderer>();
        meshData.meshFilter = meshHolder.GetComponent<MeshFilter>();
    }
}