using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalMapTest : MonoBehaviour
{
    public Texture2D heightTexture;
    public Material material;
    private TerrainGenerator _generator;

    private Color[] _buffer;
    private bool _flag;
    private static readonly int HeightMap = Shader.PropertyToID("HeightMap");

    public void Init()
    {
        _generator = FindObjectOfType<TerrainGenerator>();

        heightTexture = new Texture2D(_generator.mapSize, _generator.mapSize);
        heightTexture.Apply();
        material.SetTexture(HeightMap, heightTexture);

        _buffer = new Color[_generator.mapSize * _generator.mapSize];
    }

    public void Update()
    {
        if (_flag == false) return;
        heightTexture.SetPixels(_buffer);
        heightTexture.Apply();
        material.SetTexture(HeightMap, heightTexture);
        _flag = false;
    }

    public void Apply(Color[] arr)
    {
        if (_flag) return;
        arr.CopyTo(_buffer, 0);
        _flag = true;
    }
}