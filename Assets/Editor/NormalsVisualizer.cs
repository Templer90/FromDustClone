using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeshFilter))]
public class NormalsVisualizer : UnityEditor.Editor {

    private Mesh _mesh;

    public void OnEnable() {
        var mf = target as MeshFilter;
        if (mf != null) {
            _mesh = mf.sharedMesh;
        }
    }

    public void OnSceneGUI() {
        if (_mesh == null) {
            return;
        }

        Handles.matrix = ((MeshFilter) target).transform.localToWorldMatrix;
        Handles.color = Color.yellow;
        var verts = _mesh.vertices;
        var normals = _mesh.normals;
        var len = _mesh.vertexCount;
        
        for (var i = 0; i < len; i++) {
            Handles.DrawLine(verts[i], verts[i] + normals[i]);
        }
    }
}