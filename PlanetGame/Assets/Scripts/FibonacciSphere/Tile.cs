using UnityEngine;
using System.Collections.Generic;
using System;

namespace Planets
{
    public class Tile : IEquatable<Tile>, IComparable<Tile>
    {
        #region Variables
        private int             _index;         // Tile index number
        private Vector3         _position;      // Relative Position of Tile
        private List<Tile>      _neighbors;     // Unordered list of neighbor tiles
        private List<bool>      _bridged_neighbors; //The neighbors which have been bridged.
        private List<Vector3>   _extents;       // Edges of tile space in counterclockwise order
        private List<Vector3>   _corners;
        #endregion

        #region Properties
        public int              Index               => _index;
        public Vector3          Position            => _position;
        public List<Tile>       Neighbors           => _neighbors;
        public List<bool>       Bridged_Neighbors   => _bridged_neighbors;
        public List<Vector3>    Extents             => _extents;
        public List<Vector3>    Corners             => _corners;
        public Vector3 Normal;
        #endregion

        public Tile(int i, Vector3 pos)
        {
            _neighbors  = new List<Tile>();
            _bridged_neighbors = new List<bool>();
            _index      = i;
            _position   = pos;
            _corners    = new List<Vector3>();
        }

        #region Methods
        /// <summary>
        /// Adds a Tile to the neighbor list of this Tile
        /// </summary>
        /// <param name="neighbor"></param>
        public void Add_Neighbor(Tile neighbor)
        {
            _neighbors.Add(neighbor);
            bool newBool = false;
            _bridged_neighbors.Add(newBool);

        }

        /// <summary>
        /// Returns the neighbor list index, returns -1 if the tile is not a neighbor.
        /// </summary>
        /// <param name="neighbor"></param>
        /// <returns></returns>
        public int Is_Neighbor(Tile neighbor)
        {
            for (int i = 0; i < _neighbors.Count; i++)
            {
                if(neighbor.Index == _neighbors[i].Index)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Sets neighbor list to a given List of Tiles
        /// </summary>
        /// <param name="neighbors"></param>
        public void Set_Neighbors(List<Tile> neighbors)
        {
            _neighbors  = neighbors;
            foreach(Tile tile in neighbors)
            {
                bool newBool = false;
                _bridged_neighbors.Add(newBool);
            }
        }

        /// <summary>
        /// Sets the index number of this Tile
        /// </summary>
        /// <param name="i"></param>
        public void Set_Index(int i)
        {
            _index      = i;
        }

        /// <summary>
        /// Sets the position vector of this Tile.
        /// </summary>
        /// <param name="pos"></param>
        public void Set_Position(Vector3 pos)
        {
            _position   = pos;
        }

        /// <summary>
        /// Creates the extent positions of the Tile, not the corners. Extent points are stored in _extents of the Tile 
        /// in Neighbor order.
        /// </summary>
        /// <param name="Planet_Pos"></param>
        /// <param name="Ext_Frac"></param>
        /// <returns></returns>
        public List<Vector3> Set_Extents(float Ext_Frac)
        {
            if(_neighbors.Count < 3)
            {
                string msg = "Not enough neighbors in Tile: " + _index;
                Debug.LogError(msg);
                return null;
            }

            Vector3 pos             = _position;
            Vector3 Normal          = (_position - Vector3.zero).normalized;
            List<Vector3> ext_poss  = new List<Vector3>();
            _extents                = new List<Vector3>();

            /*  Grab extent points  */
            for (int i = 0; i < _neighbors.Count; i++)
            {
                Vector3 ext_pos     = pos + Vector3.ProjectOnPlane(Vector3.Lerp(pos, _neighbors[i].Position, Ext_Frac), Normal);
                
                ext_poss.Add(ext_pos);
            }
            _extents = ext_poss;
            return _extents;
        }

        /// <summary>
        /// Sorts neighbors into CC order.
        /// </summary>
        /// <returns></returns>
        public List<Tile> Sort_Neighbors()
        {
            if(_neighbors.Count < 3)
            {
                string msg = "Not enough Neighbors in Tile: " + _index;
                Debug.LogError(msg);
                return null;
            }

            Vector3 pos                         = _position;
            Vector3 Normal                      = (_position - Vector3.zero).normalized;
            List<Vector3> neighbor_positions    = new List<Vector3>();
            List<Tile> sorted_neighbors         = new List<Tile>();

            int current_point                   = 0;    //This defines which extent point is currently selected to measure from
                                                        //  It's fine that this isn't in the order defined by the original neighbor
                                                        //  list because they have the same number of points anyway.

            //Project neighbors onto normal plane
            for (int i = 0; i < _neighbors.Count; i++)
            {
                Vector3 neighbor_pos            = pos + Vector3.ProjectOnPlane(_neighbors[i].Position, Normal);
                neighbor_positions.Add(neighbor_pos);
            }

            sorted_neighbors.Add(_neighbors[0]);    //First neighbor is always first
            for (int i = 0; i < _neighbors.Count; i++)
            {
                float minAngle                  = float.PositiveInfinity;   //used to find smallest angle of the next checks

                Vector3 A                       = (pos - neighbor_positions[current_point]).normalized;   //Define first vector pointing from tile origin to neighbor
                int C                           = 0;

                for (int j = 0; j < _neighbors.Count; j++)
                {
                    if (j != current_point)  //No point in checking the angle between a neighbor and itself
                    {
                        //Define second vector pointing from tile origin to a different extent point
                        Vector3 B = (pos - neighbor_positions[j]).normalized;

                        //Cross returns a normal vector following the left hand rule, if that normal is the same as the Normal of the
                        //  planet, then the angle being measured is on the correct side.
                        //  Also don't need to check the angle of the last extent being measured.
                        if ((Vector3.Cross(B, A).normalized - Normal).magnitude < 0.0001f && i != _neighbors.Count - 1)
                        {
                            float f = Vector3.Angle(A, B);

                            if (f < minAngle)
                            {
                                minAngle = f;
                                C = j;
                                current_point = j;
                            }
                        }
                    }
                }

                if (C != 0)  //If it didn't redefine C then there is either a problem or this is the last neighbor
                {
                    //Now C should be nearest neighbor in the CC direction so add it to the list
                    sorted_neighbors.Add(_neighbors[C]);

                    //Debug.Log(i.ToString() + " -> " + decided.ToString() + 
                    //    " \t : Angle: \t " + minAngle + 
                    //    " \t : Cross: \t " + (Vector3.Cross(A, pos - C).normalized));
                }
                else if (i != _neighbors.Count - 1)
                {
                    sorted_neighbors.Add(_neighbors[0]);
                }
            }

            _neighbors = sorted_neighbors;
            return sorted_neighbors;
        }

        /// <summary>
        /// Uses magical trigonometry to find the corners between each _extent point. Then it puts them in CC order.
        /// </summary>
        /// <returns></returns>
        public List<Vector3> Calculate_Corners(float extent_frac)
        {
            //       /q\       // This is a trig problem, easy to solve.
            //    C /   \ A    // Known: x, Q, b, B, d, D | Desired: a, A, c, C, q
            //     /a_Q_c\     // Law of Sines: A/Sin(a) = C/Sin(c) = Q/Sin(q)
            // j   \b   d/   i //
            //    D \   / B    // A = Q * (Sin(a) / Sin(q)) | C = Q * (Sin(c) / Sin(q))
            //       \x/       // What I really want is the point at q, I can get it with the length of A or C.

            if (_neighbors.Count < 3)
            {
                string msg = "Not enough neighbors in Tile: " + _index;
                Debug.LogError(msg);
                return null;
            }

            float fraction = (extent_frac < 0.5f) ? extent_frac : 0.49999f;
            Set_Extents(fraction);
            List<Vector3> corners       = new List<Vector3>();
            Normal              = (_position - Vector3.zero).normalized;

            for (int i = 0; i < _extents.Count; i++)
            {
                //Grab knowns
                int j                   = (i < _extents.Count - 1) ? i + 1 : 0;
                Vector3 B               = (_position - _extents[j]);
                Vector3 D               = (_position - _extents[i]);
                Vector3 Q               = (_extents[i] - _extents[j]);
                //float x                 = (Vector3.Angle(B, D)) * Mathf.Deg2Rad;
                float d                 = (Vector3.Angle(_extents[i] - _extents[j], B));
                float b                 = (Vector3.Angle(_extents[j] - _extents[i], D));

                //Find a and c and q
                float a                 = (90f - b);
                float c                 = (90f - d);
                float q                 = (180f - a - c) * Mathf.Deg2Rad;


                //Debug.Log("\na:" + a + " | c:" + c + " | q:" + q + " || b:" + b + " | d:" + d); 

                //Find A
                float C = Q.magnitude * (Mathf.Sin(c * Mathf.Deg2Rad) / Mathf.Sin(q));

                Vector3 left_extent     = Vector3.Cross(_extents[i], Normal).normalized;

                Vector3 corner          = _extents[i] + (left_extent * C);
                corners.Add(corner);
                _corners.Add(corner);
            }
            Sort_Corners(ref corners);

            return corners;
        }
        private List<Vector3> Sort_Corners(ref List<Vector3> corners)
        {
            if (_corners.Count < 3)
            {
                string msg = "Not enough neighbors in Tile: " + _index;
                Debug.LogError(msg);
                return null;
            }

            Vector3 pos = _position;
            Vector3 Normal = (_position - Vector3.zero).normalized;
            List<Vector3> sorted_corners = new List<Vector3>();

            int current = 0;

            sorted_corners.Add(corners[0]);
            for (int i = 0; i < corners.Count; i++)
            {
                float minAngle = float.PositiveInfinity;

                Vector3 A = (pos - corners[current]).normalized;
                Vector3 C = Vector3.zero;

                for (int j = 0; j < corners.Count; j++)
                {
                    if(j != current)
                    {
                        Vector3 B = (pos - corners[j]).normalized;

                        if((Vector3.Cross(B, A).normalized - Normal).magnitude < 0.0001f && i != corners.Count - 1)
                        {
                            float f = Vector3.Angle(A, B);

                            if(f < minAngle)
                            {
                                minAngle = f;
                                C = corners[j];
                                current = j;
                            }
                        }
                    }
                }

                if(C != Vector3.zero)
                {
                    sorted_corners.Add(C);
                }
                else if(i != corners.Count - 1)
                {
                    sorted_corners.Add(corners[0]);
                }
            }

            _corners = sorted_corners;
            return sorted_corners;
        }

        /// <summary>
        /// Creates a simple tile mesh with a triangle between two corners and the center.
        /// </summary>
        /// <param name="Triangles"></param>
        /// <param name="Vertices"></param>
        /// <param name="extent_frac"></param>
        public void Generate_Tile_Triangles_and_Vertices(ref List<int> Triangles, ref List<Vector3> Vertices, float extent_frac)
        {
            int count = 0;

            if (Calculate_Corners(extent_frac) != null)
            {
                int start = Vertices.Count;

                Vertices.Add(_position);
                for (int i = 0; i < _corners.Count; i++)
                {
                    Vertices.Add(_corners[i]);
                }
                //Debug.Log(corners.Count + " " + extents.Count);
                for (int i = 0; i < _corners.Count; i++)
                {
                    count++;
                    int j = (i < _corners.Count - 1) ? (i + 1) : 0;

                    Triangles.Add(start);
                    Triangles.Add(start + j + 1);
                    Triangles.Add(start + i + 1);

                    //Debug.Log("Triangle: " + count + " " + start + "," + (start + j + 1) + "," + (start + i + 1));
                }
            }
        }

        //TODO: Create bridges to neighbors
        public void Generate_Tile_Bridges(ref List<int> Triangles, ref List<Vector3> Vertices)
        {
            //Should be able to use Neighbor list and Is_Neighbor to find the appropriate corners to stitch between.
        }

        public bool Is_Point_on_Tile(Vector3 point)
        {
            Vector2 point_LatLong = new Vector2(Point_To_Long(point), Point_To_Lat(point));

            List<Vector2> Corner_LatLongs = new List<Vector2>();

            if (_corners.Count >= 3)    //  Needs 3 or more corners for a polygon
            {
                for (int j = 0; j < _corners.Count; j++)
                {
                    Vector2 cLL = new Vector2(Point_To_Long(_corners[j]), Point_To_Lat(_corners[j]));
                    Corner_LatLongs.Add(cLL);
                }

                // New point to create a line segment
                Vector2 extreme = new Vector2(point_LatLong.x + 0.5f, point_LatLong.y);
                

                // Count intersection of the above line with sides of polygon
                int count = 0, i = 0;
                do
                {
                    int next = (i + 1) % _corners.Count;

                    // Check if the line segment from point_LatLong to extreme intersects
                    // with the line segment from Corner_LatLongs[i] to Corner_LatLongs[next]
                    if (doIntersect(Corner_LatLongs[i], Corner_LatLongs[next], point_LatLong, extreme))
                    {
                        // if the point 'point_LatLong' is colinear with line segment 'i-next',
                        // then check if it lies on segment. If it lies, return true,
                        // otherwise false
                        if (orientation(Corner_LatLongs[i], point_LatLong, Corner_LatLongs[next]) == 0)
                        {
                            return onSegment(Corner_LatLongs[i], point_LatLong, Corner_LatLongs[next]);
                        }

                        count++;
                    }
                    i = next;
                } while (i != 0);

                // Return true if count is odd, false otherwise
                return (count % 2 == 1);
            }
            Debug.LogError("Not enough corners, can't find if Tile contains point!");
            return false;
        }

        /// <summary>
        /// Given three colinear points p, q, r, the function checks if
        /// point q lies on line segment 'pr'
        /// Hopefully usable with lat long instead of x y
        /// </summary>
        /// <param name="p"></param>
        /// <param name="q"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        private bool onSegment(Vector2 p, Vector2 q, Vector2 r)
        {
            if (q.x <= Mathf.Max(p.x, r.x) && 
                q.x >= Mathf.Min(p.x, r.x) &&
                q.y <= Mathf.Max(p.y, r.y) && 
                q.y >= Mathf.Min(p.y, r.y))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Find orientation of triplet (p, q, r).
        /// The function returns following values
        /// 0 --> p, q, r are colinear
        /// 1 --> Clockwise
        /// 2 --> Counterclockwise
        /// </summary>
        /// <param name="p"></param>
        /// <param name="q"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        private int orientation(Vector2 p, Vector2 q, Vector2 r)
        {
            float val = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);

            if (val <= 0.0001f) return 0;    //  colinear
            return (val > 0.0001f) ? 1 : 2;  //  c or cc
        }

        /// <summary>
        /// The function return true if line segment 'p1q1' and 'p2q2' intersect.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="q1"></param>
        /// <param name="p2"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        private bool doIntersect(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
        {
            // Find the four orientations needed for general and special cases
            int o1 = orientation(p1, q1, p2);
            int o2 = orientation(p1, q1, q2);
            int o3 = orientation(p2, q2, p1);
            int o4 = orientation(p2, q2, q1);

            // General case
            if(o1 != o2 && o3 != o4)
            {
                return true;
            }

            // Special cases
            // p1, q1 and p2 are colinear and p2 lies on segment p1q1
            if(o1 == 0 && onSegment(p1, p2, q1))
            {
                return true;
            }

            // p1, q1 and p2 are colinear and q2 lies on legment p1q1
            if(o2 == 0 && onSegment(p1, q2, q1))
            {
                return true;
            }

            // p2, q2 and p1 are colinear and p1 lies on segment p2q2
            if (o3 == 0 && onSegment(p2, p1, q2))
            {
                return true;
            }

            // p2, q2 and q1 are colinear and q1 lies on segment p2q2
            if (o4 == 0 && onSegment(p2, q1, q2))
            {
                return true;
            }

            return false;   // Doesn't fall in any above case
        }

        public float Point_To_Lat(Vector3 point)
        {
            float Lat = Mathf.Asin(point.y) * Mathf.Rad2Deg;
            return Lat;
        }

        public float Point_To_Long(Vector3 point)
        {
            float Long = Mathf.Atan2(point.z, point.x) * Mathf.Rad2Deg;
            return Long;
        }

        //Equals is used in IEquatable interface: Lets a comparison to check if two Tiles are equivalent
        public bool Equals(Tile other)
        {
            if (other == null) return false;
            return (this.Index == other.Index && this._position == other.Position);
        }

        //CompareTo is used in IComparable interface: Lets the tiles be sorted by index
        public int CompareTo(Tile other)
        {
            if (other == null) return 1;
            return _index.CompareTo(other.Index);
        }
        #endregion
    }
}