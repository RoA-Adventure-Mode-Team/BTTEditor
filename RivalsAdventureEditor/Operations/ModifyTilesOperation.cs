using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Panels;
using System;
using System.Collections.Generic;
using System.Text;

namespace RivalsAdventureEditor.Operations
{
    class ModifyTilesOperation : OperationBase
    {
        public Tilemap Map { get; set; }
        public List<Tuple<int, int>> ModifiedTiles { get; set; }
        public List<int> OldTiles { get; set; }
        public List<int> NewTiles { get; set; }
        public override string Parameter => $"{Map.ArticleNum}({Map.Name}), Ties:  {string.Join(", ", ModifiedTiles)}";

        public ModifyTilesOperation(Project proj, Tilemap map, List<Tuple<int, int>> tiles, List<int> values) : base(proj)
        {
            Map = map;
            ModifiedTiles = tiles;
            NewTiles = values;
            OldTiles = new List<int>();
            foreach(var tile in tiles)
            {
                OldTiles.Add(map.Tilegrid.GetTileAt(tile));
            }
        }

        public override void Execute()
        {
            for(int i = 0; i < ModifiedTiles.Count; i++)
            {
                Map.Tilegrid.SetTileAt(ModifiedTiles[i], NewTiles[i]);
            }
            RoomEditor.Instance.SelectedObj = Map;
        }

        public override void Undo()
        {
            for (int i = 0; i < ModifiedTiles.Count; i++)
            {
                Map.Tilegrid.SetTileAt(ModifiedTiles[i], OldTiles[i]);
            }
            RoomEditor.Instance.SelectedObj = Map;
        }
    }
}
