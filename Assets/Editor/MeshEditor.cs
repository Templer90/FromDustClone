using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor (typeof (TerrainGenerator))]
    public class MeshEditor : UnityEditor.Editor {

        TerrainGenerator _terrainGenerator;

        public override void OnInspectorGUI () {
            DrawDefaultInspector ();

            if (GUILayout.Button ("Generate Mesh")) {
                _terrainGenerator.EmptyChildren();
                _terrainGenerator.GenerateHeightMap ();
                _terrainGenerator.ConstructMesh();
            }

            /*string numIterationsString = _terrainGenerator.numErosionIterations.ToString();
            if (_terrainGenerator.numErosionIterations >= 1000) {
                numIterationsString = (_terrainGenerator.numErosionIterations/1000) + "k";
            }

            if (GUILayout.Button ("Erode (" + numIterationsString + " iterations)")) {
                var sw = new System.Diagnostics.Stopwatch ();

                sw.Start();
                _terrainGenerator.GenerateHeightMap();
                int heightMapTimer = (int)sw.ElapsedMilliseconds;
                sw.Reset();

                sw.Start();
                _terrainGenerator.Erode ();
                int erosionTimer = (int)sw.ElapsedMilliseconds;
                sw.Reset();

                sw.Start();
                _terrainGenerator.ContructMesh();
                int meshTimer = (int)sw.ElapsedMilliseconds;

                if (_terrainGenerator.printTimers) {
                    Debug.Log($"{_terrainGenerator.mapSize}x{_terrainGenerator.mapSize} heightmap generated in {heightMapTimer}ms");
                    Debug.Log ($"{numIterationsString} erosion iterations completed in {erosionTimer}ms");
                    Debug.Log ($"Mesh constructed in {meshTimer}ms");
                }

            }*/
        }

        void OnEnable () {
            _terrainGenerator = (TerrainGenerator) target;
            Tools.hidden = true;
        }

        void OnDisable () {
            Tools.hidden = false;
        }
    }
}