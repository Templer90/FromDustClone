using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Chunk))]
public class ChunkVisualizer : UnityEditor.Editor {

    private Chunk _chunk;

    public void OnEnable() {
        var mf = target as Chunk;
        if (mf != null) {
            _chunk = (Chunk)target;
        }
    }

    public void OnSceneGUI() {
        if (_chunk == null) {
            return;
        }
    }
    
    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    private static void DrawGizmoForMyScript(Chunk chunk, GizmoType gizmoType)
    {
        if (chunk == null) {
            return;
        }

        Gizmos.DrawCube(chunk.bounds.center,chunk.bounds.extents);
    }
}