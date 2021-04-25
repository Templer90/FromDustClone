using System;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(GlobalMapTest))]
    public class GlobalMapTestEditor : UnityEditor.Editor
    {
        private GlobalMapTest _global;

        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        public override void OnInspectorGUI()
        {
            return;
            _global = (GlobalMapTest) target;
            //DrawDefaultInspector();

            GUILayout.BeginHorizontal();
            _global.material = (Material)EditorGUILayout.ObjectField("Material",   _global.material , typeof(Material));
            GUILayout.EndHorizontal();
            
            AssetPreview.SetPreviewTextureCacheSize(1);
            var myTexture = AssetPreview.GetAssetPreview(_global.heightTexture);
            GUILayout.Label(myTexture);
            
            Repaint();
        }
    }
}