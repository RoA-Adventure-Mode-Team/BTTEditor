using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using System.Text.RegularExpressions;
using ImageMagick;
using RivalsAdventureEditor.DrawingObjects;
using System.Windows.Controls.Primitives;
using System.Collections.Specialized;
using RivalsAdventureEditor.Data;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Windows.Interop;
using RivalsAdventureEditor.Procedures;

namespace RivalsAdventureEditor.Panels
{
    /// <summary>
    /// Interaction logic for RoomEditor.xaml
    /// </summary>
    public partial class RoomEditor : Grid
    {
        public IntPtr WhiteBrush
        {
            get
            {
                if (_whiteBrush == IntPtr.Zero && renderer != IntPtr.Zero)
                    WindowAPI.CreateBrush(renderer, -1, out _whiteBrush);
                return _whiteBrush;
            }
        }
        private IntPtr _whiteBrush;
        public IntPtr GrayBrush
        {
            get
            {
                if (_grayBrush == IntPtr.Zero && renderer != IntPtr.Zero)
                    WindowAPI.CreateBrush(renderer, unchecked((int)0xFFAAAAAA), out _grayBrush);
                return _grayBrush;
            }
        }
        private IntPtr _grayBrush;
        public IntPtr OrangeBrush
        {
            get
            {
                if (_orangeBrush == IntPtr.Zero && renderer != IntPtr.Zero)
                    WindowAPI.CreateBrush(renderer, System.Drawing.Color.Orange.ToArgb(), out _orangeBrush);
                return _orangeBrush;
            }
        }
        private IntPtr _orangeBrush;

        public static RoomEditor Instance { get; set; }

        public Dictionary<string, TexData> LoadedImages { get; } = new Dictionary<string, TexData>();

        #region OverlayProperty
        public static readonly DependencyProperty OverlayProperty = DependencyProperty.Register(nameof(Overlay),
            typeof(TilemapOverlay),
            typeof(RoomEditor));

        public TilemapOverlay Overlay
        {
            get { return GetValue(OverlayProperty) as TilemapOverlay; }
            set { SetValue(OverlayProperty, value); }
        }
        #endregion

        #region SelectedObjProperty
        public static readonly DependencyProperty SelectedObjProperty = DependencyProperty.Register(nameof(SelectedObj),
            typeof(Article),
            typeof(RoomEditor),
            new FrameworkPropertyMetadata(SelectedObjPropertyChanged));

        private static void SelectedObjPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (ObjViewer.Instance != null)
                ObjViewer.Instance.Update();
            if (TilesetEditor.Instance != null && Instance.SelectedObj is Tilemap)
                TilesetEditor.Instance.Update();
            Instance.SelectedPath = -1;
        }

        public Article SelectedObj
        {
            get { return GetValue(SelectedObjProperty) as Article; }
            set { SetValue(SelectedObjProperty, value); }
        }
        #endregion

        public int SelectedPath { get; set; } = -1;

        private Point PrevPoint { get; set; }
        public Point RenderOffset { get; set; } = new Point(0, 0);
        public float zoomLevel = 0.5f;

        private readonly List<Article> articles = new List<Article>();

        public ProcedureBase ActiveProcedure { get; private set; }
        private DX_Article[] articles_internal = new DX_Article[16];
        int articles_count_internal = 0;
        private DX_Line[] lines_internal = new DX_Line[16];
        int lines_count_internal = 0;
        private DX_Tilemap[] tilemap_internal = new DX_Tilemap[16];
        int tilemap_count_internal = 0;

        HwndControl HwndControl;
        public IntPtr renderer = IntPtr.Zero;

        public RoomEditor()
        {
            InitializeComponent();
            if (Instance == null)
                Instance = this;

            HwndControl = new HwndControl(windowBorder.Width, windowBorder.Height);
            windowBorder.Child = HwndControl;
            HwndControl.MessageHook += new HwndSourceHook(ControlMsgFilter);
        }

        private IntPtr ControlMsgFilter(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;
            if (msg == WindowAPI.WM_COMMAND)
            {
                switch ((uint)wParam.ToInt32() >> 16 & 0xFFFF) //extract the HIWORD
                {
                    default:
                        break;
                }
            }
            return IntPtr.Zero;
        }

        ~RoomEditor()
        {
            WindowAPI.DestroyRenderer(renderer);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (HwndControl.Hwnd != IntPtr.Zero && renderer == IntPtr.Zero)
            {
                WindowAPI.InitRenderer(HwndControl.Hwnd, out renderer);
                CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            double dpiScale = 1.0; // default value for 96 dpi

            // determine DPI
            // (as of .NET 4.6.1, this returns the DPI of the primary monitor, if you have several different DPIs)
            var hwndTarget = PresentationSource.FromVisual(this).CompositionTarget as HwndTarget;
            if (hwndTarget != null)
            {
                dpiScale = hwndTarget.TransformToDevice.M11;
            }

            int surfWidth = (int)(ActualWidth < 0 ? 0 : Math.Ceiling(ActualWidth * dpiScale));
            int surfHeight = (int)(ActualHeight < 0 ? 0 : Math.Ceiling(ActualHeight * dpiScale));

            if(renderer != IntPtr.Zero)
                HRESULT.Check(WindowAPI.SetSize(renderer, surfWidth, surfHeight));
        }

        unsafe void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            RenderingEventArgs args = (RenderingEventArgs)e;

            // It's possible for Rendering to call back twice in the same frame
            // so only render when we haven't already rendered in this frame.
            if (_lastRender != args.RenderingTime)
            {
                if (!LoadedImages.ContainsKey("roaam_path"))
                {
                    LoadDefaults();
                }

                foreach (Article art in articles)
                {
                    string spriteName = art.Sprite;
                    // Check if the sprite is loaded
                    if (!LoadedImages.ContainsKey(spriteName))
                    {
                        // If spritename is set, attempt to load sprite
                        bool hasSprite = false;
                        if (!string.IsNullOrEmpty(spriteName))
                        {
                            hasSprite = WindowAPI.LoadImage(spriteName, renderer, out TexData data);
                            LoadedImages.Add(spriteName, data);
                        }

                        // If no sprite is found, use EmptyImage for this article
                        if (!hasSprite)
                        {
                            BitmapImage overrideSpr = FindResource("EmptyImage") as BitmapImage;
                            WindowAPI.RegisterTexture(renderer, spriteName, AppDomain.CurrentDomain.BaseDirectory + overrideSpr.UriSource.LocalPath, 1, out int texture);
                            LoadedImages[spriteName] = new TexData(false, texture, null);
                        }
                    }

                    // Get texture data for this article
                    TexData sprite = LoadedImages[spriteName];
                    Point offset = art.RealPoint - new Vector(sprite.offset.X, sprite.offset.Y);
                    Point scale = new Point(1, 1);
                    System.Drawing.Color color = System.Drawing.Color.White;

                    switch (art.ArticleNum)
                    {
                        case ArticleType.Terrain:
                            scale = new Point(2, 2);
                            break;
                        case ArticleType.Zone:
                            if (art is Zone zone)
                            {
                                scale = new Point(zone.TriggerWidth, zone.TriggerHeight);
                                // Set the color based on event type
                                switch (zone.EventID)
                                {
                                    //blastzone
                                    case 4:
                                        color = System.Drawing.Color.Red;
                                        break;
                                }
                                // Make semi-transparent
                                color = System.Drawing.Color.FromArgb(64, color.R, color.G, color.B);

                                // Show transform handles
                                if (SelectedObj == art )
                                {
                                    // Push a box at each corner of the zone for scale handles
                                    TexData spr = LoadedImages["roaam_square"];
                                    foreach (Point point in ROAAM_CONST.ZONE_POINTS)
                                    {
                                        PushArticle(new DX_Article(
                                            spr.texture,
                                            zone.RealPoint + new Vector(point.X * zone.TriggerWidth, point.Y * zone.TriggerHeight) - (Vector)spr.offset / zoomLevel,
                                            new Point(1 / zoomLevel, 1 / zoomLevel),
                                            -15));
                                    }
                                }
                            }
                            break;
                        case ArticleType.Target:
                            if (SelectedObj == art && (art as Target).Path.Any())
                            {
                                Vector prev = (Vector)(art as Target).Path.Last();
                                for (int i = 0; i < (art as Target).Path.Count; i++)
                                {
                                    Vector point = (Vector)(art as Target).Path[i];
                                    TexData targSpr = LoadedImages["roaam_path"];

                                    // Push path point sprite to renderer
                                    PushArticle(new DX_Article(
                                        targSpr.texture,
                                        (Point)((point * ROAAM_CONST.GRID_SIZE) - (Vector)targSpr.offset),
                                        new Point(1, 1),
                                        -10));
                                    // Push semi-transparent target sprite to renderer
                                    PushArticle(new DX_Article(
                                        sprite.texture,
                                        (Point)((point * ROAAM_CONST.GRID_SIZE) - (Vector)sprite.offset),
                                        scale,
                                        art.Depth,
                                        unchecked((int)0x40FFFFFF)));

                                    // Push transform handles if this path point is selected
                                    if (SelectedPath == i)
                                    {
                                        TexData arrowSpr = LoadedImages["roaam_arrows"];
                                        PushArticle(new DX_Article(
                                            arrowSpr.texture,
                                            (Point)((point * ROAAM_CONST.GRID_SIZE) - ((Vector)arrowSpr.offset / zoomLevel)),
                                            new Point(1 / zoomLevel, 1 / zoomLevel),
                                            -15));
                                    }

                                    // Push path line to renderer, and highlight orange if selected in editor panel
                                    IntPtr pathColor = WhiteBrush;
                                    float width = 2;
                                    if (i == ObjViewer.Instance.HighlightedPath)
                                    {
                                        pathColor = OrangeBrush;
                                        width = 4;
                                    }
                                    PushLine(new DX_Line((Point)(prev * ROAAM_CONST.GRID_SIZE),
                                        (Point)(point * ROAAM_CONST.GRID_SIZE),
                                        width,
                                        art.Depth - 0.1f,
                                        pathColor));
                                    // Store this point for line drawing
                                    prev = point;
                                }
                            }
                            break;
                        case ArticleType.Tilemap:
                            if (art is Tilemap tilemap)
                            {
                                Tileset tileset = tilemap.Tileset;
                                if (tileset == null || string.IsNullOrEmpty(tileset.SpritePath))
                                    continue;
                                // Check if the sprite is loaded
                                if (!LoadedImages.ContainsKey(tileset.SpritePath))
                                {
                                    // If spritename is set, attempt to load sprite
                                    bool hasSprite = false;
                                    if (!string.IsNullOrEmpty(tileset.SpritePath))
                                    {
                                        hasSprite = WindowAPI.LoadImage(tileset.SpritePath, renderer, out TexData data);
                                        LoadedImages.Add(tileset.SpritePath, data);
                                    }

                                    // If no sprite is found, use EmptyImage for this article
                                    if (!hasSprite)
                                    {
                                        BitmapImage overrideSpr = FindResource("EmptyImage") as BitmapImage;
                                        WindowAPI.RegisterTexture(renderer, tileset.SpritePath, AppDomain.CurrentDomain.BaseDirectory + overrideSpr.UriSource.LocalPath, 1, out int texture);
                                        LoadedImages[tileset.SpritePath] = new TexData(false, texture, null);
                                    }
                                }

                                TexData tex = LoadedImages[tileset.SpritePath];
                                foreach (var tilegrid in tilemap.Tilegrid)
                                {
                                    Point pos = new Point(tileset.TileWidth * 2 * TilegridArray.ChunkSizeX * tilegrid.Key.Item1 + tilemap.RealPoint.X,
                                        tileset.TileHeight * 2 * TilegridArray.ChunkSizeY * tilegrid.Key.Item2 + tilemap.RealPoint.Y);
                                    PushTilemap(new DX_Tilemap(tex.texture, pos, tileset.TileWidth, tileset.TileHeight, tilegrid.Value, tilemap.Depth, new Point(2, 2)));
                                }
                            }
                            break;
                    }

                    if (art.ArticleNum != ArticleType.Tilemap)
                    {
                        var c = color.ToArgb();
                        // Push the article sprite to the renderer
                        PushArticle(new DX_Article(
                            sprite.texture,
                            new Point(offset.X, offset.Y),
                            scale,
                            art.Depth,
                            SelectedObj == art ? System.Drawing.Color.Orange.ToArgb() : color.ToArgb()));
                    }

                    if (SelectedObj == art && SelectedPath == -1 && Overlay.Visibility != Visibility.Visible)
                    {
                        TexData arrowSpr = LoadedImages["roaam_arrows"];
                        if (art is Zone zone)
                            offset = new Point(offset.X + zone.TriggerWidth / 2, offset.Y + zone.TriggerHeight / 2);

                        // Push the transform handles to the renderer if selected
                        PushArticle(new DX_Article(
                            arrowSpr.texture,
                            offset - ((Vector)arrowSpr.offset / zoomLevel),
                            new Point(1 / zoomLevel, 1 / zoomLevel),
                            -15));
                    }
                }

                if (ApplicationSettings.Instance.ActiveProject != null)
                {
                    TexData respawnSpr = LoadedImages["roaam_respawn"];
                    PushArticle(new DX_Article(
                        respawnSpr.texture,
                        ApplicationSettings.Instance.ActiveProject.RespawnPoint - (Vector)respawnSpr.offset,
                        new Point(1, 1),
                        5));
                }

                WindowAPI.PrepareForRender(renderer);
                WindowAPI.SetCameraTransform(renderer, new Point(RenderOffset.X, RenderOffset.Y), zoomLevel);
                WindowAPI.Render(renderer, articles_internal, articles_count_internal, lines_internal, lines_count_internal, tilemap_internal, tilemap_count_internal);

                ClearArticles();
                ClearLines();
                ClearTilemaps();

                _lastRender = args.RenderingTime;
            }
        }

        TimeSpan _lastRender;

        // Import the methods exported by the unmanaged Direct3D content.
        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public POINT(Point p)
            {
                x = (int)p.X;
                y = (int)p.Y;
            }

            public int x;
            public int y;
        }

        public void Reload()
        {
            if(ApplicationSettings.Instance.ActiveRoom != null)
            {
                ApplicationSettings.Instance.ActiveRoom.Objs.CollectionChanged += UpdateArticles;
                
            }
            articles.Clear();
            if (ApplicationSettings.Instance.ActiveRoom != null)
            {
                foreach (Article art in ApplicationSettings.Instance.ActiveRoom.Objs)
                {
                    articles.Add(art);
                }
            }
        }

        private void UpdateArticles(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (Article article in e.NewItems)
                {
                    articles.Add(article);
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (Article article in e.OldItems)
                {
                    articles.Remove(article);
                }
            }
            if(e.Action == NotifyCollectionChangedAction.Reset)
            {
                articles.Clear();
            }
        }

        public void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                CaptureMouse();
            }
            if (ActiveProcedure != null)
                ActiveProcedure.Update();
            else if (e.ChangedButton == MouseButton.Left && SelectedObj != null)
            {
                var offset = LoadedImages["roaam_arrows"].offset;
                var cell_offset = new Vector(-SelectedObj.CellX * ROAAM_CONST.CELL_WIDTH, SelectedObj.CellY * ROAAM_CONST.CELL_HEIGHT);
                var center = (cell_offset + new Vector(SelectedObj.X * ROAAM_CONST.GRID_SIZE, SelectedObj.Y * ROAAM_CONST.GRID_SIZE) );
                if (SelectedObj is Zone zone)
                {
                    center += new Vector(zone.TriggerWidth / 2, zone.TriggerHeight / 2);
                }
                if (SelectedPath != -1)
                {
                    var targpoint = (SelectedObj as Target).Path[SelectedPath];
                    center = new Vector(targpoint.X * ROAAM_CONST.GRID_SIZE, targpoint.Y * ROAAM_CONST.GRID_SIZE);
                }
                Rect centerRect = new Rect(new Point(center.X - 7 / zoomLevel, center.Y - 7 / zoomLevel), new Size(15 / zoomLevel, 15 / zoomLevel));
                Rect upRect = new Rect(new Point(center.X - 8 / zoomLevel, center.Y - 56 / zoomLevel), new Size(19 / zoomLevel, 49 / zoomLevel));
                Rect rightRect = new Rect(new Point(center.X + 8 / zoomLevel, center.Y - 10 / zoomLevel), new Size(50 / zoomLevel, 20 / zoomLevel));
                var point = e.GetPosition(this);
                var transform = GetTransform();
                var absPoint = transform.Transform(point);
                int movingDir = -1;
                if (centerRect.Contains(absPoint))
                    movingDir = 0;
                else if (upRect.Contains(absPoint))
                    movingDir = 1;
                else if (rightRect.Contains(absPoint))
                    movingDir = 2;
                if(movingDir > -1)
                {
                    CaptureMouse();
                    ProcedureBase proc;
                    if (SelectedPath == -1)
                        proc = new TranslateProcedure(ApplicationSettings.Instance.ActiveProject, SelectedObj, movingDir);
                    else
                        proc = new PathTranslateProcedure(ApplicationSettings.Instance.ActiveProject, SelectedObj as Target, movingDir, SelectedPath);
                    SetActiveProcedure(proc);
                }
                else if(SelectedObj is Zone z)
                {
                    for(int i = 0; i < ROAAM_CONST.ZONE_POINTS.Length; i++)
                    {
                        var p = ROAAM_CONST.ZONE_POINTS[i];
                        Size s = new Size(36 / zoomLevel, 36 / zoomLevel);
                        Rect r = new Rect(new Point(z.RealPoint.X + p.X * z.TriggerWidth - s.Width / 2, z.RealPoint.Y + p.Y * z.TriggerHeight - s.Height / 2), s);
                        if(r.Contains(absPoint))
                        {
                            ProcedureBase proc = new ResizeProcedure(ApplicationSettings.Instance.ActiveProject, z, i);
                            SetActiveProcedure(proc);
                            break;
                        }
                    }
                }
            }
        }

        public void OnMouseMove(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(this);
            if (Mouse.Captured == this)
            {
                if (ActiveProcedure == null)
                {
                    RenderOffset += ((point - PrevPoint) * (1 / zoomLevel));
                }
            }
            if (ActiveProcedure != null)
                ActiveProcedure.Update();
            PrevPoint = point;
        }

        public void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Middle && Mouse.Captured == this)
            {
                Mouse.Capture(null);
            }
            if (e.ChangedButton == MouseButton.Left)
            {
                if(ActiveProcedure == null)
                {
                    var point = e.GetPosition(this);
                    TrySelectArticle(point);
                }
            }
            if (ActiveProcedure != null)
                ActiveProcedure.Update();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (ActiveProcedure != null)
                ActiveProcedure.Update();
        }

        public void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            float diff = (float)Math.Pow(1.2, (e.Delta / 100.0));
            zoomLevel *= diff;
            if (zoomLevel < 0.001f)
                zoomLevel = 0.001f;
        }

        public void SetActiveProcedure(ProcedureBase proc)
        {
            proc.Begin();
            proc.OnEnd += OnProcedureEnd;
            if (ActiveProcedure != null)
                ActiveProcedure.Cancel();
            ActiveProcedure = proc;
        }

        public void OnProcedureEnd()
        {
            Mouse.Capture(null);
            ActiveProcedure = null;
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            var bounds = LayoutInformation.GetLayoutSlot(this);
            if (bounds.Contains(RenderTransform.Transform(hitTestParameters.HitPoint)))
                return new PointHitTestResult(this, hitTestParameters.HitPoint);
            return null;
        }

        public void TrySelectArticle(Point point)
        {
            var transform = GetTransform();
            var absPoint = transform.Transform(point);

            Article top = null;
            int selectedPoint = -1;

            if (SelectedObj != null && SelectedObj is Target targ)
            {
                for(int i = 0; i < targ.Path.Count; i++)
                {
                    if (SelectedPath == i)
                        continue;
                    var pathPoint = targ.Path[i];
                    var x = pathPoint.X * ROAAM_CONST.GRID_SIZE - absPoint.X;
                    var y = pathPoint.Y * ROAAM_CONST.GRID_SIZE - absPoint.Y;
                    var sqrDist = x * x + y * y;
                    if(sqrDist <= 12 *12)
                    {
                        selectedPoint = i;
                        break;
                    }
                }
                if(selectedPoint != -1)
                {
                    top = SelectedObj;
                }
            }

            if (top == null)
            {
                selectedPoint = -1;
                foreach (var art in articles)
                {
                    if (art.Depth >= (top?.Depth ?? 100))
                        continue;
                    if (art.ContainsPoint(absPoint))
                        top = art;
                }
            }

            SelectedObj = top;
            SelectedPath = selectedPoint;
        }

        public void ResetImages()
        {
            LoadedImages.Clear();
            LoadDefaults();
        }

        public System.Drawing.Bitmap GetImage(string name)
        {
            if (LoadedImages.ContainsKey(name))
                return LoadedImages[name].image;
            else
                return null;
        }

        internal void LoadDefaults()
        {
            var spr = FindResource("PathPoint") as BitmapImage;
            WindowAPI.RegisterTexture(renderer, "roaam_path", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1, out int texture);
            LoadedImages["roaam_path"] = new TexData (true, texture, WindowAPI.BitmapImageToBitmap(spr), new Point (12, 12));

            spr = FindResource("EmptyImage") as BitmapImage;
            WindowAPI.RegisterTexture(renderer, "roaam_empty", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1, out texture);
            LoadedImages["roaam_empty"] = new TexData(true, texture, WindowAPI.BitmapImageToBitmap(spr), new Point(9, 9));

            spr = FindResource("WhitePixel") as BitmapImage;
            WindowAPI.RegisterTexture(renderer, "roaam_zone", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1, out texture);
            LoadedImages["roaam_zone"] = new TexData(true, texture, WindowAPI.BitmapImageToBitmap(spr));

            spr = FindResource("TargetSprite") as BitmapImage;
            WindowAPI.RegisterTexture(renderer, "roaam_target", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1, out texture);
            LoadedImages["roaam_target"] = new TexData(true, texture, WindowAPI.BitmapImageToBitmap(spr));

            spr = FindResource("Arrows") as BitmapImage;
            WindowAPI.RegisterTexture(renderer, "roaam_arrows", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1, out texture);
            LoadedImages["roaam_arrows"] = new TexData(true, texture, WindowAPI.BitmapImageToBitmap(spr), new Point(56, 56));

            spr = FindResource("Square") as BitmapImage;
            WindowAPI.RegisterTexture(renderer, "roaam_square", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1, out texture);
            LoadedImages["roaam_square"] = new TexData(true, texture, WindowAPI.BitmapImageToBitmap(spr), new Point(18, 18));

            spr = FindResource("Respawn") as BitmapImage;
            WindowAPI.RegisterTexture(renderer, "roaam_respawn", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1, out texture);
            LoadedImages["roaam_respawn"] = new TexData(true, texture, WindowAPI.BitmapImageToBitmap(spr), new Point(16, 32));
        }

        public Transform GetTransform()
        {
            return new TransformGroup
            {
                Children =
                    {
                        new ScaleTransform(1 / zoomLevel, 1 / zoomLevel),
                        new TranslateTransform(-RenderOffset.X, -RenderOffset.Y)
                    }
            };
        }

        public void PushArticle(DX_Article article)
        {
            if (articles_count_internal + 1 >= articles_internal.Length)
            {
                DX_Article[] new_array = new DX_Article[articles_internal.Length * 2];
                articles_internal.CopyTo(new_array, 0);
                articles_internal = new_array;
            }

            articles_internal[articles_count_internal++] = article;
        }

        public void PushLine(DX_Line line)
        {
            if (lines_count_internal + 1 >= lines_internal.Length)
            {
                DX_Line[] new_array = new DX_Line[lines_internal.Length * 2];
                lines_internal.CopyTo(new_array, 0);
                lines_internal = new_array;
            }

            lines_internal[lines_count_internal++] = line;
        }

        public void PushTilemap(DX_Tilemap tilemap)
        {
            if (tilemap_count_internal + 1 >= tilemap_internal.Length)
            {
                DX_Tilemap[] new_array = new DX_Tilemap[tilemap_internal.Length * 2];
                tilemap_internal.CopyTo(new_array, 0);
                tilemap_internal = new_array;
            }

            tilemap_internal[tilemap_count_internal++] = tilemap;
        }

        private void ClearArticles()
        {
            articles_count_internal = 0;
        }
        private void ClearLines()
        {
            lines_count_internal = 0;
        }
        private void ClearTilemaps()
        {
            tilemap_count_internal = 0;
        }
    }

    public static class HRESULT
    {
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public static void Check(int hr)
        {
            Marshal.ThrowExceptionForHR(hr);
        }
    }

    public struct DX_Article
    {
        public DX_Article(int texture, Point translate, Point scale, float depth, int color = unchecked((int)0xFFFFFFFF))
        {
            Depth = depth;
            Type = DX_Type.ARTICLE;
            Texture = texture;
            Translate = translate;
            Scale = scale;
            Color = color;
            CropEnd = new Point(1, 1);
        }

        public float Depth;
        public DX_Type Type;
        public int Texture;
        public Point Translate;
        public Point Scale;
        public int Color;
        public Point CropStart;
        public Point CropEnd;
    }

    public struct DX_Line
    {
        public DX_Line(Point start, Point end, float width, float depth, IntPtr color)
        {
            Depth = depth;
            Type = DX_Type.LINE;
            Start = start;
            End = end;
            Width = width;
            Color = color;
        }

        public float Depth;
        public DX_Type Type;
        public Point Start;
        public Point End;
        public float Width;
        public IntPtr Color;
    }

    public struct DX_Tilemap
    {
        public unsafe DX_Tilemap(int texture, Point translate, int tile_width, int tile_height, IntPtr map, float depth, Point scale)
        {
            Depth = depth;
            Type = DX_Type.TILEMAP;
            Texture = texture;
            Translate = translate;
            TileWidth = tile_width;
            TileHeight = tile_height;
            Map = map;
            Scale = scale;
        }

        public float Depth;
        public DX_Type Type;
        public int Texture;
        public int TileWidth;
        public int TileHeight;
        public Point Translate;
        public IntPtr Map;
        public Point Scale;
    }

    public enum DX_Type
    {
        ARTICLE = 0,
        LINE,
        TILEMAP,
    }
}
