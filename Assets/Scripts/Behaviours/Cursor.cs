using UnityEngine;


public class Cursor : MonoBehaviour
{
    public Vector3 worldPosition;
    public Vector2Int test;
    public GameObject positionSphere;
    public float amount = 0.1f;
    public float size = 3;
    public Cell.Type type = Cell.Type.Water;

    private Camera _cam;
    private RuntimeMapHolder _runtimeMap;

    public void Start()
    {
        _cam = Camera.main;
        _runtimeMap = FindObjectOfType<RuntimeMapHolder>();
    }

    public void OnGUI()
    {
        if (!Physics.Raycast(_cam.ScreenPointToRay(Input.mousePosition), out var hit))
            return;

        var meshCollider = hit.collider as MeshCollider;
        if (meshCollider == null || meshCollider.sharedMesh == null)
            return;

        worldPosition = hit.point;
        test = _runtimeMap.WorldCoordinatesToCell(hit.point);

        if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1)) return;
        var quantity = amount * (Input.GetMouseButton(0) ? 1 : Input.GetMouseButton(1) ? -1 : 0);
        var load = quantity / (size*size);
        for (var x = Mathf.FloorToInt(size/-2.0f); x < size/2.0f; x++)
        {
            for (var y = Mathf.FloorToInt(size/-2.0f); y < size/2.0f; y++)
            {
                _runtimeMap.Add(test.x + x, test.y + y, type, load);
            }
        }
    }
}