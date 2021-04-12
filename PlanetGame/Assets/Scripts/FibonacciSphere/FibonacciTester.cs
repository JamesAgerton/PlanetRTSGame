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
    [SerializeField]
    float       _radius             = 1f;
    [SerializeField]
    int         _selection          = 25;
    [SerializeField]
    bool        _raw_mesh           = true;

    float       _current_radius     = 1f;
    float       _current_points     = 100;

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
        _planet.Generate_Planet();
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

        if(_current_points != _num_points || _current_radius != _radius)
        {
            _planet                 = new Planet(_num_points, _radius);
            _current_radius         = _radius;
            _current_points         = _num_points;

            _planet.Generate_Planet();
            Make_Mesh();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color                = Color.white;
        Vector3 pos                 = _planet.Tiles[_selection].Position;

        Gizmos.DrawSphere(transform.TransformPoint(pos), 0.01f);

        Vector3 Normal              = _planet.Tiles[_selection].Position - this.transform.position;
        Gizmos.DrawLine(pos, pos + transform.TransformPoint(Normal));

        //Find right and left direction from intermediate positions
        List<Vector3> int_poss      = new List<Vector3>();
        List<Vector3> int_rights    = new List<Vector3>();
        List<Vector3> int_lefts     = new List<Vector3>();
        for(int i = 0; i < _planet.Tiles[_selection].Neighbors.Count; i++)
        {
            Vector3 N               = _planet.Tiles[_selection].Neighbors[i].Position;
            Vector3 int_pos         = pos + transform.TransformPoint(Vector3.ProjectOnPlane(Vector3.Lerp(pos, N, 0.49f), Normal));
            
            int_poss.Add(int_pos);

            Gizmos.color            = Color.white;
            Gizmos.DrawWireSphere(N, 0.015f);
            //Gizmos.DrawWireSphere(intermediate_position, 0.01f);

            Vector3 a               = pos - int_pos;
            Vector3 right           = Vector3.Cross(a, Normal);
            Vector3 left            = Vector3.Cross(Normal, a);

            int_rights.Add(right);
            int_lefts.Add(left);

            Gizmos.color            = Color.magenta;
            Gizmos.DrawLine(int_pos, int_pos + right * 0.5f);
            Gizmos.color            = Color.green;
            Gizmos.DrawLine(int_pos, int_pos + left * 0.5f);
            Handles.color = Color.white;
            Handles.Label(int_pos, i.ToString());
        }

        //Find corners
        //all int_rights should intersect an int_left
        //the closest intersection should be the corner
        
        List<Vector3> corners       = new List<Vector3>();
        for(int i = 0; i < int_poss.Count; i++)
        {

            Vector3 intersection = Vector3.zero;
            Vector3 linePoint1, lineVec1, linePoint2, lineVec2;

            linePoint1              = int_poss[i];
            lineVec1                = int_rights[i];

            float minDist           = float.PositiveInfinity;
            //for every left
            for(int j = 0; j < int_poss.Count; j++)
            {
                if(j != i)
                {
                    linePoint2  = int_poss[j];
                    lineVec2    = int_rights[j];

                    Vector3 curr_intersection;
                    Vector3 curr_intersection2;
                    //LineLineIntersection(out curr_intersection, linePoint1, lineVec1, linePoint2, lineVec2);
                    ClosestPointsOnTwoLines(out curr_intersection, out curr_intersection2, linePoint1, lineVec1, linePoint2, lineVec2);
                    float curr_dist = Vector3.Distance(curr_intersection, linePoint1);
                    bool exists     = false;
                    foreach(Vector3 corner in corners)
                    {
                        if(corner == curr_intersection)
                        {
                            exists  = true;
                        }
                    }
                    if (curr_dist < minDist && !exists && curr_intersection != Vector3.zero)
                    {
                        minDist     = curr_dist;
                        intersection = curr_intersection;
                    }
                }
            }

            Debug.Log(i + " corner");
            corners.Add(intersection);
        }
        for (int i = 0; i < corners.Count; i++)
        {
            Gizmos.color            = Color.Lerp(Color.blue, Color.red, (float)i / (float)corners.Count);
            Handles.color           = Color.green;
            Handles.Label(corners[i], i.ToString());
            Gizmos.DrawWireSphere(corners[i], 0.01f);
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
            _mesh = new Mesh();
            _mesh.name = "mesh";

            Vector3[] verts = _planet.Tile_Positions.ToArray();
            _mesh.vertices = verts;
            _mesh.triangles = _planet.Default_Triangles;
            _mesh.RecalculateNormals();

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

    void Make_Planet_Tile_Mesh()
    {
        if (_planet.Tiles_Mesh)
        {
            _mesh = _planet.Tiles_Mesh;
            _mesh.name = "Tile Mesh";

            _mesh.vertices = _planet.Tile_Verts.ToArray();
            _mesh.triangles = _planet.Tile_Triangles.ToArray();
            _mesh.RecalculateNormals();

            _filter.mesh = _mesh;
        }
    }

    public static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {
        Vector3 lineVec3 = linePoint2 - linePoint1;
        Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

        float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

        //is coplanar and not parrallel
        if (Mathf.Abs(planarFactor) < 0.0001f && crossVec1and2.sqrMagnitude > 0.0001f)
        {
            float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
            intersection = linePoint1 + (lineVec1 * s);
            return true;
        }
        else
        {
            Debug.Log("Not coplanar");
            intersection = Vector3.zero;
            return false;
        }
    }

    public static bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {

        closestPointLine1 = Vector3.zero;
        closestPointLine2 = Vector3.zero;

        float a = Vector3.Dot(lineVec1, lineVec1);
        float b = Vector3.Dot(lineVec1, lineVec2);
        float e = Vector3.Dot(lineVec2, lineVec2);

        float d = a * e - b * b;

        //lines are not parallel
        if (d != 0.0f)
        {

            Vector3 r = linePoint1 - linePoint2;
            float c = Vector3.Dot(lineVec1, r);
            float f = Vector3.Dot(lineVec2, r);

            float s = (b * f - c * e) / d;
            float t = (a * f - c * b) / d;

            closestPointLine1 = linePoint1 + lineVec1 * s;
            closestPointLine2 = linePoint2 + lineVec2 * t;

            return true;
        }

        else
        {
            Debug.Log("not parallel");
            return false;
        }
    }
    
    public static bool AreLineSegmentsCrossing()
    {
        //TODO: Find closest point between two line segments, then take the midpoint of the resulting line as the corner
        return false;
    }
    #endregion
}
