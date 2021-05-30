using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Panels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RivalsAdventureEditor.DrawingObjects
{
    /// <summary>
    /// Interaction logic for TilesetViewer.xaml
    /// </summary>
    public partial class TilesetViewer : Border
    {
        private IntPtr WhiteBrush
        {
            get
            {
                if (_whiteBrush == IntPtr.Zero && renderer != IntPtr.Zero)
                    WindowAPI.CreateBrush(renderer, -1, out _whiteBrush);
                return _whiteBrush;
            }
        }
        private IntPtr _whiteBrush;

        HwndControl HwndControl;
        IntPtr renderer = IntPtr.Zero;

        private Tileset CurrentTileset => TilesetEditor.Instance.SelectedTileset;
        private Dictionary<string, TexData> LoadedImages { get; } = new Dictionary<string, TexData>();

        public TilesetViewer()
        {
            InitializeComponent();
            HwndControl = new HwndControl(Width, Height);
            Child = HwndControl;
        }

        ~TilesetViewer()
        {
            WindowAPI.DestroyRenderer(renderer);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (HwndControl.Hwnd != IntPtr.Zero && renderer == IntPtr.Zero)
            {
                WindowAPI.InitRenderer(HwndControl.Hwnd, out renderer);
                CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);

                var spr = FindResource("WhitePixel") as BitmapImage;
                WindowAPI.RegisterTexture(renderer, "roaam_zone", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1, out var texture);
                LoadedImages["roaam_zone"] = new TexData(true, texture, WindowAPI.BitmapImageToBitmap(spr));
            }
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            RenderingEventArgs args = (RenderingEventArgs)e;
            if (_lastRender != args.RenderingTime)
            {
                SetSize();
                if (CurrentTileset != null && !string.IsNullOrEmpty(CurrentTileset.SpritePath))
                {
                    TexData spr = LoadImage(CurrentTileset.SpritePath);

                    DX_Article[] articles = new DX_Article[3];
                    int articles_count = 0;

                    int tile_span = spr.image.Width / CurrentTileset.TileWidth;
                    if (spr.image.Width % CurrentTileset.TileWidth != 0)
                        tile_span++;
                    int tile_depth = spr.image.Height / CurrentTileset.TileHeight;
                    if (spr.image.Height % CurrentTileset.TileHeight != 0)
                        tile_depth++;
                    int lines_count = tile_span + tile_depth;
                    DX_Line[] lines = new DX_Line[lines_count];

                    for (int i = 0; i < tile_span; i++)
                    {
                        lines[i] = new DX_Line(
                            new Point(i * CurrentTileset.TileWidth, 0),
                            new Point(i * CurrentTileset.TileWidth, spr.image.Height),
                            1,
                            -1,
                            WhiteBrush);
                    }

                    for (int i = 0; i < tile_depth; i++)
                    {
                        lines[tile_span + i] = new DX_Line(
                            new Point(0, i * CurrentTileset.TileHeight),
                            new Point(spr.image.Width, i * CurrentTileset.TileHeight),
                            1,
                            -1,
                            WhiteBrush);
                    }

                    articles[articles_count++] = new DX_Article(spr.texture, new Point(0, 0), new Point(1, 1), 0);

                    var selected_x = (TilesetEditor.Instance.SelectedTile) % tile_span;
                    var selected_y = (TilesetEditor.Instance.SelectedTile) / tile_span;
                    articles[articles_count++] = new DX_Article(LoadedImages["roaam_zone"].texture,
                        new Point(selected_x * CurrentTileset.TileWidth, selected_y * CurrentTileset.TileHeight),
                        new Point(CurrentTileset.TileWidth, CurrentTileset.TileHeight),
                        -1,
                        unchecked((int)0xAAFF8000));

                    var mousePos = Mouse.GetPosition(this);
                    if(new Rect(0, 0, ActualWidth, ActualHeight).Contains(mousePos))
                    {
                        int highlight_x = (int)((mousePos.X / (HwndControl.ActualWidth / spr.image.Width)) / CurrentTileset.TileWidth);
                        int highlight_y = (int)((mousePos.Y / (HwndControl.ActualHeight / spr.image.Height)) / CurrentTileset.TileHeight);
                        articles[articles_count++] = new DX_Article(LoadedImages["roaam_zone"].texture,
                            new Point(highlight_x * CurrentTileset.TileWidth, highlight_y * CurrentTileset.TileHeight),
                            new Point(CurrentTileset.TileWidth, CurrentTileset.TileHeight),
                            -2,
                            unchecked((int)0xAAFFFFFF));
                    }

                    WindowAPI.PrepareForRender(renderer);
                    WindowAPI.SetCameraTransform(renderer, new Point(0, 0), (float)(HwndControl.ActualWidth / spr.image.Width));
                    WindowAPI.Render(renderer, articles, articles_count, lines, lines_count, null, 0);

                    if (Mouse.LeftButton == MouseButtonState.Pressed)
                    {
                        var pos = GetMousePos();
                        if (pos != null)
                        {
                            TilesetEditor.Instance.SelectedTile = pos.Item1 + pos.Item2 * tile_span;
                        }
                    }
                }

                _lastRender = args.RenderingTime;
            }
        }
        TimeSpan _lastRender;

        public Tuple<int, int> GetMousePos()
        {
            TexData spr = LoadImage(CurrentTileset.SpritePath);
            var mousePos = Mouse.GetPosition(this);
            if (new Rect(0, 0, ActualWidth, ActualHeight).Contains(mousePos))
            {
                int highlight_x = (int)((mousePos.X / (HwndControl.ActualWidth / spr.image.Width)) / CurrentTileset.TileWidth);
                int highlight_y = (int)((mousePos.Y / (HwndControl.ActualHeight / spr.image.Height)) / CurrentTileset.TileHeight);
                return Tuple.Create(highlight_x, highlight_y);
            }
            return null;
        }

        public TexData LoadImage(string name)
        {
            // Check if the sprite is loaded
            if (!LoadedImages.ContainsKey(name))
            {
                // If spritename is set, attempt to load sprite
                bool hasSprite = false;
                hasSprite = WindowAPI.LoadImage(name, renderer, out TexData data);
                LoadedImages.Add(name, data);

                // If no sprite is found, use EmptyImage for this article
                if (!hasSprite)
                {
                    BitmapImage overrideSpr = FindResource("EmptyImage") as BitmapImage;
                    WindowAPI.RegisterTexture(renderer, name, AppDomain.CurrentDomain.BaseDirectory + overrideSpr.UriSource.LocalPath, 1, out int texture);
                    LoadedImages[name] = new TexData(false, texture, null);
                }
            }
            return LoadedImages[name];
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            SetSize();
        }

        private void SetSize()
        {
            if(CurrentTileset != null && !string.IsNullOrEmpty(CurrentTileset.SpritePath))
            {
                if(LoadedImages.ContainsKey(CurrentTileset.SpritePath))
                {
                    TexData spr = LoadedImages[CurrentTileset.SpritePath];
                    double ratio = (double)spr.image.Height / spr.image.Width;
                    double adjustedHeight = ActualHeight / ratio;
                    double maxDimension = Math.Min(ActualWidth, adjustedHeight);
                    HwndControl.Width = maxDimension;
                    HwndControl.Height = maxDimension * ratio;
                    HwndControl.VerticalAlignment = VerticalAlignment.Top;

                    if (renderer != IntPtr.Zero)
                        HRESULT.Check(WindowAPI.SetSize(renderer, (int)maxDimension, (int)(maxDimension * ratio)));
                }
            }
        }
    }
}
