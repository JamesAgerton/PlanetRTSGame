using System.Collections.Generic;
using UnityEngine;

namespace Planets
{
    public class Planet
    {
        #region Variables (PRIVATE)
        private FibonacciSphere _FS;
        
        private List<Vector3>   _tile_positions;
        private int[]           _default_triangles;

        private List<Vector3>   _tile_vertices;
        private List<int>       _tile_triangles;
        private Mesh            _tiled_mesh;

        private float           _radius;
        private int             _num_tiles;
        #endregion

        #region Properties (PUBLIC)
        public List<Tile>       Tiles               = new List<Tile>();
        public List<Vector3>    Tile_Positions      => _tile_positions;
        public int[]            Default_Triangles   => _default_triangles;
        public List<Vector3>    Tile_Verts          => _tile_vertices;
        public List<int>        Tile_Triangles      => _tile_triangles;
        public Mesh             Tiled_Mesh          => _tiled_mesh;
        #endregion

        public Planet(int num_tiles, float radius)
        {
            _FS = new FibonacciSphere();
            Tiles = new List<Tile>();
            _tile_positions = new List<Vector3>();
            _tile_vertices = new List<Vector3>();
            _tile_triangles = new List<int>();

            if(radius >= 1f)
            {
                _radius     = radius;
            }
            else
            {
                _radius     = 1f;
            }

            if(num_tiles >= 50)
            {
                _num_tiles  = num_tiles;
            }
            else
            {
                _num_tiles  = 50;
            }
        }

        public void Generate_Planet(float Tile_Seperation)
        {
            SpherePoints    SP     = _FS.Generate_Delaunay_Sphere(_num_tiles - 1, _radius);

            _tile_positions         = SP.Positions;
            _default_triangles      = SP.Triangles;

            //Populate Planet_Tiles
            for(int i = 0; i < _tile_positions.Count; i++)
            {
                Tiles.Add(new Tile(i, _tile_positions[i]));
            }
            //UnityEngine.Debug.Log("Positions: " + _tile_positions.Count);

            //Create a list of neighbor tiles to add to each Tile on the planet
            Generate_Tile_Neighbor_Lists(_default_triangles);
            //Give each tile their Extents
            float fraction = 0.5f - (Tile_Seperation / 2f);
            foreach(Tile tile in Tiles)
            {
                tile.Generate_Tile_Triangles_and_Vertices(ref _tile_triangles, ref _tile_vertices, fraction);
            }

            _tiled_mesh = new Mesh();
            _tiled_mesh.name = "Tiled Mesh";
            _tiled_mesh.vertices = _tile_vertices.ToArray();
            _tiled_mesh.triangles = _tile_triangles.ToArray();
            _tiled_mesh.RecalculateNormals();
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
                List<Tile>  New_Neighbors   = new List<Tile>();
                for (int i = 0; i < triangles.Length; i++)
                {
                    //triangles is a list of indexes, tile indexes should be the same as their position index.
                    if (triangles[i] == tile.Index)
                    {
                        switch (i % 3)
                        {
                            //Add the triangle to the New_Neighbors list
                            case (0):
                                if (!New_Neighbors.Contains(Tiles[triangles[i]]))       { New_Neighbors.Add(Tiles[triangles[i]]); }
                                if (!New_Neighbors.Contains(Tiles[triangles[i + 1]]))   { New_Neighbors.Add(Tiles[triangles[i + 1]]); }
                                if (!New_Neighbors.Contains(Tiles[triangles[i + 2]]))   { New_Neighbors.Add(Tiles[triangles[i + 2]]); }
                                break;
                            case (1):
                                if (!New_Neighbors.Contains(Tiles[triangles[i - 1]]))   { New_Neighbors.Add(Tiles[triangles[i - 1]]); }
                                if (!New_Neighbors.Contains(Tiles[triangles[i]]))       { New_Neighbors.Add(Tiles[triangles[i]]); }
                                if (!New_Neighbors.Contains(Tiles[triangles[i + 1]]))   { New_Neighbors.Add(Tiles[triangles[i + 1]]); }
                                break;
                            case (2):
                                if (!New_Neighbors.Contains(Tiles[triangles[i - 2]]))   { New_Neighbors.Add(Tiles[triangles[i - 2]]); }
                                if (!New_Neighbors.Contains(Tiles[triangles[i - 1]]))   { New_Neighbors.Add(Tiles[triangles[i - 1]]); }
                                if (!New_Neighbors.Contains(Tiles[triangles[i]]))       { New_Neighbors.Add(Tiles[triangles[i]]); }
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
                tile.Set_Neighbors(New_Neighbors);
                tile.Sort_Neighbors();
            }

            return 0;
        }

        /// <summary>
        /// Finds the tile which a point is within using latitude and longitude.
        /// returns -1 on no tile.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public int Point_To_Tile(Vector3 point)
        {
            if(Tiles.Count > 0)
            {
                for (int i = 0; i < Tiles.Count; i++)
                {
                    int pole = 0;
                    if (i == 0)
                    {
                        pole = 1;
                    }
                    else if (i == Tiles.Count - 1)
                    {
                        pole = -1;
                    }

                    if (Tiles[i].Is_Point_on_Tile(point, pole))
                    {
                        return i;
                    }
                }
            }
            Debug.Log("Couldn't find tile from point!");
            return -1;
        }

        /// <summary>
        /// Use the mesh triangles to identify tile. Used with raycast Hit.triangleIndex.
        /// </summary>
        /// <param name="triIndex"></param>
        /// <returns></returns>
        public int Point_To_Tile_Triangle(int triIndex)
        {
            if (Tiles.Count > 0)
            {
                for (int i = 0; i < Tiles.Count; i++)
                {
                    if (Tiles[i].Is_Point_on_Tile_Triangle(triIndex))
                    {
                        return i;
                    }
                }
            }
            Debug.Log("Couldn't find tile from point!");
            return -1;
        }
    }
}

