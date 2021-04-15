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
        private List<Vector3>   _extents;       // Edges of tile space in counterclockwise order
        private List<Vector3>   _corners;
        #endregion

        #region Properties
        public int              Index       => _index;
        public Vector3          Position    => _position;
        public List<Tile>       Neighbors   => _neighbors;
        public List<Vector3>    Extents     => _extents;
        public List<Vector3>    Corners    => _corners;
        #endregion

        public Tile(int i, Vector3 pos)
        {
            _neighbors  = new List<Tile>();
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
        }

        /// <summary>
        /// Checks if the given tile is in the neighbor list of this Tile
        /// </summary>
        /// <param name="neighbor"></param>
        /// <returns></returns>
        public bool Is_Neighbor(Tile neighbor)
        {
            foreach(Tile tile in Neighbors)
            {
                if(tile._index == neighbor._index)
                {
                    return  true;
                }
            }
            return  false;
        }

        /// <summary>
        /// Sets neighbor list to a given List of Tiles
        /// </summary>
        /// <param name="neighbors"></param>
        public void Set_Neighbors(List<Tile> neighbors)
        {
            _neighbors  = neighbors;
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

            float fraction = (extent_frac < 0.5f) ? extent_frac : 0.49999f;
            Set_Extents(fraction);
            List<Vector3> corners       = new List<Vector3>();
            Vector3 Normal              = (_position - Vector3.zero).normalized;

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

        public void Generate_Tile_Triangles_and_Vertices(ref List<int> Triangles, ref List<Vector3> Vertices, float extent_frac)
        {
            int count = 0;

            Calculate_Corners(extent_frac);

            int start = Vertices.Count;

            Vertices.Add(_position);
            for (int i = 0; i < _corners.Count; i++)
            {
                Vertices.Add(_corners[i]);
            }
            //Debug.Log(corners.Count + " " + extents.Count);
            for(int i = 0; i < _corners.Count; i++)
            {
                count++;
                int j = (i < _corners.Count - 1) ? (i + 1) : 0;

                Triangles.Add(start);
                Triangles.Add(start + j + 1);
                Triangles.Add(start + i + 1);

                //Debug.Log("Triangle: " + count + " " + start + "," + (start + j + 1) + "," + (start + i + 1));
            }
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