using System;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.Serialization;


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

        [NonSerialized] public MeshRenderer MeshRenderer;
        [NonSerialized] public MeshFilter MeshFilter;
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

        float StoneUpdate(int x, int y)
        {
            return GETVal(x, y, Cell.Type.Stone) * elevationScale;
        }

        float WaterUpdate(int x, int y)
        {
            var water = GETVal(x, y, Cell.Type.Water);
            if (water < 0.01f)
            {
                return 0;
            }
            return (GETVal(x, y, Cell.Type.Stone) + water) * elevationScale;
        }

        Func<int,int,float > updateFunc;
        if (meshData.Type == Cell.Type.Water)
        {
            updateFunc = WaterUpdate;
        }
        else
        {
            updateFunc = StoneUpdate;
        }              
        
        
        var numChunks = (_mainSize / mapSize);
        var xSize = mapSize + (((int) coords.x < numChunks) ? 1 : 0);
        var ySize = mapSize + (((int) coords.y < numChunks) ? 1 : 0);
        var maxSize = mapSize + 1;

        var verts = meshData.MeshFilter.mesh.vertices;
        for (var x = 0; x < xSize; x++)
        {
            for (var y = 0; y < ySize; y++)
            {
                var meshMapIndex = y * maxSize + x;

                verts[meshMapIndex].y = updateFunc(x, y);
            }
        }

        var mesh = meshData.MeshFilter.mesh;
        mesh.vertices = verts;
        mesh.normals = CalculateNormals(verts, mesh.triangles);
    }


    private float GETVal(int x, int y, Cell.Type type)
    {
        return _map.ValueAt((int) coords.x * mapSize + x, (int) coords.y * mapSize + y, type);
    }

    private void ConstructMesh(MeshData meshData)
    {
        var numChunks = (_mainSize / mapSize);
        var xSize = mapSize + (((int) coords.x < numChunks) ? 1 : 0);
        var ySize = mapSize + (((int) coords.y < numChunks) ? 1 : 0);
        var maxSize = mapSize + 1;

        var verts = new Vector3[maxSize * maxSize];
        var triangles = new int[(maxSize - 1) * (maxSize - 1) * 6];
        var t = 0;

        void MakeTri(int x, int y, float normalizedHeight)
        {
            var meshMapIndex = y * maxSize + x;

            var percent = new Vector2(x / (maxSize - 1f), y / (maxSize - 1f));
            var pos = new Vector3(percent.x * 2 - 1, 0, percent.y * 2 - 1) * scale;

            pos += Vector3.up * (normalizedHeight * elevationScale);
            verts[meshMapIndex] = pos;

            // Construct triangles
            if (x != xSize - 1 && y != ySize - 1)
            {
                t = (y * (maxSize - 1) + x) * 3 * 2;

                triangles[t + 0] = meshMapIndex + maxSize;
                triangles[t + 1] = meshMapIndex + maxSize + 1;
                triangles[t + 2] = meshMapIndex;

                triangles[t + 3] = meshMapIndex + maxSize + 1;
                triangles[t + 4] = meshMapIndex + 1;
                triangles[t + 5] = meshMapIndex;
                t += 6;
            }
        }

        for (var x = 0; x < xSize; x++)
        {
            for (var y = 0; y < ySize; y++)
            {
                MakeTri(x, y, GETVal(x, y, meshData.Type));
            }
        }


        AssignMeshComponents(meshData);
        var mesh = meshData.MeshFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh();
        }
        else
        {
            mesh.Clear();
        }

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.triangles = triangles;
        mesh.normals = CalculateNormals(verts, mesh.triangles);

        meshData.MeshFilter.sharedMesh = mesh;
        //meshData.MeshRenderer.sharedMaterial = stoneMaterial;

        //stoneMaterial.SetFloat(MaxHeight, elevationScale);
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

        meshData.MeshRenderer = meshHolder.GetComponent<MeshRenderer>();
        meshData.MeshFilter = meshHolder.GetComponent<MeshFilter>();
    }

    private static Vector3[] CalculateNormals(Vector3[] verts, int[] triangles)
    {
        Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
        {
            var pointA = verts[indexA];
            var pointB = verts[indexB];
            var pointC = verts[indexC];

            var sideAb = pointB - pointA;
            var sideAc = pointC - pointA;
            return Vector3.Cross(sideAb, sideAc).normalized;
        }

        var vertxNormals = new Vector3[verts.Length];
        var triangleCount = triangles.Length / 3;
        for (var i = 0; i < triangleCount; i++)
        {
            var normalTriangleIndex = i * 3;
            var vertIndexA = triangles[normalTriangleIndex + 0];
            var vertIndexB = triangles[normalTriangleIndex + 1];
            var vertIndexC = triangles[normalTriangleIndex + 2];

            var triangleNormal = SurfaceNormalFromIndices(vertIndexA, vertIndexB, vertIndexC);
            vertxNormals[vertIndexA] += triangleNormal;
            vertxNormals[vertIndexB] += triangleNormal;
            vertxNormals[vertIndexC] += triangleNormal;
        }

        for (var i = 0; i < vertxNormals.Length; i++)
        {
            vertxNormals[i].Normalize();
        }

        return vertxNormals;
    }
}