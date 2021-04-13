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
        #endregion

        #region Properties
        public int              Index       => _index;
        public Vector3          Position    => _position;
        public List<Tile>       Neighbors   => _neighbors;
        public List<Vector3>    Extents     => _extents;
        #endregion

        public Tile(int i, Vector3 pos)
        {
            _neighbors  = new List<Tile>();
            _index      = i;
            _position   = pos;
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
        /// in CC order.
        /// </summary>
        /// <param name="Planet_Pos"></param>
        /// <param name="Ext_Frac"></param>
        /// <returns></returns>
        public List<Vector3> Set_Extents(Vector3 Planet_Pos, float Ext_Frac)
        {
            Vector3 pos             = _position;
            Vector3 Normal          = (_position - Planet_Pos).normalized;
            List<Vector3> ext_poss  = new List<Vector3>();
            _extents                = new List<Vector3>();

            /*  Grab extent points  */
            for (int i = 0; i < _neighbors.Count; i++)
            {
                Vector3 ext_pos     = pos + Vector3.ProjectOnPlane(Vector3.Lerp(pos, _neighbors[i].Position, Ext_Frac), Normal);
                
                ext_poss.Add(ext_pos);
            }

            /*  Re order extents to be in counterclockwise order    */
            int current_point               = 0;        //This defines which extent point is currently selected to measure from
                                                        //  It's fine that this isn't in the order defined by the original neighbor
                                                        //  list because they have the same number of points anyway.
            
            _extents.Add(ext_poss[0]);  //First extent point is always first

            for(int i = 0; i < ext_poss.Count; i++)
            {
                float minAngle              = float.PositiveInfinity;               //used to find smallest angle of next checks
                
                Vector3 A                   = (pos - ext_poss[current_point]).normalized; //Define first vector pointing from tile origin to extent point
                Vector3 C                   = Vector3.zero;

                for(int j = 0; j < ext_poss.Count; j++)
                {
                    if(j != current_point)    //No point in checking the angle between an extent point and itself
                    {
                        //Define second vector pointing from tile origin to a different extent point
                        Vector3 B           = (pos - ext_poss[j]).normalized;

                        //Cross returns a normal vector following the left hand rule, if that normal is the same as the Normal of the
                        //  planet, then the angle being measured is on the correct side.
                        //Also don't need to check the angle of the last extent being measured.
                        if((Vector3.Cross(B, A).normalized - Normal).magnitude < 0.0001f && i != ext_poss.Count - 1)
                        {
                            float f         = Vector3.Angle(A, B);

                            if(f < minAngle)
                            {
                                minAngle    = f;
                                C           = ext_poss[j];
                                current_point     = j;
                            }
                        }
                    }
                }

                if(C != Vector3.zero)   //if it didn't redefine C then there is either a mistake or this is the last extent point
                {
                    //Now C should be the nearest extent point in the CC direction so add it to _extents
                    _extents.Add(C);

                    //Debug.Log(i.ToString() + " -> " + decided.ToString() + 
                    //    " \t : Angle: \t " + minAngle + 
                    //    " \t : Cross: \t " + (Vector3.Cross(A, pos - C).normalized));
                }else if(i != ext_poss.Count - 1)
                {
                    _extents.Add(ext_poss[i]);
                }
            }
            return _extents;
        }

        /// <summary>
        /// Closer, but I think I need to add some sort of walking algorithm to get the corners closer
        /// </summary>
        /// <param name="Planet_Pos"></param>
        /// <returns></returns>
        public List<Vector3> Calculate_Corners(Vector3 Planet_Pos)
        {
            List<Vector3> corners   = new List<Vector3>();
            Vector3 Normal          = (_position - Planet_Pos).normalized;

            for (int i = 0; i < _extents.Count; i++)
            {
                float Angle = 0f;
                Vector3 to_extent = (_position - _extents[i]);
                if (i < _extents.Count - 1)
                {
                    Angle = Vector3.Angle(to_extent, _position - _extents[i + 1]) / 2f;
                }
                else
                {
                    Angle = Vector3.Angle(to_extent, _position - _extents[0]) / 2f;
                }

                Vector3 point = Vector3.zero;
                if(Angle > 0f)
                {
                    float dist = to_extent.magnitude * Mathf.Asin(Angle * (Mathf.PI / 180f));
                    Vector3 Perp = Vector3.Cross(Normal, to_extent).normalized;
                    point = _extents[i] + (Perp * dist);

                    corners.Add(point);
                    Debug.Log(i + " " + Angle);
                }
                else
                {
                    Debug.LogError("Failed to find Corner");
                }
            }

            return corners;
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