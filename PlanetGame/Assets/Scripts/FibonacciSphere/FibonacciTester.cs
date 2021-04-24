

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Planets;
using UnityEngine.UI;


//[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class FibonacciTester : MonoBehaviour
{
    const int MAX_TILES = 1474;
    enum _mesh_type { RAW, TILED };

    #region Variables (PRIVATE)
    [SerializeField, Range(50, MAX_TILES)]  //  1474 is the number just before the north pole generation creates overlapping tiles
    int _num_points = 100;                  //      at 1083 fewer edge issues occur.
    [SerializeField, Range(0f, 0.2f)]
    float _tile_seperation = 0.02f;
    [SerializeField]
    float _radius = 1f;
    [SerializeField]
    int _selection = 25;
    [SerializeField]
    _mesh_type _mesh_type_shown = _mesh_type.RAW;
    [SerializeField]
    GameObject _tile_marker_prefab;

    Planet _planet;
    Mesh _mesh;
    MeshCollider _mesh_collider;
    MeshFilter _filter;
    List<GameObject> _tile_markers;
    #endregion

    #region Properties (PUBLIC)
    public Planet Planet => _planet;
    public float Radius => _radius;
    #endregion

    #region Unity Methods
    // Start is called before the first frame update
    private void Awake()
    {
        _planet = new Planet(_num_points, _radius);
        _planet.Generate_Planet(_tile_seperation);
        _filter = GetComponent<MeshFilter>();
        _mesh_collider = gameObject.AddComponent<MeshCollider>();

        Make_Mesh();

        if (_tile_markers != null)
        {
            foreach (GameObject tile in _tile_markers)
            {
                Destroy(tile);
            }
            _tile_markers.Clear();
        }

        // Add tile markers for some reason?
        _tile_markers = new List<GameObject>();
        if (_tile_marker_prefab != null)
        {
            for (int i = 0; i < _planet.Tiles.Count; i++)
            {
                _tile_markers.Add(Instantiate(_tile_marker_prefab));
                _tile_markers[i].transform.SetParent(this.transform);
                _tile_markers[i].SetActive(false);
            }
            foreach (GameObject tile in _tile_markers)
            {
                tile.SetActive(false);
            }

            for (int i = 0; i < _num_points; i++)
            {
                _tile_markers[i].transform.position = transform.TransformPoint((_planet.Tiles[i].Position).normalized * _radius * 1.001f);
                _tile_markers[i].transform.forward = transform.position - transform.TransformPoint(_planet.Tiles[i].Position);
                _tile_markers[i].SetActive(true);

                Vector3 pos = _planet.Tiles[i].Position.normalized;
                float Lat = Mathf.Asin(pos.y) * Mathf.Rad2Deg;
                float Long = Mathf.Atan2(pos.z, pos.x) * Mathf.Rad2Deg;
                string LatLong = i.ToString() + "\n" + Lat.ToString() + "\n" + Long.ToString();
                _tile_markers[i].transform.GetChild(0).GetComponent<Text>().text = LatLong;

                float minsize = float.PositiveInfinity;
                foreach (Vector3 extent in _planet.Tiles[i].Extents)
                {
                    float dist = Vector3.Distance(_planet.Tiles[i].Position, extent);
                    if (dist < minsize)
                        minsize = dist;
                }

                _tile_markers[i].transform.GetComponent<RectTransform>().localScale = Vector3.one * minsize;
            }
        }
    }

    private void Update()
    {
        _selection = Mathf.Clamp(_selection, 0, _planet.Tiles.Count - 1);

        if (Input.GetMouseButton(0))
        {
            HandleInput();
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            //Draw touch
            Gizmos.color = Color.blue;
            if (Input.GetMouseButton(0))
            {
                Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if(Physics.Raycast(inputRay, out hit))
                {
                    Vector3 point = hit.point;

                    Gizmos.DrawLine(Camera.main.transform.position, point);
                    Gizmos.DrawWireSphere(point, 0.03f);
                    float Lat = Mathf.Asin(point.y) * Mathf.Rad2Deg;
                    float Long = Mathf.Atan2(point.z, point.x) * Mathf.Rad2Deg;

                    Handles.Label(point, Lat.ToString() + "\n" + Long.ToString());
                }
                else
                {
                    Gizmos.DrawRay(inputRay);
                }
            }

            // Draw debugs for selected tile
            Gizmos.color = Color.white;
            Vector3 pos = transform.TransformPoint(_planet.Tiles[_selection].Position);
            Gizmos.DrawWireSphere(pos, 0.02f);
            Vector3 Normal = (pos - this.transform.position).normalized;
            Gizmos.DrawLine(pos, pos + Normal);
            for (int i = 0; i < _planet.Tiles[_selection].Neighbors.Count; i++)
            {
                Gizmos.DrawWireSphere(transform.TransformPoint(_planet.Tiles[_selection].Neighbors[i].Position), 0.015f);
                Gizmos.DrawWireSphere(transform.TransformPoint(_planet.Tiles[_selection].Extents[i]), 0.001f);

                //Find right and left
                float length = _radius * 0.1f;
                Gizmos.color = Color.magenta;
                Vector3 right = Vector3.Cross(pos - _planet.Tiles[_selection].Extents[i], Normal).normalized * length;
                Gizmos.DrawLine(transform.TransformPoint(_planet.Tiles[_selection].Extents[i]), transform.TransformPoint(_planet.Tiles[_selection].Extents[i]) + right);
                Gizmos.color = Color.green;
                Vector3 left = Vector3.Cross(Normal, pos - _planet.Tiles[_selection].Extents[i]).normalized * length;
                Gizmos.DrawLine(transform.TransformPoint(_planet.Tiles[_selection].Extents[i]), transform.TransformPoint(_planet.Tiles[_selection].Extents[i]) + left);

                for (int j = 0; j < _planet.Tiles[_selection].Corners.Count; j++)
                {
                    Gizmos.color = Color.Lerp(Color.blue, Color.white, (float)j / (float)_planet.Tiles[_selection].Corners.Count);
                    Gizmos.DrawWireSphere(transform.TransformPoint(_planet.Tiles[_selection].Corners[j]), 0.025f);
                    Handles.Label(transform.TransformPoint(_planet.Tiles[_selection].Corners[j]), j.ToString());
                }
                Handles.Label(transform.TransformPoint(_planet.Tiles[_selection].Extents[i]), i.ToString());
                Handles.Label(transform.TransformPoint(_planet.Tiles[_selection].Neighbors[i].Position), i.ToString());
            }

            //Draw all points on the sphere
            /*
            for (int i = 0; i < _planet.Tiles.Count; i++)
            {
                Gizmos.color = Color.Lerp(Color.green, Color.blue, ((float)i / (float)_planet.Tiles.Count));
                if(i == _selection)
                {
                    Gizmos.color = Color.white;
                    foreach(Tile tile in _planet.Tiles[_selection].Neighbors)
                    {
                        Gizmos.DrawWireSphere(transform.TransformPoint(tile.Position), 0.06f);
                    }
                }

                Gizmos.DrawSphere(transform.TransformPoint(_planet.Tiles[i].Position), 0.05f);
            }
            */
        }
    }
    #endregion

    #region Methods
    void Make_Mesh()
    {
        switch (_mesh_type_shown)
        {
            case (_mesh_type.TILED):
                _mesh = new Mesh();
                _mesh = _planet.Tiled_Mesh;
                _filter.mesh = _mesh;
                _mesh_collider.sharedMesh = _mesh;
                _mesh.RecalculateNormals();
                break;
            case (_mesh_type.RAW):
                _mesh = new Mesh();
                _mesh.name = "Raw Mesh";
                _mesh.vertices = _planet.Tile_Positions.ToArray();
                _mesh.triangles = _planet.Default_Triangles;
                _filter.mesh = _mesh;
                _mesh_collider.sharedMesh = _mesh;
                _mesh.RecalculateNormals();
                break;
        }
    }

    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(inputRay, out hit))
        {
            TouchCell(hit.point);
        }
    }

    void TouchCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        int tile = _planet.Point_To_Tile(position);

        if(tile > 0)
        {
            _selection = tile;
        }

        Debug.Log("touched at " + tile);
    }
    #endregion
}
