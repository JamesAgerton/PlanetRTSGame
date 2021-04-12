using UnityEngine;
using System.Collections.Generic;
using System;

namespace Planets
{
    public class Tile : IEquatable<Tile>, IComparable<Tile>
    {
        #region Variables
        private int             _index;
        private Vector3         _position;
        private List<Tile>      _neighbors;
        #endregion

        #region Properties
        public int              Index       => _index;
        public Vector3          Position    => _position;
        public List<Tile>       Neighbors   => _neighbors;
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