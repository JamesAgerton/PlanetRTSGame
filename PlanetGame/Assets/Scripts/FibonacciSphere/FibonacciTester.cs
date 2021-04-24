

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
    [SerializeField, Range(50, MAX_TILES)]   //  1474 is the number just before the north pole generation creates overlapping tiles
                                             //      at 1083 fewer edge issues occur.
    int _num_points = 100;
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

    float _current_radius = 1f;
    float _current_points = 100;
    float _current_seperation = 0.02f;
    _mesh_type _current_mesh_type = _mesh_type.RAW;

    Planet _planet;
    Mesh _mesh;
    MeshFilter _filter;
    MeshRenderer _renderer;
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
        _renderer = GetComponent<MeshRenderer>();

        Make_Mesh();

        if (_tile_markers != null)
        {
            foreach (GameObject tile in _tile_markers)
            {
                Destroy(tile);
            }
            _tile_markers.Clear();
        }
        _tile_markers = new List<GameObject>();
        for (int i = 0; i < MAX_TILES; i++)
        {
            _tile_markers.Add(Instantiate(_tile_marker_prefab));
            _tile_markers[i].transform.SetParent(this.transform);
            _tile_markers[i].SetActive(false);
        }
    }

    private void Update()
    {
        if (_selection <= 0)
        {
            _selection = 0;
        }
        else if (_selection >= _num_points)
        {
            _selection = _num_points - 1;
        }

        if (_radius <= 1f)
        {
            _radius = 1f;
        }

        Mathf.Clamp(_num_points, 50, 2500);

        if (_current_points != _num_points ||
            _current_radius != _radius ||
            _current_seperation != _tile_seperation ||
            _current_mesh_type != _mesh_type_shown)
        {
            _planet = new Planet(_num_points, _radius);
            _current_radius = _radius;
            _current_points = _num_points;
            _current_seperation = _tile_seperation;
            _current_mesh_type = _mesh_type_shown;

            _planet.Generate_Planet(_tile_seperation);
            Make_Mesh();

            // Add tile markers for some reason?
            if (_tile_marker_prefab != null)
            {
                foreach (GameObject tile in _tile_markers)
                {
                    tile.SetActive(false);
                }

                for (int i = 0; i < _num_points; i++)
                {
                    _tile_markers[i].transform.position = transform.TransformPoint((_planet.Tiles[i].Position).normalized * _radius * 1.001f);
                    _tile_markers[i].transform.forward = transform.position - transform.TransformPoint(_planet.Tiles[i].Position);
                    _tile_markers[i].SetActive(true);
                    _tile_markers[i].transform.GetChild(0).GetComponent<Text>().text = _planet.Tiles[i].Index.ToString();

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
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            // Draw debugs for selected tile
            Gizmos.color = Color.white;
            Vector3 pos = transform.TransformPoint(_planet.Tiles[_selection].Position);
            Gizmos.DrawSphere(pos, 0.02f);
            Vector3 Normal = (pos - this.transform.position).normalized;
            Gizmos.DrawLine(pos, pos + Normal);
            for (int i = 0; i < _planet.Tiles[_selection].Neighbors.Count; i++)
            {
                Gizmos.DrawSphere(transform.TransformPoint(_planet.Tiles[_selection].Neighbors[i].Position), 0.015f);
                Gizmos.DrawSphere(transform.TransformPoint(_planet.Tiles[_selection].Extents[i]), 0.001f);

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
                break;
            case (_mesh_type.RAW):
                _mesh = new Mesh();
                _mesh.name = "Raw Mesh";
                _mesh.vertices = _planet.Tile_Positions.ToArray();
                _mesh.triangles = _planet.Default_Triangles;
                _filter.mesh = _mesh;
                _mesh.RecalculateNormals();
                break;
        }
    }
    #endregion
}
