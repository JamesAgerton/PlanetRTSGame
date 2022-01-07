

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

                Vector3 pos = transform.InverseTransformPoint(_planet.Tiles[i].Position.normalized);
                float Lat = Point_To_Lat(pos);
                float Long = Point_To_Long(pos);
                string LatLong = i.ToString() + "\n" + Long.ToString() + "\n" + Lat.ToString();
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
            //Draw Map
            Gizmos.DrawLine(new Vector3(0, -90, 0), new Vector3(0, 90, 0));
            Gizmos.DrawLine(new Vector3(0, -90, 0), new Vector3(360, -90, 0));
            Gizmos.DrawLine(new Vector3(0, 90, 0), new Vector3(360, 90, 0));
            Gizmos.DrawLine(new Vector3(360, -90, 0), new Vector3(360, 90, 0));

            Vector3 latlonga, latlongb;
            for(int j = 0; j < Planet.Tiles.Count; j++)
            {
                for(int i = 0; i < Planet.Tiles[j].Corners.Count; i++)
                {
                    Vector3 tilepos = new Vector3(Point_To_Long(Planet.Tiles[j].Position) + 180f,
                        Point_To_Lat(Planet.Tiles[j].Position));
                    if(i == 0)
                    {
                        latlonga = new Vector3(
                            Planet.Tiles[j].Point_To_Long(Planet.Tiles[j].Corners[Planet.Tiles[j].Corners.Count - 1]) + 180f, 
                            Planet.Tiles[j].Point_To_Lat(Planet.Tiles[j].Corners[Planet.Tiles[j].Corners.Count - 1])
                            );
                        latlongb = new Vector3(
                            Planet.Tiles[j].Point_To_Long(Planet.Tiles[j].Corners[0]) + 180f, 
                            Planet.Tiles[j].Point_To_Lat(Planet.Tiles[j].Corners[0])
                            );
                    }
                    else
                    {
                        latlonga = new Vector3(
                            Planet.Tiles[j].Point_To_Long(Planet.Tiles[j].Corners[i]) + 180f,
                            Planet.Tiles[j].Point_To_Lat(Planet.Tiles[j].Corners[i])
                            );
                        latlongb = new Vector3(
                            Planet.Tiles[j].Point_To_Long(Planet.Tiles[j].Corners[i - 1]) + 180f,
                            Planet.Tiles[j].Point_To_Lat(Planet.Tiles[j].Corners[i - 1])
                            );
                    }

                    if (j != 0 || j != Planet.Tiles.Count - 1)
                    {
                        if (Vector3.Distance(latlonga, tilepos) > 300f)
                        {
                            if(tilepos.x - latlonga.x > 0)
                            {
                                latlonga.x += 360f;
                            }
                            else
                            {
                                latlonga.x -= 360f;
                            }
                        }
                        if (Vector3.Distance(latlongb, tilepos) > 300f)
                        {
                            if(tilepos.x - latlongb.x > 0)
                            {
                                latlongb.x += 360f;
                            }
                            else
                            {
                                latlongb.x -= 360f;
                            }
                        }
                    }
                    //else
                    //{
                    //    //if(latlonga.x > 350 && latlonga.y > 80f)
                    //    //{
                    //    //    Debug.DrawLine(latlonga, new Vector3(360, 90, 0));
                    //    //}
                    //}

                    Debug.DrawLine(latlonga, latlongb);
                }
            }

            //Draw touch
            Gizmos.color = Color.blue;
            if (Input.GetMouseButton(0))
            {
                Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if(Physics.Raycast(inputRay, out hit))
                {
                    Vector3 point = transform.InverseTransformPoint(hit.point);

                    Gizmos.DrawLine(Camera.main.transform.position, point);
                    Gizmos.DrawWireSphere(point, 0.03f);
                    float Lat = Point_To_Lat(point);
                    float Long = Point_To_Long(point);
                    Gizmos.DrawWireSphere(new Vector3(Long + 180f, Lat, 0f), 1f);

                    Handles.Label(point, Long.ToString() + "\n" + Lat.ToString());
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
            //Vector3 Normal = (pos - this.transform.position).normalized;
            //Gizmos.DrawLine(pos, pos + Normal);
            for (int i = 0; i < _planet.Tiles[_selection].Neighbors.Count; i++)
            {
                for (int j = 0; j < _planet.Tiles[_selection].Corners.Count; j++)
                {
                    Gizmos.color = Color.Lerp(Color.blue, Color.white, (float)j / (float)_planet.Tiles[_selection].Corners.Count);
                    Gizmos.DrawWireSphere(transform.TransformPoint(_planet.Tiles[_selection].Corners[j]), 0.025f);
                    Handles.Label(transform.TransformPoint(_planet.Tiles[_selection].Corners[j]), j.ToString());
                }
                Handles.Label(transform.TransformPoint(_planet.Tiles[_selection].Extents[i]), i.ToString());
                Handles.Label(transform.TransformPoint(_planet.Tiles[_selection].Neighbors[i].Position), i.ToString());
            }
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
            //TouchCell(hit.point);
            TouchCell(hit.triangleIndex);
        }
    }

    void TouchCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        int tile = _planet.Point_To_Tile(position);

        if(tile >= 0)
        {
            _selection = tile;
        }

        Debug.Log("touched at " + tile);
    }

    void TouchCell(int triangleIndex)
    {
        int tile = _planet.Point_To_Tile_Triangle(triangleIndex);

        if(tile >= 0)
        {
            _selection = tile;
        }

        Debug.Log("touched at " + tile + " triangle: " + triangleIndex);
    }

    public float Point_To_Lat(Vector3 point)
    {
        float Lat; // = Mathf.Asin(point.y) * Mathf.Rad2Deg;

        Lat = 90f - Vector3.Angle(Vector3.up, point.normalized);

        return Lat;
    }

    public float Point_To_Long(Vector3 point)
    {
        float Long; // = Mathf.Atan2(point.z, point.x) * Mathf.Rad2Deg;

        Long = Vector3.Angle(Vector3.forward, Vector3.ProjectOnPlane(point.normalized, Vector3.up));
        Vector3 A = -Vector3.forward;
        Vector3 B = (Vector3.zero - Vector3.ProjectOnPlane(point.normalized, Vector3.up)).normalized;
        if ((Vector3.Cross(B, A).normalized - Vector3.up).magnitude < 0.001f)
        {
            Long *= -1f;
        }

        return Long;
    }
    #endregion
}
