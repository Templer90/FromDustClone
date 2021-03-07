using System;
using System.ComponentModel;
using UnityEngine;
using System.Threading;
using UnityEngine.Serialization;

public class SynchronizedThread : MonoBehaviour
{
    public long meanUpdateTime = 0;
    public int lapses = 0;

    private Thread _thread;
    private IRuntimeMap _runtimeMap;
    public bool running;
    public bool pause;

    public void Start()
    {
        _runtimeMap = gameObject.GetComponent<TerrainGenerator>().runtimeMap;
        running = false;
        _thread = new Thread(DoUpdate) {IsBackground = true};
    }

    public void Update()
    {
        if (running) return;
        running = true;
        _thread.Start();
    }

    private void DoUpdate()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (!pause)
        {
            watch.Reset();
            _runtimeMap.MapUpdate();
            lapses++;
            watch.Stop();
            meanUpdateTime = watch.ElapsedMilliseconds;
        }
    }


    public void OnDisable()
    {
        if (_thread.IsAlive) _thread.Abort();
    }
}