using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class SynchronizedThread : MonoBehaviour
{
    public float meanUpdateTime;
    public int meanLapsesPerUpdate;
    public int lapses;
    public bool running;
    public bool pause;
    public bool synchronize;

    private int _oldLapses = 0;
    private SynchroThread _thread;
    private IRuntimeMap _runtimeMap;

    public void Start()
    {
        _runtimeMap = gameObject.GetComponent<RuntimeMapHolder>().runtimeMap;
        running = false;
        _thread = new SynchroThread(DoUpdate);
        _thread.Start();
    }

    public void Update()
    {
        _thread.synchro = synchronize;
        
        meanLapsesPerUpdate = lapses - _oldLapses;
        meanUpdateTime = meanLapsesPerUpdate * Time.deltaTime;
        _oldLapses = lapses;

        _thread.Resume();
    }

    private void DoUpdate()
    {
        _runtimeMap.MapUpdate();
        lapses++;
    }

    public void KillThread()
    {
        if (_thread == null) return;
        if (_thread.IsAlive) _thread.Abort();
    }

    public void OnDisable()
    {
        KillThread();
    }

    public void OnApplicationQuit()
    {
        KillThread(); 
    }

    private class SynchroThread
    {
        public bool synchro = false;
        private bool _isRunning;
        private Thread _thread;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(true);
        private readonly Action _action;

        public SynchroThread(Action doUpdate)
        {
            _action = doUpdate;
            _isRunning = false;
        }

        public bool IsAlive => _thread.IsAlive;

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            _thread = new Thread(Process) {IsBackground = true};
            _thread.Start();
        }

        private void Process()
        {
            while (_isRunning)
            {
                _action();
                if (synchro) Pause();
                _resetEvent.WaitOne();
            }
        }

        public void Pause()
        {
            // unset the reset event which will cause the loop to pause
            _resetEvent.Reset();
        }

        public void Resume()
        {
            // set the reset event which will cause the loop to continue
            _resetEvent.Set();
        }

        public void Stop()
        {
            // set a flag that will abort the loop
            _isRunning = false;

            // set the event in case we are currently paused
            _resetEvent.Set();

            // wait for the thread to finish
            _thread.Join();
        }

        public void Abort()
        {
            // set a flag that will abort the loop
            _isRunning = false;

            // set the event in case we are currently paused
            _resetEvent.Set();

            // wait for the thread to finish
            _thread.Abort();
        }
    }
}