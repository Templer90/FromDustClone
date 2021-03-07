using System;
using UnityEngine;
using System.Collections;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(Func<int, int, float> heightMapFunc, int meshSize,
        float heightMultiplier, float scale)
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

                var newHeight = heightMapFunc(x, y);
                if (!float.IsNaN(newHeight)) height = newHeight;
                vertexPosition.y = height * heightMultiplier;

                meshBuffer.AddVertex(vertexPosition, percent, vertexIndex);

                if (x < borderedSize - 1 && y < borderedSize - 1)
                {
                    var a = vertexIndicesMap[x, y];
                    var b = vertexIndicesMap[x + 1, y];
                    var c = vertexIndicesMap[x, y + 1];
                    var d = vertexIndicesMap[x + 1, y + 1];

                    meshBuffer.AddTriangle(a, c, d);
                    meshBuffer.AddTriangle(d, b, a);
                }

                vertexIndex++;
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
        private readonly Vector2[] _uvs;

        private readonly Vector3[] _borderVertices;
        private readonly int[] _borderTriangles;

        private int _triangleIndex;
        private int _borderTriangleIndex;

        public MeshData(int verticesPerLine)
        {
            _vertices = new Vector3[verticesPerLine * verticesPerLine];
            _colors32 = new Color32[verticesPerLine * verticesPerLine];
            _colors = new Color[verticesPerLine * verticesPerLine];
            _uvs = new Vector2[verticesPerLine * verticesPerLine];
            _triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

            _borderVertices = new Vector3[verticesPerLine * 4 + 4];
            _borderTriangles = new int[24 * verticesPerLine];
        }

        public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
        {
            if (vertexIndex < 0)
            {
                _borderVertices[-vertexIndex - 1] = vertexPosition;
            }
            else
            {
                _vertices[vertexIndex] = vertexPosition;
                _uvs[vertexIndex] = uv;
            }
        }

        public void AddTriangle(int a, int b, int c)
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
                _triangles[_triangleIndex] = a;
                _triangles[_triangleIndex + 1] = b;
                _triangles[_triangleIndex + 2] = c;
                _triangleIndex += 3;
            }
        }

        public Vector3[] CalculateNormals()
        {
            var vertexNormals = new Vector3[_vertices.Length];
            var triangleCount = _triangles.Length / 3;
            for (var i = 0; i < triangleCount; i++)
            {
                var normalTriangleIndex = i * 3;
                var vertexIndexA = _triangles[normalTriangleIndex];
                var vertexIndexB = _triangles[normalTriangleIndex + 1];
                var vertexIndexC = _triangles[normalTriangleIndex + 2];

                var triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
                vertexNormals[vertexIndexA] += triangleNormal;
                vertexNormals[vertexIndexB] += triangleNormal;
                vertexNormals[vertexIndexC] += triangleNormal;
            }

            var borderTriangleCount = _borderTriangles.Length / 3;
            for (var i = 0; i < borderTriangleCount; i++)
            {
                var normalTriangleIndex = i * 3;
                var vertexIndexA = _borderTriangles[normalTriangleIndex];
                var vertexIndexB = _borderTriangles[normalTriangleIndex + 1];
                var vertexIndexC = _borderTriangles[normalTriangleIndex + 2];

                var triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
                if (vertexIndexA >= 0)
                {
                    vertexNormals[vertexIndexA] += triangleNormal;
                }

                if (vertexIndexB >= 0)
                {
                    vertexNormals[vertexIndexB] += triangleNormal;
                }

                if (vertexIndexC >= 0)
                {
                    vertexNormals[vertexIndexC] += triangleNormal;
                }
            }


            for (var i = 0; i < vertexNormals.Length; i++)
            {
                vertexNormals[i].Normalize();
            }

            return vertexNormals;
        }

        public Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
        {
            var pointA = (indexA < 0) ? _borderVertices[-indexA - 1] : _vertices[indexA];
            var pointB = (indexB < 0) ? _borderVertices[-indexB - 1] : _vertices[indexB];
            var pointC = (indexC < 0) ? _borderVertices[-indexC - 1] : _vertices[indexC];

            var sideAb = pointB - pointA;
            var sideAc = pointC - pointA;
            return Vector3.Cross(sideAb, sideAc).normalized;
        }

        public Mesh CreateMesh()
        {
            var mesh = new Mesh
            {
                vertices = _vertices,
                triangles = _triangles,
                uv = _uvs,
                normals = CalculateNormals(),
                colors32 = _colors32,
                colors = _colors
            };
            return mesh;
        }
    }
}