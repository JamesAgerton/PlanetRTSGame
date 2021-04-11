using System.Collections.Generic;
using UnityEngine;
using GK;

namespace Planets
{
    public class Planet
    {
        #region Variables (PRIVATE)
        private FibonacciSphere _FS;
        
        private List<Vector3> _positions;
        private int[] _raw_triangles;

        private List<Vector3> _vertices;
        private List<int> _triangles;
        private Mesh _mesh;

        private float _radius;
        private int _num_tiles;
        #endregion

        #region Properties (PUBLIC)
        public List<Tile> Tiles = new List<Tile>();
        public List<Vector3> POSITIONS => _positions;
        public int[] RAW_TRIANGLES => _raw_triangles;
        public Mesh MESH => _mesh;
        #endregion

        public Planet(int num_tiles, float radius)
        {
            _FS = new FibonacciSphere();
            Tiles = new List<Tile>();
            if(radius > 0)
            {
                _radius = radius;
            }
            else
            {
                _radius = 1f;
            }
            if(num_tiles > 50)
            {
                _num_tiles = num_tiles;
            }
            else
            {
                _num_tiles = 50;
            }
        }

        public void Generate_Planet()
        {
            SpherePoints SP = _FS.Generate_Whole_Sphere(_num_tiles - 1, _radius);

            _positions = SP.Positions;
            _raw_triangles = SP.Triangles;

            //Populate Planet_Tiles
            for(int i = 0; i < _positions.Count; i++)
            {
                Tiles.Add(new Tile(i, _positions[i]));
            }
            UnityEngine.Debug.Log("Positions: " + _positions.Count);

            //Create a list of neighbor tiles to add to each Tile on the planet
            Generate_Tile_Neighbor_Lists(_raw_triangles);
        }

        /// <summary>
        /// Create a list of Tile objects that are neighbors to any tile.
        /// </summary>
        /// <param name="triangles"></param>
        /// <returns></returns>
        public int Generate_Tile_Neighbor_Lists(int[] triangles)
        {
            foreach (Tile tile in Tiles)
            {
                List<Tile> New_Neighbors = new List<Tile>();
                for (int i = 0; i < triangles.Length; i++)
                {
                    //triangles is a list of indexes, tile indexes should be the same as their position index.
                    if (triangles[i] == tile.Index)
                    {
                        switch (i % 3)
                        {
                            //Add the triangle to the New_Neighbors list
                            case (0):
                                if (!New_Neighbors.Contains(Tiles[triangles[i]])) { New_Neighbors.Add(Tiles[triangles[i]]); }
                                if (!New_Neighbors.Contains(Tiles[triangles[i + 1]])) { New_Neighbors.Add(Tiles[triangles[i + 1]]); }
                                if (!New_Neighbors.Contains(Tiles[triangles[i + 2]])) { New_Neighbors.Add(Tiles[triangles[i + 2]]); }
                                break;
                            case (1):
                                if (!New_Neighbors.Contains(Tiles[triangles[i - 1]])) { New_Neighbors.Add(Tiles[triangles[i - 1]]); }
                                if (!New_Neighbors.Contains(Tiles[triangles[i]])) { New_Neighbors.Add(Tiles[triangles[i]]); }
                                if (!New_Neighbors.Contains(Tiles[triangles[i + 1]])) { New_Neighbors.Add(Tiles[triangles[i + 1]]); }
                                break;
                            case (2):
                                if (!New_Neighbors.Contains(Tiles[triangles[i - 2]])) { New_Neighbors.Add(Tiles[triangles[i - 2]]); }
                                if (!New_Neighbors.Contains(Tiles[triangles[i - 1]])) { New_Neighbors.Add(Tiles[triangles[i - 1]]); }
                                if (!New_Neighbors.Contains(Tiles[triangles[i]])) { New_Neighbors.Add(Tiles[triangles[i]]); }
                                break;
                            default:
                                UnityEngine.Debug.LogError("wierd stuff happened while looking through triangles array.");
                                break;
                        }
                    }
                }

                //Check to make sure the tile isn't its own neighbor
                for (int i = 0; i < New_Neighbors.Count; i++)
                {
                    if (New_Neighbors[i] == tile)
                    {
                        New_Neighbors.RemoveAt(i);
                    }
                }
                New_Neighbors.Sort();
                tile.Set_Neighbors(New_Neighbors);
            }

            return 0;
        }

        public void Generate_Tile_Mesh(int index)
        {
            //Get a point halfway between this tile and its neighbors
            //Get a point halfway between each of those points
            //Draw Triangles to each of those points
        }
    }
}

