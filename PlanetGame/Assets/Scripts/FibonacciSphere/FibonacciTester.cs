using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Planets;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class FibonacciTester : MonoBehaviour
{
    #region Variables (PRIVATE)
    [SerializeField]
    int _num_points = 100;
    [SerializeField]
    float _radius = 1f;
    [SerializeField]
    int _selection = 25;
    [SerializeField]
    bool _raw_mesh = true;

    float _current_radius = 1f;
    float _current_points = 100;

    Planet _planet;
    Mesh _mesh;
    MeshFilter _filter;
    MeshRenderer _renderer;
    #endregion

    #region Properties (PUBLIC)

    #endregion

    #region Unity Methods
    // Start is called before the first frame update
    private void Start()
    {
        _planet = new Planet(_num_points, _radius);
        _planet.Generate_Planet();
        _filter = GetComponent<MeshFilter>();
        _renderer = GetComponent<MeshRenderer>();

        _renderer.sharedMaterial = new Material(Shader.Find("SpatialMappingWireframe"));

        Make_Mesh();
    }

    private void Update()
    {
        if(_selection < 0)
        {
            _selection = 0;
        }else if(_selection > _num_points)
        {
            _selection = _num_points - 1;
        }

        if(_num_points < 50)
        {
            _num_points = 50;
        }

        if(_current_points != _num_points || _current_radius != _radius)
        {
            _planet = new Planet(_num_points, _radius);
            if(_planet.Generate_Planet() == -1)
            {
                Debug.LogError("Couldn't generate planet", this);
                return;
            }

            Make_Mesh();

            _current_radius = _radius;
            _current_points = _num_points;
        }
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < _planet.Tiles.Count; i++)
        {
            Gizmos.color = Color.Lerp(Color.green, Color.blue, ((float)i / (float)_planet.Tiles.Count));
            if(i == _selection)
            {
                Gizmos.color = Color.white;
                foreach(Tile tile in _planet.Tiles[_selection].Neighbors)
                {
                    Gizmos.DrawWireSphere(transform.TransformPoint(tile.Position), 0.05f);
                }
            }

            Gizmos.DrawSphere(transform.TransformPoint(_planet.Tiles[i].Position), 0.025f);
        }
    }
    #endregion

    #region Methods
    void Make_Mesh()
    {
        if (_raw_mesh)
        {
            _mesh = new Mesh();
            _mesh.name = "mesh";

            Vector3[] verts = _planet.POSITIONS.ToArray();
            _mesh.vertices = verts;
            _mesh.triangles = _planet.RAW_TRIANGLES;
            _mesh.RecalculateNormals();

            _filter.mesh = _mesh;
        }
    }
    #endregion
}
