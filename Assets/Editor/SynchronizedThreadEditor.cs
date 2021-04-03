using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor (typeof (SynchronizedThread))]
    public class SynchronizedThreadEditor : UnityEditor.Editor {

        SynchronizedThread _synchronizedThread;

        public override void OnInspectorGUI () {
            DrawDefaultInspector ();

            if (GUILayout.Button ("Kill Thread"))
            {
                _synchronizedThread.KillThread();
                Debug.Log("Killed");
            }
        }

        void OnEnable () {
            _synchronizedThread = (SynchronizedThread) target;
            Tools.hidden = true;
        }

        void OnDisable () {
            Tools.hidden = false;
        }
    }
}