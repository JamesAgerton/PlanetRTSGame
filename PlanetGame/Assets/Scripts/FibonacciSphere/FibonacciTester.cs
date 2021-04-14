using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Planets;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class FibonacciTester : MonoBehaviour
{
    #region Variables (PRIVATE)
    [SerializeField]
    int         _num_points         = 100;
    [SerializeField, Range(0f, 0.2f)]
    float       _tile_seperation    = 0.02f;
    [SerializeField]
    float       _radius             = 1f;
    [SerializeField]
    int         _selection          = 25;
    [SerializeField]
    bool        _raw_mesh           = true;

    float       _current_radius     = 1f;
    float       _current_points     = 100;
    float       _current_seperation = 0.02f;

    Planet          _planet;
    Mesh            _mesh;
    MeshFilter      _filter;
    MeshRenderer    _renderer;
    #endregion

    #region Properties (PUBLIC)

    #endregion

    #region Unity Methods
    // Start is called before the first frame update
    private void Start()
    {
        _planet                     = new Planet(_num_points, _radius);
        _planet.Generate_Planet(_tile_seperation);
        _filter                     = GetComponent<MeshFilter>();
        _renderer                   = GetComponent<MeshRenderer>();

        _renderer.sharedMaterial    = new Material(Shader.Find("VR/SpatialMappingWireframe"));

        Make_Mesh();
    }

    private void Update()
    {
        if(_selection < 0)
        {
            _selection              = 0;
        }
        else if(_selection > _num_points)
        {
            _selection              = _num_points - 1;
        }

        if(_num_points < 50)
        {
            _num_points             = 50;
        }
        else if(_num_points > 2500)
        {
            _num_points             = 2500;
        }

        if(_current_points != _num_points || _current_radius != _radius || _current_seperation != _tile_seperation)
        {
            _planet                 = new Planet(_num_points, _radius);
            _current_radius         = _radius;
            _current_points         = _num_points;
            _current_seperation     = _tile_seperation;

            _planet.Generate_Planet(_tile_seperation);
            Make_Mesh();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Vector3 pos = _planet.Tiles[_selection].Position;

        Gizmos.DrawSphere(transform.TransformPoint(pos), 0.01f);

        Vector3 Normal = (_planet.Tiles[_selection].Position - this.transform.position).normalized;
        Gizmos.DrawLine(pos, pos + transform.TransformPoint(Normal));

        //Find right and left direction from intermediate positions?
        List<Vector3> int_poss      = _planet.Tiles[_selection].Extents;
        List<Vector3> int_rights    = new List<Vector3>();
        List<Vector3> int_lefts     = new List<Vector3>();

        //Find right and left directions
        for (int i = 0; i < int_poss.Count; i++)
        {
            Vector3 a = pos - int_poss[i];
            Vector3 right = Vector3.Cross(a, Normal).normalized;
            Vector3 left = Vector3.Cross(Normal, a).normalized;

            int_rights.Add(right);
            int_lefts.Add(left);
        }

        float max_int_dist = 0f;
        float min_int_dist = float.PositiveInfinity;
        foreach (Vector3 a in int_poss)
        {
            float f = Vector3.Distance(a, pos);
            if (f > max_int_dist)
            {
                max_int_dist = f;
            }
            else if(f < min_int_dist)
            {
                min_int_dist = f;
            }
        }
        float mid_int_dist = ((max_int_dist - min_int_dist) / 2f) + min_int_dist;

        float used_dist = mid_int_dist;

        for (int i = 0; i < int_poss.Count; i++)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(int_poss[i], int_poss[i] + int_rights[i] * used_dist);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(int_poss[i], int_poss[i] + int_lefts[i] * used_dist);
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(int_poss[i], 0.01f);
            Handles.Label(int_poss[i], i.ToString());
        }

        //Find corners
        List<Vector3> corners       = _planet.Tiles[_selection].Calculate_Corners();
        Debug.Log(corners.Count + " " + int_poss.Count);

        for (int i = 0; i < corners.Count; i++)
        {
            if (corners.Count != _planet.Tiles[_selection].Neighbors.Count)
            {
                Gizmos.color = Color.red;
            }
            else
            {
                Gizmos.color = Color.Lerp(Color.blue, Color.red, (float)i / (float)corners.Count);
            }

            Handles.Label(corners[i], i.ToString());
            Gizmos.DrawSphere(corners[i], 0.01f);
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
    #endregion

    #region Methods
    void Make_Mesh()
    {
        if (_raw_mesh)
        {
            _mesh = _planet.Tiled_Mesh;

            _filter.mesh = _mesh;
        }
        else
        {
            _mesh = new Mesh();
            _mesh.name = "No Mesh";
            _mesh.vertices = new Vector3[0];
            _mesh.triangles = new int[0];
            _filter.mesh = _mesh;
        }
    }
    #endregion
}
