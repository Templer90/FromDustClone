using System;
using UnityEngine;


[Serializable]
public class LODTriangles
{
    public enum LOD
    {
        LOD0,
        LOD1,
        LOD2
    };

    public int[] lod0Triangles;
    public int[] lod1Triangles;
    public int[] lod2Triangles;
    private LOD _oldLOD = LOD.LOD0;

    public LODTriangles(int[] lod0, int[] lod1, int[] lod2)
    {
        lod0Triangles = new int[lod0.Length];
        lod0.CopyTo(lod0Triangles, 0);

        lod1Triangles = new int[lod1.Length];
        lod1.CopyTo(lod1Triangles, 0);

        lod2Triangles = new int[lod2.Length];
        lod2.CopyTo(lod2Triangles, 0);
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
}