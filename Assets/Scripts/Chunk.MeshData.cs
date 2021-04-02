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
        [NonSerialized] public Vector2[] uv5;
        [NonSerialized] public MeshFilter meshFilter;

        public void RecalculateAndRefresh(IRuntimeMap map, Func<Cell, (float, bool)> heightAtCell)
        {
            RecalculateNormals(map, heightAtCell);
            var meshStone = meshFilter.mesh;
            meshStone.vertices = vertices;
            meshStone.colors = color;
            meshStone.uv5 = uv5;
        }
        
        public void RefreshMesh()
        {
            var meshStone = meshFilter.mesh;
            meshStone.vertices = vertices;
            meshStone.colors = color;
            meshStone.uv5 = uv5;
        }
        
        public void RecalculateNormals(IRuntimeMap map, Func<Cell, (float, bool)> heightAtCell)
        {
            meshFilter.mesh.normals = lod.RecalculateNormals( vertices, (index) => heightAtCell(map.CellAt(index)).Item1);
        }

        public void RecalculateNormalsSharedMesh(IRuntimeMap map, Func<Cell, (float, bool)> heightAtCell)
        {
            meshFilter.sharedMesh.normals = lod.RecalculateNormals(meshFilter.sharedMesh.vertices,
                (index) => heightAtCell(map.CellAt(index)).Item1);
        }
    }
}