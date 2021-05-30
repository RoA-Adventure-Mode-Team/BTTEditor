using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Operations;
using RivalsAdventureEditor.Panels;

namespace RivalsAdventureEditor.Procedures
{
    public class TilemapRectangleProcedure : TilemapProcedureBase
    {
        public bool Finished { get; set; }

        public Point Start { get; set; }

        public static TilemapProcedureBase Create(Project project, Tilemap obj, int paintTile, bool leftMouse)
        {
            return new TilemapRectangleProcedure(project, obj, paintTile, leftMouse);
        }

        public TilemapRectangleProcedure(Project project, Tilemap obj, int paintTile, bool leftMouse) : base(project, obj, paintTile, leftMouse)
        {
            Obj = obj;
            PaintTile = paintTile;
        }

        public override void Begin()
        {
            var transform = RoomEditor.Instance.GetTransform();

            Start = transform.Transform(Mouse.GetPosition(RoomEditor.Instance));

            CompositionTarget.Rendering += OnRender;
        }

        public override void Update()
        {
            if (LeftMouse && Mouse.LeftButton == MouseButtonState.Released)
            {
                End();
            }
            else if (!LeftMouse && Mouse.RightButton == MouseButtonState.Released)
            {
                End();
            }
        }

        private void OnRender(object sender, EventArgs e)
        {
            double xIncrement = Obj.Tileset.TileWidth * 2;
            double yIncrement = Obj.Tileset.TileHeight * 2;
            double tilemapXOffset = TilemapOverlay.Repeat(Obj.RealPoint.X, xIncrement);
            double tilemapYOffset = TilemapOverlay.Repeat(Obj.RealPoint.Y, yIncrement);

            var transform = RoomEditor.Instance.GetTransform();
            Point hoveredPoint = transform.Transform(Mouse.GetPosition(RoomEditor.Instance));
            hoveredPoint = new Point(Math.Floor((hoveredPoint.X - tilemapXOffset) / xIncrement) * xIncrement + tilemapXOffset, Math.Floor((hoveredPoint.Y - tilemapYOffset) / yIncrement) * yIncrement + tilemapYOffset);
            Point startPoint = new Point(Math.Floor((Start.X - tilemapXOffset) / xIncrement) * xIncrement + tilemapXOffset, Math.Floor((Start.Y - tilemapYOffset) / yIncrement) * yIncrement + tilemapYOffset);

            Point upperLeft = new Point(Math.Min(startPoint.X, hoveredPoint.X), Math.Min(startPoint.Y, hoveredPoint.Y));
            Point lowerRight = new Point(Math.Max(startPoint.X, hoveredPoint.X) + Obj.Tileset.TileWidth * 2, Math.Max(startPoint.Y, hoveredPoint.Y) + Obj.Tileset.TileHeight * 2);

            var scale = new Point(lowerRight.X - upperLeft.X, lowerRight.Y - upperLeft.Y);
            var spr = RoomEditor.Instance.LoadedImages["roaam_zone"];
            RoomEditor.Instance.PushArticle(new DX_Article(spr.texture, upperLeft, scale, Obj.Depth - 0.1f, unchecked((int)0xAAFFFFFF)));
        }

        public override void Cancel()
        {
            CompositionTarget.Rendering -= OnRender;
        }

        public override void End()
        {
            if (Finished)
                return;
            Finished = true;

            var transform = RoomEditor.Instance.GetTransform();
            Point hoveredPoint = transform.Transform(Mouse.GetPosition(RoomEditor.Instance));
            Point upperLeft = new Point(Math.Min(Start.X, hoveredPoint.X), Math.Min(Start.Y, hoveredPoint.Y));
            Point lowerRight = new Point(Math.Max(Start.X, hoveredPoint.X), Math.Max(Start.Y, hoveredPoint.Y));

            Tuple<int, int> startIndex = Obj.PointToIndex(upperLeft);
            Tuple<int, int> endIndex = Obj.PointToIndex(lowerRight);

            Dictionary<Tuple<int, int>, int> changedTiles = new Dictionary<Tuple<int, int>, int>();

            for(int y = startIndex.Item2; y <= endIndex.Item2; y++)
            {
                for(int x = startIndex.Item1; x <= endIndex.Item1; x++)
                {
                    Tuple<int, int> index = Tuple.Create(x, y);
                    changedTiles.Add(index, Obj.Tilegrid.GetTileAt(index));
                }
            }

            var op = new SetTilesUniformOperation(Project, Obj, PaintTile, changedTiles);
            Project.ExecuteOp(op);

            base.End();
            CompositionTarget.Rendering -= OnRender;
        }
    }
}
