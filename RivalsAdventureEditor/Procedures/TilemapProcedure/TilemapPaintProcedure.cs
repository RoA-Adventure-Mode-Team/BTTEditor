using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;
using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Operations;
using RivalsAdventureEditor.Panels;

namespace RivalsAdventureEditor.Procedures
{
    public class TilemapPaintProcedure : TilemapProcedureBase
    {
        public Dictionary<Tuple<int, int>, int> ChangedTiles { get; set; } = new Dictionary<Tuple<int, int>, int>();
        public bool Finished { get; set; }

        public static TilemapProcedureBase Create(Project project, Tilemap obj, int paintTile, bool leftMouse)
        {
            return new TilemapPaintProcedure(project, obj, paintTile, leftMouse);
        }

        public TilemapPaintProcedure(Project project, Tilemap obj, int paintTile, bool leftMouse) : base(project, obj, paintTile, leftMouse)
        {
            Obj = obj;
            PaintTile = paintTile;
        }

        public override void Begin()
        {

        }

        public override void Update()
        {
            if (LeftMouse && Mouse.LeftButton == MouseButtonState.Released)
            {
                End();
            }
            else if(!LeftMouse && Mouse.RightButton == MouseButtonState.Released)
            {
                End();
            }
            else
            {
                var transform = RoomEditor.Instance.GetTransform();
                Point hoveredPoint = transform.Transform(Mouse.GetPosition(RoomEditor.Instance));
                Tuple<int, int> index = Obj.PointToIndex(hoveredPoint);
                if (!ChangedTiles.ContainsKey(index))
                    ChangedTiles.Add(index, Obj.Tilegrid.GetTileAt(index));
                Obj.Tilegrid.SetTileAt(index, PaintTile);
            }
        }

        public override void Cancel()
        {
            foreach (var pair in ChangedTiles)
            {
                Obj.Tilegrid.SetTileAt(pair.Key, pair.Value);
            }
        }

        public override void End()
        {
            if (Finished)
                return;
            Finished = true;
            var op = new SetTilesUniformOperation(Project, Obj, PaintTile, ChangedTiles);
            Project.ExecuteOp(op);
            base.End();
        }
    }
}
