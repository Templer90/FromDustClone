using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class RuntimeMapHolder : MonoBehaviour
{
    public MapTypes mapType = MapTypes.CellBased;
    public PhysicData data;
    
    public enum MapTypes
    {
        Simple,CellBased
    }

    public IRuntimeMap MakeNewRuntimeMap(IReadOnlyList<float> initialHeightMap, int sideLength)
    {
        switch(mapType)
        {
            case MapTypes.Simple:
                return new SimpleMapUpdate(initialHeightMap, sideLength, data);
            case MapTypes.CellBased:
                return new CellBasedMapUpdate(initialHeightMap, sideLength, data);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
