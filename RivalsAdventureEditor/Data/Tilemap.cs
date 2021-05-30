using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using ImageMagick;
using Newtonsoft.Json;
using RivalsAdventureEditor.Panels;

namespace RivalsAdventureEditor.Data
{
    public class Tilemap : Article
    {
        public string TilesetID { get; set; } = "";
        [JsonIgnore]
        public Tileset Tileset
        {
            get
            {
                if(!string.IsNullOrEmpty(TilesetID))
                {
                    return (ApplicationSettings.Instance.ActiveProject?.Tilesets.ContainsKey(TilesetID) ?? false) ? ApplicationSettings.Instance.ActiveProject.Tilesets[TilesetID] : null;
                }
                return null;
            }
        }
        public TilegridArray Tilegrid { get; private set; } = new TilegridArray();

        public override bool ContainsPoint(Point point)
        {
            return Tilegrid.GetTileAt(PointToIndex(point)) != 0;
        }

        public Tuple<int, int> PointToIndex(Point point)
        {
            Point localPoint = (Point)(point - RealPoint);
            Point scaledPoint = new Point(localPoint.X / (Tileset.TileWidth * 2), localPoint.Y / (Tileset.TileHeight * 2));
            return new Tuple<int, int>((int)Math.Floor(scaledPoint.X), (int)Math.Floor(scaledPoint.Y));
        }

        public Point IndexToPoint(int x, int y)
        {
            Point localPoint = new Point(x * Tileset.TileWidth * 2, y * Tileset.TileHeight * 2);
            return localPoint + (Vector)RealPoint;
        }

        public Tuple<int, int> IndexToCell(int x, int y)
        {
            var point = IndexToPoint(x, y);
            var scaledPoint = new Point(point.X / ROAAM_CONST.CELL_WIDTH, point.Y / ROAAM_CONST.CELL_HEIGHT);
            return new Tuple<int, int>((int)Math.Floor(scaledPoint.X), (int)Math.Floor(scaledPoint.Y));
        }

        public Tuple<int, int> MaxIndexInCell(int x, int y)
        {
            var point = new Point(x * ROAAM_CONST.CELL_WIDTH + ROAAM_CONST.CELL_WIDTH - 1, y * ROAAM_CONST.CELL_HEIGHT + ROAAM_CONST.CELL_HEIGHT - 1);
            return PointToIndex(point);
        }

        public IEnumerable<Tuple<Terrain, Tuple<int, int>>> GetTilemapAsTerrain(int tmap_num)
        {
            string directory;
            if (ApplicationSettings.Instance.ActiveProject.Type == ProjectType.AdventureMode)
                directory = Path.Combine(Path.GetDirectoryName(ApplicationSettings.Instance.ActiveProject.ProjectPath), "sprites", "articles");
            else
                directory = Path.Combine(Path.GetDirectoryName(ApplicationSettings.Instance.ActiveProject.ProjectPath), "sprites");

            MagickImage tileset = new MagickImage(Path.Combine(directory, Tileset.SpritePath + ".png"));
            List<IMagickImage<ushort>> tiles = tileset.CropToTiles(Tileset.TileWidth, Tileset.TileHeight).ToList();

            // Define the bounds of the tilemap in this cell
            int tl_x = Tilegrid.MinX * TilegridArray.ChunkSizeX, tl_y = Tilegrid.MinY * TilegridArray.ChunkSizeY, br_x = tl_x, br_y = tl_y;
            int max_x = Tilegrid.MaxX * TilegridArray.ChunkSizeX + TilegridArray.ChunkSizeX;
            int max_y = Tilegrid.MaxY * TilegridArray.ChunkSizeY + TilegridArray.ChunkSizeY;
            var min_cell = IndexToCell(tl_x, tl_y);
            var max_cell = IndexToCell(max_x, max_y);

            for(int y = min_cell.Item2; y <= max_cell.Item2; y++)
            {
                for(int x = min_cell.Item1; x <= max_cell.Item1; x++)
                {
                    var maxIndex = MaxIndexInCell(x, y);
                    br_x = maxIndex.Item1 < max_x ? maxIndex.Item1 : max_x;
                    br_y = maxIndex.Item2 < max_y ? maxIndex.Item2 : max_y;

                    if (BuildTerrain(tl_x, tl_y, br_x, br_y, x, y, tmap_num, directory, tiles, out Terrain terrain))
                    {
                        yield return Tuple.Create(terrain, Tuple.Create(x, y));
                    }

                    tl_x = br_x + 1;
                }

                // Shift the top of our cell 1 tile down
                tl_y = br_y + 1;
                // Reset x pos
                tl_x = Tilegrid.MinX * TilegridArray.ChunkSizeX;
            }
        }

        private bool BuildTerrain(int tl_x, int tl_y, int br_x, int br_y, int cell_x, int cell_y, int tmap_num, string directory, List<IMagickImage<ushort>> tiles, out Terrain terrain)
        {
            int width = ((br_x + 1) - tl_x) * Tileset.TileWidth;
            int height = ((br_y + 1) - tl_y) * Tileset.TileHeight;
            MagickImage output = new MagickImage(new MagickColor(0, 0, 0, 0), width, height);
            output.Format = MagickFormat.Png;

            bool export = false;
            for (int y = tl_y; y <= br_y; y++)
            {
                for(int x = tl_x; x <= br_x; x++)
                {
                    int tile = Tilegrid.GetTileAt(Tuple.Create(x, y));
                    if (tile == 0)
                        continue;
                    export = true;
                    output.Composite(tiles[tile - 1], (x - tl_x) * Tileset.TileWidth, (y - tl_y) * Tileset.TileHeight, CompositeOperator.Over);
                }
            }

            if(!export)
            {
                terrain = null;
                return false;
            }    

            string sprite_name = $"tmap_{tmap_num}_{cell_x}-{cell_y}";
            string sprite_path = Path.Combine(directory, sprite_name + ".png");
            output.Write(sprite_path, MagickFormat.Png);

            Point tl_point = IndexToPoint(tl_x, tl_y);
            double offset_x = tl_point.X - (cell_x * ROAAM_CONST.CELL_WIDTH);
            double offset_y = tl_point.Y - (cell_y * ROAAM_CONST.CELL_HEIGHT);

            terrain = new Terrain
            {
                CellX = cell_x,
                CellY = cell_y,
                X = (int)(4 * offset_x / ROAAM_CONST.GRID_SIZE) / 4.0f,
                Y = (int)(4 * offset_y / ROAAM_CONST.GRID_SIZE) / 4.0f,
                Depth = Depth,
                Sprite = sprite_name,
                
            };
            terrain.ArticleNum = ArticleType.Terrain;

            return true;
        }
    }
}
