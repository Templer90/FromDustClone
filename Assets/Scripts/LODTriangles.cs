using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[Serializable]
public class LODTriangles
{
    public enum LOD
    {
        LOD0 = 1, //Every Vertex
        LOD1 = 2, //Every second Vertex (quarter of the Vertices in total)
        LOD2 = 4 //Every fourth Vertex (sixteens of the Vertices in total)
    }

    [SerializeField] public int[] lod0Triangles;
    [SerializeField] public int[] lod1Triangles;
    [SerializeField] public int[] lod2Triangles;

    [SerializeField] public Vector3[] borderVertices;
    [SerializeField] public int[] borderVerticesIndices;
    [SerializeField] public int[] borderTriangles;

    private LOD _oldLOD = LOD.LOD0;

    public LODTriangles(int[] lod0, int[] lod1, int[] lod2, Vector3[] borderVert, int[] borderVertIndices,
        int[] borderTri)
    {
        lod0Triangles = new int[lod0.Length];
        lod0.CopyTo(lod0Triangles, 0);

        lod1Triangles = new int[lod1.Length];
        lod1.CopyTo(lod1Triangles, 0);

        lod2Triangles = new int[lod2.Length];
        lod2.CopyTo(lod2Triangles, 0);

        borderVertices = new Vector3[borderVert.Length];
        borderVert.CopyTo(borderVertices, 0);

        borderVerticesIndices = new int[borderVertIndices.Length];
        borderVertIndices.CopyTo(borderVerticesIndices, 0);

        borderTriangles = new int[borderTri.Length];
        borderTri.CopyTo(borderTriangles, 0);
    }

    public void SwitchTriangles(Mesh mesh, LOD lodLevel)
    {
        if (lodLevel == _oldLOD) return;
        switch (lodLevel)
        {
            case LOD.LOD0:
                mesh.triangles = lod0Triangles;
                mesh.RecalculateBounds();
                break;
            case LOD.LOD1:
                mesh.triangles = lod1Triangles;
                mesh.RecalculateBounds();
                break;
            case LOD.LOD2:
                mesh.triangles = lod2Triangles;
                mesh.RecalculateBounds();
                break;
            default:
                mesh.triangles = mesh.triangles;
                break;
        }

        _oldLOD = lodLevel;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC, IReadOnlyList<Vector3> vertices,
        Func<int, float> heightMapFunc)
    {
        var pointA = (indexA < 0) ? borderVertices[-indexA - 1] : vertices[indexA];
        var pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
        var pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

        pointA.y = (indexA < 0) ? heightMapFunc(borderVerticesIndices[-indexA - 1]) : pointA.y;
        pointB.y = (indexB < 0) ? heightMapFunc(borderVerticesIndices[-indexB - 1]) : pointB.y;
        pointC.y = (indexC < 0) ? heightMapFunc(borderVerticesIndices[-indexC - 1]) : pointC.y;

        var sideAb = pointB - pointA;
        var sideAc = pointC - pointA;
        var perp = Vector3.Cross(sideAb, sideAc);
        var perpLength = perp.magnitude;
        perp /= perpLength;
        return perp;
    }

    public Vector3[] RecalculateNormals(Vector3[] vertices, Mesh mesh, Func<int, float> heightMapFunc)
    {
        mesh.RecalculateNormals();
        var vertexNormals = mesh.normals;

        var borderTriangleCount = borderTriangles.Length / 3;
        for (var i = 0; i < borderTriangleCount; i++)
        {
            var normalTriangleIndex = i * 3;
            var vertexIndexA = borderTriangles[normalTriangleIndex];
            var vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            var vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            var triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC, vertices, heightMapFunc);
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

    public Vector3[] RecalculateNormals(Vector3[] vertices, Func<int, float> heightMapFunc)
    {
        var vertexNormals = new Vector3[vertices.Length];
        var triangleCount = lod0Triangles.Length / 3;
        for (var i = 0; i < triangleCount; i++)
        {
            var normalTriangleIndex = i * 3;
            var vertexIndexA = lod0Triangles[normalTriangleIndex];
            var vertexIndexB = lod0Triangles[normalTriangleIndex + 1];
            var vertexIndexC = lod0Triangles[normalTriangleIndex + 2];

            var triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC, vertices, heightMapFunc);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        var borderTriangleCount = borderTriangles.Length / 3;
        for (var i = 0; i < borderTriangleCount; i++)
        {
            var normalTriangleIndex = i * 3;
            var vertexIndexA = borderTriangles[normalTriangleIndex];
            var vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            var vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            var triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC, vertices, heightMapFunc);
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
}