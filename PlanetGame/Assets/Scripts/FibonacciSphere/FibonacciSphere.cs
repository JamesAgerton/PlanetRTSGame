using System.Collections.Generic;
using UnityEngine;
using GK;

namespace Planets
{
    public struct SpherePoints
    {
        public List<Vector3>    Positions;
        public int[]            Triangles;

        public SpherePoints(List<Vector3> _pos, int[] _tris)
        {
            Positions           = _pos;
            Triangles           = _tris;
        }
    }

    public class FibonacciSphere
    {
        /// <summary>
        /// This class handles creating a fibonacci sphere of points, and the triangles that connect all those pieces.
        /// </summary>
        
        #region Variables (PRIVATE)
        private DelaunayCalculator      _Delaunay_Calculator;
        private DelaunayTriangulation   _Delaunay_Triangles;
        #endregion

        #region Properties (PUBLIC)
        public float    GOLDEN_RATIO    = 1.6180339887f;  //  (Mathf.Sqrt(5) + 1) / 2;    //  ----> ~1.6180339887f
        public float    GOLDEN_ANGLE    = 2.3999632297f;  //  (2f - GOLDEN_RATIO) * (2f * Mathf.PI);  //  ----> ~2.3999632297f
        #endregion

        public FibonacciSphere()
        {
            _Delaunay_Calculator        = new DelaunayCalculator();
        }

        #region Methods

        public SpherePoints Generate_Whole_Sphere(int num_points, float radius)
        {
            int[]           triangles;
            List<Vector3>   new_positions = Generate_Fibonacci_Sphere(num_points, radius);
            triangles       = Generate_Fibonacci_Sphere_Trianges(radius, new_positions);

            Stitch_Bottom(ref triangles, ref new_positions, radius);

            SpherePoints    result      = new SpherePoints(new_positions, triangles);

            return result;
        }

        /// <summary>
        /// This creates all but the very final position in the Fibonacci Sphere.
        /// </summary>
        /// <param name="num_points"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public List<Vector3> Generate_Fibonacci_Sphere(int num_points, float radius)
        {
            List<Vector3>   positions   = new List<Vector3>();
            float           latitude,   longitude,  x,  y,  z;
            Vector3         position;
            for (int i = 0; i < num_points; i++)
            {
                float   t   = (float)i / (num_points + 1);
                latitude    = Mathf.Asin(-1f + 2f * t);
                longitude   = 2f * Mathf.PI * GOLDEN_RATIO * (float)i;

                x           = Mathf.Cos(longitude) * Mathf.Cos(latitude);
                y           = Mathf.Sin(longitude) * Mathf.Cos(latitude);
                z           = Mathf.Sin(latitude);

                position    = (new Vector3(x, y, z) * radius);

                positions.Add(Rotate_Points(position, new Vector3(0f, 0f, 90f)));
            }

            return positions;
        }

        /// <summary>
        /// This method rotates the points around the 0,0,0 position, used to rotate the spiral so its around the Y (up) axis.
        /// </summary>
        /// <param name="original_point"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        private Vector3 Rotate_Points(Vector3 original_point, Vector3 rotation)
        {
            Vector3     rot     = new Vector3(rotation.x * (Mathf.PI / 180f), rotation.y * (Mathf.PI / 180f), rotation.z * (Mathf.PI / 180f));

            float x     = Mathf.Cos(rot.x) * Mathf.Cos(rot.y) * original_point.x +
                        (Mathf.Cos(rot.x) * Mathf.Sin(rot.y) * Mathf.Sin(rot.z) - Mathf.Sin(rot.x) * Mathf.Cos(rot.z)) * original_point.y +
                        (Mathf.Cos(rot.x) * Mathf.Sin(rot.y) * Mathf.Cos(rot.z) + Mathf.Sin(rot.x) * Mathf.Sin(rot.z)) * original_point.z;
            float y     = Mathf.Sin(rot.x) * Mathf.Cos(rot.y) * original_point.x +
                        (Mathf.Sin(rot.x) * Mathf.Sin(rot.y) * Mathf.Sin(rot.z) + Mathf.Cos(rot.x) * Mathf.Cos(rot.z)) * original_point.y +
                        (Mathf.Sin(rot.x) * Mathf.Sin(rot.y) * Mathf.Cos(rot.z) - Mathf.Cos(rot.x) * Mathf.Sin(rot.z)) * original_point.z; ;
            float z     = (-1f * Mathf.Sin(rot.y)) * original_point.x +
                        (Mathf.Cos(rot.y) * Mathf.Sin(rot.z)) * original_point.y +
                        (Mathf.Cos(rot.y) * Mathf.Cos(rot.z)) * original_point.z;

            return      new Vector3(x, y, z);
        }

        public List<Vector2> Stereograph_Project_Sphere(float radius, List<Vector3> sphere_points)
        {
            List<Vector2>   positions   = new List<Vector2>();

            for (int i = 0; i < sphere_points.Count; i++)
            {
                positions.Add(Stereograph_Project_Point(sphere_points[i], radius));
            }

            return          positions;
        }
        public Vector2 Stereograph_Project_Point(Vector3 point3, float radius)
        {
            float   x       = point3.z / (point3.y + radius);
            float   y       = point3.x / (point3.y + radius);

            return          new Vector2(x, y);
        }

        public int[] Generate_Fibonacci_Sphere_Trianges(float radius, List<Vector3> positions)
        {
            List<Vector2>   flatPos     = new List<Vector2>();
            flatPos         = Stereograph_Project_Sphere(radius, positions);

            _Delaunay_Triangles         = _Delaunay_Calculator.CalculateTriangulation(flatPos);
            int[]           triangles   = _Delaunay_Triangles.Triangles.ToArray();

            return          triangles;
        }

        public void Stitch_Bottom(ref int[] tris, ref List<Vector3> positions, float radius)
        {
            Vector3     sPole   = new Vector3(0f, 0f - radius, 0f);

            //Add south pole to positions list
            positions.Add(sPole);

            //Stitch the triangles together properly ... hopefully
            int[]   newTris     = new int[tris.Length + 15];
            //initialize newTris with original triangles
            for(int i = 0; i < tris.Length; i++)
            {
                newTris[i]      = tris[i];
            }
            
            newTris[tris.Length]        = positions.Count - 4;
            newTris[tris.Length + 1]    = positions.Count - 2;
            newTris[tris.Length + 2]    = positions.Count - 1;
            
            newTris[tris.Length + 3]    = positions.Count - 6;
            newTris[tris.Length + 4]    = positions.Count - 4;
            newTris[tris.Length + 5]    = positions.Count - 1;
            
            newTris[tris.Length + 6]    = positions.Count - 3;
            newTris[tris.Length + 7]    = positions.Count - 6;
            newTris[tris.Length + 8]    = positions.Count - 1;
            
            newTris[tris.Length + 9]    = positions.Count - 5;
            newTris[tris.Length + 10]   = positions.Count - 3;
            newTris[tris.Length + 11]   = positions.Count - 1;

            newTris[tris.Length + 12]   = positions.Count - 2;
            newTris[tris.Length + 13]   = positions.Count - 5;
            newTris[tris.Length + 14]   = positions.Count - 1;

            tris    = newTris;
        }
        #endregion
    }
}

