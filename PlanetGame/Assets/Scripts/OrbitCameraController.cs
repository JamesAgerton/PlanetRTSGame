using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Planets;

public class OrbitCameraController : MonoBehaviour
{
    #region Variables (PRIVATE)
    [SerializeField]
    Transform _camera_transform;
    [SerializeField]
    FibonacciTester _target_planet;
    [SerializeField]
    float _zoom_speed = 1f;
    [SerializeField, Range(1f, 360f)]
    float _rotation_speed = 90f;
    [SerializeField, Range(-89f, 89f)]
    float _min_vertical_angle = -30f, _max_vertical_angle = 60f;
    [SerializeField]
    float _max_distance = 5f;
    [SerializeField]
    float _min_distance = -0.5f;

    Camera _camera;
    Vector3 _focus_point;

    Planet _planet;
    Transform _planet_transform;
    float _radius = 1f;

    float _current_dist = 2f;
    Vector2 _orbit_angles = new Vector2(45f, 0f);
    #endregion

    #region Properties (PUBLIC)

    #endregion

    #region Unity Methods
    // Start is called before the first frame update
    private void Awake()
    {
        if(_planet == null)
        {
            _target_planet = GameObject.FindGameObjectWithTag("Planet").GetComponent<FibonacciTester>();
        }
        _planet = _target_planet.Planet;
        _planet_transform = _target_planet.transform;
        _radius = _target_planet.Radius;

        _focus_point = _planet_transform.position;

        if(_camera_transform == null)
        {
            _camera_transform = Camera.main.transform;
        }
        _camera = _camera_transform.GetComponent<Camera>();
        _camera_transform.position = new Vector3(_radius + _current_dist, 0f, 0f);
        _camera_transform.localRotation = Quaternion.Euler(_orbit_angles);
    }

    private void OnValidate()
    {
        if(_max_vertical_angle < _min_vertical_angle)
        {
            _max_vertical_angle = _min_vertical_angle;
        }
    }

    private void LateUpdate()
    {
        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            _current_dist -= _zoom_speed;
        }
        else if(Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            _current_dist += _zoom_speed;
        }
        _current_dist = Mathf.Clamp(_current_dist, _min_distance + _radius, _max_distance + _radius);

        UpdateFocusPoint();
        Quaternion lookRotation = _camera_transform.localRotation; ;
        if (Input.GetKey(KeyCode.Mouse1))
        {
            if (ManualRotation())
            {
                ConstrainAngles();
                lookRotation = Quaternion.Euler(_orbit_angles);
            }
        }
        Vector3 lookDirection = lookRotation * Vector3.forward;
        Vector3 lookPosition = (_focus_point - lookDirection).normalized * (_current_dist);
        _camera_transform.SetPositionAndRotation(lookPosition, lookRotation);
    }
    #endregion

    #region Methods
    void UpdateFocusPoint()
    {
        Vector3 targetPoint = _planet_transform.position;
        _focus_point = targetPoint;
    }
    bool ManualRotation()
    {
        Vector2 input = new Vector2(
            Input.GetAxis("Mouse Y") * (-1f),
            Input.GetAxis("Mouse X")
            );
        const float e = 0.001f;
        if(input.x < -e || input.x > e || input.y < -e || input.y > e)
        {
            _orbit_angles += _rotation_speed * Time.unscaledDeltaTime * input;
            return true;
        }
        return false;
    }
    void ConstrainAngles()
    {
        _orbit_angles.x =
            Mathf.Clamp(_orbit_angles.x, _min_vertical_angle, _max_vertical_angle);
        if(_orbit_angles.y < 0f)
        {
            _orbit_angles.y += 360f;
        }
        else if(_orbit_angles.y >= 360f)
        {
            _orbit_angles.y -= 360f;
        }
    }
    #endregion
}
