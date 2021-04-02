using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.VirtualTexturing;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(Func<int, int, (float, int)> heightMapFunc, int meshSize, float scale)
    {
        var internalSize = meshSize + 1;
        var borderedSize = internalSize + 2;

        var meshBuffer = new MeshData(internalSize);

        var vertexIndicesMap = new int[borderedSize, borderedSize];
        var meshVertexIndex = 0;
        var borderVertexIndex = -1;

        for (var y = 0; y < borderedSize; y++)
        {
            for (var x = 0; x < borderedSize; x++)
            {
                var isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;

                if (isBorderVertex)
                {
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        var height = 0f;
        for (var y = 0; y < borderedSize; y++)
        {
            for (var x = 0; x < borderedSize; x++)
            {
                var vertexIndex = vertexIndicesMap[x, y];

                var percent = new Vector2((x - 1) / ((float) internalSize - 1), (y - 1) / ((float) internalSize - 1));
                var vertexPosition = new Vector3(percent.x, 0, percent.y) * scale;

                var (heightValue, mapIndex) = heightMapFunc(x, y);
                if (!float.IsNaN(heightValue)) height = heightValue;
                vertexPosition.y = height;

                meshBuffer.AddVertex(vertexPosition, percent, vertexIndex, mapIndex);

                if (x < borderedSize - 1 && y < borderedSize - 1)
                {
                    var a = vertexIndicesMap[x, y];
                    var b = vertexIndicesMap[x + 1, y];
                    var c = vertexIndicesMap[x, y + 1];
                    var d = vertexIndicesMap[x + 1, y + 1];

                    meshBuffer.AddTriangle(a, c, d, LODTriangles.LOD.LOD0);
                    meshBuffer.AddTriangle(d, b, a, LODTriangles.LOD.LOD0);
                }

                vertexIndex++;
            }
        }


        for (var y = 1; y < borderedSize - 2; y += 2)
        {
            for (var x = 1; x < borderedSize - 2; x += 2)
            {
                if (x < borderedSize - 1 && y < borderedSize - 1)
                {
                    var a = vertexIndicesMap[x, y];
                    var b = vertexIndicesMap[x + 2, y];
                    var c = vertexIndicesMap[x, y + 2];
                    var d = vertexIndicesMap[x + 2, y + 2];

                    meshBuffer.AddTriangle(a, c, d, LODTriangles.LOD.LOD1);
                    meshBuffer.AddTriangle(d, b, a, LODTriangles.LOD.LOD1);
                }
            }
        }

        for (var y = 1; y < borderedSize - 4; y += 4)
        {
            for (var x = 1; x < borderedSize - 4; x += 4)
            {
                if (x < borderedSize - 1 && y < borderedSize - 1)
                {
                    var a = vertexIndicesMap[x, y];
                    var b = vertexIndicesMap[x + 4, y];
                    var c = vertexIndicesMap[x, y + 4];
                    var d = vertexIndicesMap[x + 4, y + 4];

                    meshBuffer.AddTriangle(a, c, d, LODTriangles.LOD.LOD2);
                    meshBuffer.AddTriangle(d, b, a, LODTriangles.LOD.LOD2);
                }
            }
        }

        return meshBuffer;
    }

    public class MeshData
    {
        private readonly Vector3[] _vertices;
        private readonly Color[] _colors;
        private readonly Color32[] _colors32;
        private readonly int[] _triangles;
        private readonly int[] _lod1Triangles;
        private readonly int[] _lod2Triangles;
        private readonly Vector2[] _uvs;

        private readonly Vector3[] _borderVertices;
        private readonly int[] _borderVerticesIndices;
        private readonly int[] _borderTriangles;

        private int _triangleIndex;
        private int _triangleLOD1Index;
        private int _triangleLOD2Index;

        private int _borderTriangleIndex;

        public MeshData(int verticesPerLine)
        {
            _vertices = new Vector3[verticesPerLine * verticesPerLine];
            _colors32 = new Color32[verticesPerLine * verticesPerLine];
            _colors = new Color[verticesPerLine * verticesPerLine];
            _uvs = new Vector2[verticesPerLine * verticesPerLine];

            _triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];
            _lod1Triangles = new int[_triangles.Length / 4];
            _lod2Triangles = new int[_triangles.Length / 16];

            _borderVertices = new Vector3[verticesPerLine * 4 + 4];
            _borderVerticesIndices = new int[_borderVertices.Length];
            _borderTriangles = new int[24 * verticesPerLine];
        }

        public LODTriangles GenLODTriangles()
        {
            var checkedLOD2 = new int[_triangleLOD2Index];
            for (var i = 0; i < _triangleLOD2Index; i++)
            {
                checkedLOD2[i] = _lod2Triangles[i];
            }

            return new LODTriangles(_triangles, _lod1Triangles, checkedLOD2, _borderVertices, _borderVerticesIndices,
                _borderTriangles);
        }

        public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex, int mapIndex)
        {
            if (vertexIndex < 0)
            {
                _borderVertices[-vertexIndex - 1] = vertexPosition;
                _borderVerticesIndices[-vertexIndex - 1] = mapIndex;
            }
            else
            {
                _vertices[vertexIndex] = vertexPosition;
                _uvs[vertexIndex] = uv;
            }
        }

        public void AddTriangle(int a, int b, int c, LODTriangles.LOD lodLevel = LODTriangles.LOD.LOD0)
        {
            if (a < 0 || b < 0 || c < 0)
            {
                _borderTriangles[_borderTriangleIndex] = a;
                _borderTriangles[_borderTriangleIndex + 1] = b;
                _borderTriangles[_borderTriangleIndex + 2] = c;
                _borderTriangleIndex += 3;
            }
            else
            {
                switch (lodLevel)
                {
                    case LODTriangles.LOD.LOD0:
                        _triangles[_triangleIndex] = a;
                        _triangles[_triangleIndex + 1] = b;
                        _triangles[_triangleIndex + 2] = c;
                        _triangleIndex += 3;
                        break;
                    case LODTriangles.LOD.LOD1:
                        _lod1Triangles[_triangleLOD1Index] = a;
                        _lod1Triangles[_triangleLOD1Index + 1] = b;
                        _lod1Triangles[_triangleLOD1Index + 2] = c;
                        _triangleLOD1Index += 3;
                        break;
                    case LODTriangles.LOD.LOD2:
                        _lod2Triangles[_triangleLOD2Index] = a;
                        _lod2Triangles[_triangleLOD2Index + 1] = b;
                        _lod2Triangles[_triangleLOD2Index + 2] = c;
                        _triangleLOD2Index += 3;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(lodLevel), lodLevel, null);
                }
            }
        }

        public Mesh CreateMesh()
        {
            var mesh = new Mesh
            {
                vertices = _vertices,
                triangles = _triangles,
                uv = _uvs,
                uv3 = new Vector2[_vertices.Length],
                colors32 = _colors32,
                colors = _colors
            };
            // mesh.RecalculateNormals();
            mesh.MarkDynamic();
            return mesh;
        }
    }
}