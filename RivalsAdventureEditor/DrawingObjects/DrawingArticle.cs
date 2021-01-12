using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Panels;

namespace RivalsAdventureEditor.DrawingObjects
{
    public class DrawingArticle : DrawingVisual
    {
        public Article Article { get; set; }
        public BitmapImage Sprite { get; set; }
        public Vector SpriteOffset { get; set; }
        public Point Translate { get; set; }
        public double Scale { get; set; } = 1;

        public void UpdateDrawing()
        {
            var spriteName = Article.Args[0] as string;
            bool hasSprite = false;
            if (spriteName != null)
                hasSprite = RoomEditor.LoadImage(spriteName);
            //if(!hasSprite)
            //    Sprite = RoomEditor.Instance.DefaultImage;
            Offset = new Vector(Article.X, Article.Y);
            DrawingContext ctx = RenderOpen();
            var cell_offset = new Vector(Article.CellX * ROAAM_CONST.CELL_WIDTH, Article.CellY * ROAAM_CONST.CELL_HEIGHT);
            var offset = Scale * (cell_offset + (Offset * ROAAM_CONST.GRID_SIZE) - SpriteOffset);
            var size = new Size(Sprite.Width * Scale, Sprite.Height * Scale);
            // Scale up terrain
            if (Article.ArticleNum == ArticleType.Terrain)
                size = new Size(size.Width * 2, size.Height * 2);
            //Rect area = new Rect(new Point(offset.X + Translate.X, offset.Y + Translate.Y), size);
            //ctx.DrawRectangle(Brushes.White, new Pen(Brushes.Black, 1), area);
            //ctx.DrawImage(Sprite, area);
            //ctx.DrawRectangle(Brushes.Black, new Pen(Brushes.Black, 1), new Rect(area.TopLeft, new Size(4, 4)));
            //ctx.DrawRectangle(Brushes.Black, new Pen(Brushes.Black, 1), new Rect(area.TopRight, new Size(4, 4)));
            //ctx.DrawRectangle(Brushes.Black, new Pen(Brushes.Black, 1), new Rect(area.BottomLeft, new Size(4, 4)));
            //ctx.DrawRectangle(Brushes.Black, new Pen(Brushes.Black, 1), new Rect(area.BottomRight, new Size(4, 4)));
            //// ctx.DrawText(new FormattedText($"({offset.X + Translate.X}, {offset.Y + Translate.Y})", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Arial"), 25, Brushes.Black, 1), area.BottomLeft);
            //ctx.Close();
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            var cell_offset = new Vector(Article.CellX * ROAAM_CONST.CELL_WIDTH, Article.CellY * ROAAM_CONST.CELL_HEIGHT);
            var offset = cell_offset + (Offset * ROAAM_CONST.GRID_SIZE - SpriteOffset);
            var area = new Rect(new System.Windows.Point(offset.X, offset.Y), new System.Windows.Size(Sprite.Width, Sprite.Height));
            var hit = area.Contains(hitTestParameters.HitPoint);
            if (hit)
                return new PointHitTestResult(this, hitTestParameters.HitPoint);
            else
                return null;
        }
    }
}
