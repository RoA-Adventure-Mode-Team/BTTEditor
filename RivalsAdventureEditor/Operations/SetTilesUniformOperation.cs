using System;
using System.Collections.Generic;
using System.Text;
using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Panels;

namespace RivalsAdventureEditor.Operations
{
    public class SetTilesUniformOperation : OperationBase
    {
        public Tilemap Obj { get; set; }
        public int PaintTile { get; set; }

        public Dictionary<Tuple<int, int>, int> ChangedTiles { get; set; } = new Dictionary<Tuple<int, int>, int>();

        public override string Parameter => $"{Obj.ArticleNum}({Obj.Name}), Set {ChangedTiles.Count} Tiles to {PaintTile}";

        public SetTilesUniformOperation(Project project, Tilemap obj, int paintTile, Dictionary<Tuple<int, int>, int> changedTiles) : base(project)
        {
            Obj = obj;
            PaintTile = paintTile;
            ChangedTiles = changedTiles;
        }

        public override void Execute()
        {
            foreach(var pair in ChangedTiles)
            {
                Obj.Tilegrid.SetTileAt(pair.Key, PaintTile);
            }
            RoomEditor.Instance.SelectedObj = Obj;
        }

        public override void Undo()
        {
            foreach (var pair in ChangedTiles)
            {
                Obj.Tilegrid.SetTileAt(pair.Key, pair.Value);
            }
            RoomEditor.Instance.SelectedObj = Obj;
        }
    }
}
