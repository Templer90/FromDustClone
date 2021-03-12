using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    [Header("Mesh Settings")] public Vector2Int coords = new Vector2Int(0, 0);
    public int mapSize = 0;
    public float scale = 0;
    public float elevationScale = 10;
    public Bounds Bounds;
    public LODTriangles.LOD LOD = LODTriangles.LOD.LOD1;


    [Serializable]
    public class LODTriangles
    {
        public enum LOD
        {
            LOD1,
            LOD2
        };

        public int[] _LOD1triangles;
        public int[] _LOD2triangles;
        private LOD oldLOD = LOD.LOD1;

        public LODTriangles(int[] lod1, int[] lod2)
        {
            _LOD1triangles = new int[lod1.Length];
            lod1.CopyTo(_LOD1triangles, 0);
            _LOD2triangles = new int[lod2.Length];
            lod2.CopyTo(_LOD2triangles, 0);
        }

        public void SwitchTriangles(Mesh mesh, LOD LOD)
        {
            if (LOD == oldLOD) return;
            switch (LOD)
            {
                case LOD.LOD1:
                    mesh.triangles = _LOD1triangles;
                    mesh.RecalculateBounds();
                    break;
                case LOD.LOD2:
                    mesh.triangles = _LOD2triangles;
                    mesh.RecalculateBounds();
                    break;
                default:
                    mesh.triangles = mesh.triangles;
                    break;
            }

            oldLOD = LOD;
        }
    }

    [Serializable]
    public class MeshData
    {
        public Cell.Type Type { get; protected internal set; }
        public GameObject holder;

        public LODTriangles lod;
        [NonSerialized] public Vector3[] vertices;
        [NonSerialized] public Color[] color;
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
    private IRuntimeMap _map;
    private int _mainSize;


    public void Initialize(int x, int y, IRuntimeMap mainMap, int mainMapSize, int chunkSize, float scaling,
        float elevationScaling)
    {
        var pos = new Vector3(x * scale, 0, y * scale);
        var halfScale = scale / 2.0f;
        var transform1 = transform;
        transform1.name = "Chunk(" + x + "," + y + ")";
        transform1.position = pos;

        Bounds = new Bounds(pos + new Vector3(halfScale, 0, halfScale), new Vector3(halfScale, 100, halfScale));

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

        var meshStone = meshes[(int) Cell.Type.Stone].meshFilter.mesh;
        meshes[(int) Cell.Type.Stone].vertices = meshStone.vertices;
        meshes[(int) Cell.Type.Stone].color = meshStone.colors;

        var meshWater = meshes[(int) Cell.Type.Water].meshFilter.mesh;
        meshes[(int) Cell.Type.Water].vertices = meshWater.vertices;
        meshes[(int) Cell.Type.Water].color = meshWater.colors;
    }

    public void ConstructMeshes()
    {
        ConstructMesh_Internal(meshes[(int) Cell.Type.Stone]);
        ConstructMesh_Internal(meshes[(int) Cell.Type.Water]);
    }

    public void UpdateMeshes()
    {
        if (!gameObject.activeSelf) return;

        meshes[(int) Cell.Type.Stone].lod
            .SwitchTriangles(meshes[(int) Cell.Type.Stone].meshFilter.mesh, LOD);

        var numChunks = (_mainSize / mapSize);
        var xSize = mapSize + ((coords.x < numChunks) ? 1 : 0);
        var ySize = mapSize + ((coords.y < numChunks) ? 1 : 0);
        var maxSize = mapSize + 1;

        var offsetX = coords.x * mapSize;
        var offsetY = coords.y * mapSize;


        var step = LOD == LODTriangles.LOD.LOD1 ? 1 : 2;
        for (var x = 0; x < xSize; x += step)
        {
            for (var y = 0; y < ySize; y += step)
            {
                var meshMapIndex = y * maxSize + x;
                var currentCell = _map.CellAt(offsetX + x, offsetY + y);

                var stone = currentCell.Stone;
                var sand = currentCell.Sand;
                var water = currentCell.Water;

                meshes[(int) Cell.Type.Stone].color[meshMapIndex].r = sand;
                meshes[(int) Cell.Type.Stone].vertices[meshMapIndex].y = (stone + sand) * elevationScale;


                meshes[(int) Cell.Type.Water].color[meshMapIndex].r = water;
                if (water < 0.0001f)
                {
                    meshes[(int) Cell.Type.Water].vertices[meshMapIndex].y = 0;
                }
                else
                {
                    meshes[(int) Cell.Type.Water].vertices[meshMapIndex].y = (stone + sand + water) * elevationScale;
                }
            }
        }

        var meshStone = meshes[(int) Cell.Type.Stone].meshFilter.mesh;
        meshStone.vertices = meshes[(int) Cell.Type.Stone].vertices;
        meshStone.colors = meshes[(int) Cell.Type.Stone].color;

        var meshWater = meshes[(int) Cell.Type.Water].meshFilter.mesh;
        meshWater.vertices = meshes[(int) Cell.Type.Water].vertices;
        meshWater.colors = meshes[(int) Cell.Type.Water].color;

        //meshStone.RecalculateNormals();
        //meshStone.normals = CalculateNormals(verts, mesh.triangles);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float GETVal(int x, int y, Cell.Type type)
    {
        return _map.ValueAt((int) coords.x * mapSize + x, (int) coords.y * mapSize + y, type);
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

        var generatedRawMeshData = MeshGenerator.GenerateTerrainMesh(GETFunc, mapSize, elevationScale, scale);

        AssignMeshComponents(meshData);
        var mesh = generatedRawMeshData.CreateMesh();
        mesh.name = coords.x + " " + coords.y;
        meshData.meshFilter.sharedMesh = mesh;
        meshData.lod = generatedRawMeshData.genLODTriangles();

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