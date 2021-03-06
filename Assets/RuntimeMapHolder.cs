using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuntimeMapHolder : MonoBehaviour
{
    public PhysicData data;
    private TerrainGenerator _terrainGenerator;

    public void Notify(TerrainGenerator terrain)
    {
        _terrainGenerator = terrain;
        data = _terrainGenerator.RuntimeMap.physic;
    }
}
