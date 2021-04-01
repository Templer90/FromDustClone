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

        public void RecalculateNormals(IRuntimeMap map, float elevationScale)
        {
            Func<int, float> heightFunc = index => map.CellAt(index).getValue(Type) * elevationScale;

            switch (Type)
            {
                case Cell.Type.Water:
                    heightFunc = index =>
                    {
                        var c = map.CellAt(index);
                        return (c.LithoHeight + c.Water) * elevationScale;
                    };
                    break;
                case Cell.Type.Stone:
                    heightFunc = index =>
                    {
                        var c = map.CellAt(index);
                        return (c.LithoHeight) * elevationScale;
                    };
                    break;
                case Cell.Type.Sand:
                    break;
                case Cell.Type.Lava:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            meshFilter.mesh.normals = lod.RecalculateNormals(vertices, heightFunc);
        }

        public void RecalculateNormalsSharedMesh(IRuntimeMap map, float elevationScale)
        {
            meshFilter.sharedMesh.normals = lod.RecalculateNormals( meshFilter.sharedMesh.vertices,
                (index) => map.CellAt(index).getValue(Type) * elevationScale);
        }
    }
}