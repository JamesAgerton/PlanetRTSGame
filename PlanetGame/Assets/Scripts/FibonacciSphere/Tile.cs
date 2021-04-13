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
        public void Add_Neighbor(Tile neighbor)
        {
            _neighbors.Add(neighbor);
        }
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
        public void Set_Neighbors(List<Tile> neighbors)
        {
            _neighbors  = neighbors;
        }

        public void Set_Index(int i)
        {
            _index      = i;
        }
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
            _extents = new List<Vector3>();

            //Grab extent points
            for (int i = 0; i < _neighbors.Count; i++)
            {
                Vector3 ext_pos = pos + Vector3.ProjectOnPlane(Vector3.Lerp(pos, _neighbors[i].Position, Ext_Frac), Normal);
                
                ext_poss.Add(ext_pos);
            }

            //Re order extents to be in counterclockwise order
            _extents.Add(ext_poss[0]);
            int decided = 0;
            for(int i = 0; i < ext_poss.Count; i++)
            {
                Vector3 A = (pos - ext_poss[decided]).normalized;
                Vector3 C = Vector3.zero;
                float minAngle = float.PositiveInfinity;
                for(int j = 0; j < ext_poss.Count; j++)
                {
                    if(j != decided)
                    {
                        Vector3 B = (pos - ext_poss[j]).normalized;

                        if((Vector3.Cross(B, A).normalized - Normal).magnitude < 0.0001f && i != ext_poss.Count - 1)
                        {
                            //On the correct side and not the final extent position
                            float f = Vector3.Angle(A, B);
                            if(f < minAngle)
                            {
                                minAngle = f;
                                C = ext_poss[j];
                                decided = j;
                            }
                        }
                    }
                }
                //Should have nearest neighbor in the CC direction
                if(C != Vector3.zero)
                {
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

        public bool Equals(Tile other)
        {
            if (other == null) return false;
            return (this.Index == other.Index);
        }

        public int CompareTo(Tile other)
        {
            if (other == null) return 1;
            return _index.CompareTo(other.Index);
        }
        #endregion
    }
}