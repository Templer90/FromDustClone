#define FastNormals
using System;
using UnityEngine;

public partial class Chunk
{
    [Serializable]
    public class MeshData
    {
        public Cell.Type Type { get; protected internal set; }
        public GameObject holder;

        public LODTriangles lod;
        [NonSerialized] public Vector3[] vertices;
        [NonSerialized] public Color[] color;
        [NonSerialized] public Vector2[] uv3;
        [NonSerialized] public MeshFilter meshFilter;

        public void FromOwnMeshFilter()
        {
            var mesh = meshFilter.mesh;
            vertices = mesh.vertices;
            color = mesh.colors;
            uv3 = mesh.uv3;
        }

        public void RecalculateAndRefresh(IRuntimeMap map, Func<Cell, (float, bool)> heightAtCell)
        {
            RecalculateNormals(map, heightAtCell);
            RefreshMesh();
        }

        public void RefreshMesh()
        {
            var mesh = meshFilter.mesh;
            mesh.vertices = vertices;
            mesh.colors = color;
            mesh.uv3 = uv3;
            mesh.MarkModified();
        }

        public void RecalculateNormals(IRuntimeMap map, Func<Cell, (float, bool)> heightAtCell)
        {
#if FastNormals
            meshFilter.mesh.normals =
 lod.RecalculateNormals(vertices, meshFilter.mesh, (index) => heightAtCell(map.CellAt(index)).Item1);
#else
            meshFilter.mesh.normals =
                lod.RecalculateNormals(vertices, (index) => heightAtCell(map.CellAt(index)).Item1);
#endif
        }

        public void RecalculateNormalsSharedMesh(IRuntimeMap map, Func<Cell, (float, bool)> heightAtCell)
        {
            meshFilter.sharedMesh.normals = lod.RecalculateNormals(meshFilter.sharedMesh.vertices,
                (index) => heightAtCell(map.CellAt(index)).Item1);
        }
    }
}