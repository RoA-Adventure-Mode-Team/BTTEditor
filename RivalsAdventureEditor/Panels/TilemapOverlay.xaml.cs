using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Procedures;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RivalsAdventureEditor.Panels
{
    /// <summary>
    /// Interaction logic for TilemapOverlay.xaml
    /// </summary>
    public partial class TilemapOverlay : Grid
    {
        public static TilemapTool PaintTool { get; } = new TilemapTool(TilemapPaintProcedure.Create, true);
        public static TilemapTool RectangleTool { get; } = new TilemapTool(TilemapRectangleProcedure.Create, false);

        private TilemapTool CurrentTilemapTool { get; set; } = PaintTool;

        public TilemapOverlay()
        {
            InitializeComponent();
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            if (!IsVisible)
                return;
            if(RoomEditor.Instance.SelectedObj is Tilemap tilemap)
            {
                double x, y, xStart, yStart;
                x = xStart = -RoomEditor.Instance.RenderOffset.X;
                y = yStart = -RoomEditor.Instance.RenderOffset.Y;
                double actualWidth = RoomEditor.Instance.ActualWidth / RoomEditor.Instance.zoomLevel;
                double actualHeight = RoomEditor.Instance.ActualHeight / RoomEditor.Instance.zoomLevel;
                double xIncrement = tilemap.Tileset.TileWidth * 2;
                double yIncrement = tilemap.Tileset.TileHeight * 2;
                double tilemapXOffset = Repeat(tilemap.RealPoint.X, xIncrement);
                double tilemapYOffset = Repeat(tilemap.RealPoint.Y, yIncrement);
                double xOffset = tilemapXOffset - Repeat(xStart, xIncrement);
                double yOffset = tilemapYOffset - Repeat(yStart, xIncrement);

                // Don't draw grid if we're too zoomed out
                if (tilemap.Tileset.TileWidth * RoomEditor.Instance.zoomLevel > 2)
                {
                    while ((x - xStart) + xOffset <= actualWidth)
                    {
                        RoomEditor.Instance.PushLine(new DX_Line(
                            new Point(x + xOffset, yStart),
                            new Point(x + xOffset, yStart + actualHeight),
                            1.5f / RoomEditor.Instance.zoomLevel,
                            tilemap.Depth,
                            RoomEditor.Instance.GrayBrush));
                        x += xIncrement;
                    }
                    while ((y - yStart) + yOffset <= actualHeight)
                    {
                        RoomEditor.Instance.PushLine(new DX_Line(
                            new Point(xStart, y + yOffset),
                            new Point(xStart + actualWidth, y + yOffset),
                            1.5f / RoomEditor.Instance.zoomLevel,
                            tilemap.Depth,
                            RoomEditor.Instance.GrayBrush));
                        y += yIncrement;
                    }
                }

                if (string.IsNullOrEmpty(tilemap.Tileset.SpritePath) || !RoomEditor.Instance.LoadedImages.ContainsKey(tilemap.Tileset.SpritePath))
                    return;
                TexData spr = RoomEditor.Instance.LoadedImages[tilemap.Tileset.SpritePath];
                int tile = TilesetEditor.Instance.SelectedTile;
                int tileSpan = spr.image.Width / tilemap.Tileset.TileWidth;
                int tileHeight = spr.image.Height / tilemap.Tileset.TileHeight;
                int tileX = tile % tileSpan;
                int tileY = tile / tileSpan;

                var transform = RoomEditor.Instance.GetTransform();
                Point hoveredPoint = transform.Transform(Mouse.GetPosition(RoomEditor.Instance));
                hoveredPoint = new Point(Math.Floor((hoveredPoint.X - tilemapXOffset) / xIncrement) * xIncrement + tilemapXOffset, Math.Floor((hoveredPoint.Y - tilemapYOffset) / yIncrement) * yIncrement + tilemapYOffset);
                hoveredPoint -= new Vector(tileX * tilemap.Tileset.TileWidth * 2, tileY * tilemap.Tileset.TileHeight * 2);
                RoomEditor.Instance.PushArticle(new DX_Article(
                    spr.texture,
                    hoveredPoint,
                    new Point(2, 2),
                    tilemap.Depth - 0.1f,
                    unchecked((int)0xAAFFFFFF))
                {
                    CropStart = new Point(tileX / (float)tileSpan, tileY / (float)tileHeight),
                    CropEnd = new Point((tileX + 1) / (float)tileSpan, (tileY + 1) / (float)tileHeight)
                });
            }
            else
            {
                Visibility = Visibility.Collapsed;
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                RoomEditor.Instance.OnMouseDown(e.Source, e);
            }
            else
            {
                e.Handled = true;
                if (e.ChangedButton == MouseButton.Left)
                {
                    if (RoomEditor.Instance.SelectedObj is Tilemap tilemap)
                    {
                        var proc = CurrentTilemapTool.CreateFunction(ApplicationSettings.Instance.ActiveProject, tilemap, TilesetEditor.Instance.SelectedTile + 1, true);
                        RoomEditor.Instance.SetActiveProcedure(proc);
                    }
                }
                else if(e.ChangedButton == MouseButton.Right)
                {
                    if (RoomEditor.Instance.SelectedObj is Tilemap tilemap)
                    {
                        var proc = CurrentTilemapTool.CreateFunction(ApplicationSettings.Instance.ActiveProject, tilemap, 0, false);
                        RoomEditor.Instance.SetActiveProcedure(proc);
                    }
                }
                if (RoomEditor.Instance.ActiveProcedure != null)
                    RoomEditor.Instance.ActiveProcedure.Update();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            RoomEditor.Instance.OnMouseMove(e.Source, e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (RoomEditor.Instance.ActiveProcedure != null)
                RoomEditor.Instance.ActiveProcedure.Update();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            RoomEditor.Instance.OnMouseWheel(e.Source, e);
        }
        public static double Repeat(double t, double length)
        {
            return t - Math.Floor(t / length) * length;
        }

        public void SetActiveTool(TilemapTool tool)
        {
            CurrentTilemapTool.Active = false;
            tool.Active = true;
            CurrentTilemapTool = tool;
        }
    }

    public class TilemapTool : DependencyObject
    {
        public TilemapProcedureBase.CreateTmapProcFunction CreateFunction { get; set; }

        public static DependencyProperty ActiveProperty = DependencyProperty.Register(nameof(Active),
            typeof(bool),
            typeof(TilemapTool));
        public bool Active
        {
            get { return (bool)GetValue(ActiveProperty); }
            set { SetValue(ActiveProperty, value); }
        }

        public TilemapTool(TilemapProcedureBase.CreateTmapProcFunction func, bool active)
        {
            CreateFunction = func;
            Active = active;
        }
    }
}
