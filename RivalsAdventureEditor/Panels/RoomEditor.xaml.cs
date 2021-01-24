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
        public static RoomEditor Instance { get; set; }

        public Dictionary<string, TexData> LoadedImages { get; } = new Dictionary<string, TexData>();
        public static readonly DependencyProperty SelectedObjProperty = DependencyProperty.Register(nameof(SelectedObj),
            typeof(Article),
            typeof(RoomEditor),
            new FrameworkPropertyMetadata(SelectedObjPropertyChanged));

        private static void SelectedObjPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (ObjViewer.Instance != null)
                ObjViewer.Instance.Update();
        }

        public Article SelectedObj
        {
            get { return GetValue(SelectedObjProperty) as Article; }
            set { SetValue(SelectedObjProperty, value); }
        }
        public int SelectedPath { get; set; } = -1;

        private Point PrevPoint { get; set; }
        private Point RenderOffset { get; set; } = new Point(0, 0);
        private float zoomLevel = 0.5f;

        private readonly List<Article> articles = new List<Article>();

        public ProcedureBase ActiveProcedure { get; private set; }
        private DX_Article[] articles_internal = new DX_Article[16];
        int articles_count_internal = 0;
        private DX_Line[] lines_internal = new DX_Line[16];
        int lines_count_internal = 0;
        private DX_Tilemap[] tilemap_internal = new DX_Tilemap[16];
        int tilemap_count_internal = 0;

        HwndControl HwndControl;

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
            if (msg == WM_COMMAND)
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
            Destroy();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (HwndControl.Hwnd != IntPtr.Zero)
            {
                Init(HwndControl.Hwnd);
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

            HRESULT.Check(SetSize(surfWidth, surfHeight));
        }

        unsafe void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            RenderingEventArgs args = (RenderingEventArgs)e;

            // It's possible for Rendering to call back twice in the same frame
            // so only render when we haven't already rendered in this frame.
            if (_lastRender != args.RenderingTime)
            {
                if (!Instance.LoadedImages.ContainsKey("roaam_path"))
                {
                    LoadDefaults();
                }

                foreach (Article art in articles)
                {
                    string spriteName = art.Sprite;
                    // Check if the sprite is loaded
                    if (!Instance.LoadedImages.ContainsKey(spriteName))
                    {
                        // If spritename is set, attempt to load sprite
                        bool hasSprite = false;
                        if (!string.IsNullOrEmpty(spriteName))
                            hasSprite = LoadImage(spriteName);

                        // If no sprite is found, use EmptyImage for this article
                        if (!hasSprite)
                        {
                            BitmapImage overrideSpr = FindResource("EmptyImage") as BitmapImage;
                            RegisterTexture(spriteName, AppDomain.CurrentDomain.BaseDirectory + overrideSpr.UriSource.LocalPath, 1, out int texture);
                            Instance.LoadedImages[spriteName] = new TexData(false, texture, null);
                        }
                    }

                    // Get texture data for this article
                    TexData sprite = Instance.LoadedImages[spriteName];
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
                                if (SelectedObj == art)
                                {
                                    // Push a box at each corner of the zone for scale handles
                                    TexData spr = Instance.LoadedImages["roaam_square"];
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
                                    TexData targSpr = Instance.LoadedImages["roaam_path"];

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
                                        TexData arrowSpr = Instance.LoadedImages["roaam_arrows"];
                                        PushArticle(new DX_Article(
                                            arrowSpr.texture,
                                            (Point)((point * ROAAM_CONST.GRID_SIZE) - ((Vector)arrowSpr.offset / zoomLevel)),
                                            new Point(1 / zoomLevel, 1 / zoomLevel),
                                            -15));
                                    }

                                    // Push path line to renderer, and highlight orange if selected in editor panel
                                    int pathColor = unchecked((int)0xFFFFFFFF);
                                    float width = 2;
                                    if (i == ObjViewer.Instance.HighlightedPath)
                                    {
                                        pathColor = System.Drawing.Color.Orange.ToArgb();
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
                                if (tileset == null)
                                    continue;
                                // Check if the sprite is loaded
                                if (!Instance.LoadedImages.ContainsKey(tileset.SpritePath))
                                {
                                    // If spritename is set, attempt to load sprite
                                    bool hasSprite = false;
                                    if (!string.IsNullOrEmpty(tileset.SpritePath))
                                        hasSprite = LoadImage(tileset.SpritePath);

                                    // If no sprite is found, use EmptyImage for this article
                                    if (!hasSprite)
                                    {
                                        BitmapImage overrideSpr = FindResource("EmptyImage") as BitmapImage;
                                        RegisterTexture(tileset.SpritePath, AppDomain.CurrentDomain.BaseDirectory + overrideSpr.UriSource.LocalPath, 1, out int texture);
                                        Instance.LoadedImages[tileset.SpritePath] = new TexData(false, texture, null);
                                    }
                                }

                                TexData tex = LoadedImages[tileset.SpritePath];
                                foreach (var tilegrid in tilemap.Tilegrid)
                                {
                                    Point pos = new Point(tileset.TileWidth * TilegridArray.ChunkSizeX * tilegrid.Key.Item1 + tilemap.RealPoint.X,
                                        tileset.TileHeight * TilegridArray.ChunkSizeY * tilegrid.Key.Item2 + tilemap.RealPoint.Y);
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

                    if (SelectedObj == art && SelectedPath == -1)
                    {
                        TexData arrowSpr = Instance.LoadedImages["roaam_arrows"];
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
                    TexData respawnSpr = Instance.LoadedImages["roaam_respawn"];
                    PushArticle(new DX_Article(
                        respawnSpr.texture,
                        ApplicationSettings.Instance.ActiveProject.RespawnPoint - (Vector)respawnSpr.offset,
                        new Point(1, 1),
                        5));
                }

                SetCameraTransform(new Point(RenderOffset.X, RenderOffset.Y), zoomLevel);
                Render(articles_internal, articles_count_internal, lines_internal, lines_count_internal, tilemap_internal, tilemap_count_internal);

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

        [DllImport("D3DContent.dll")]
        static extern int Init(IntPtr hwnd);

        [DllImport("D3DContent.dll")]
        static extern int SetSize(int width, int height);

        [DllImport("D3DContent.dll")]
        static extern int RegisterTexture([MarshalAs(UnmanagedType.LPStr)]string key, [MarshalAs(UnmanagedType.LPWStr)]string fname, int frames, out int texture);

        [DllImport("D3DContent.dll")]
        static extern int SetCameraTransform(Point pos, float zoom);

        [DllImport("D3DContent.dll")]
        static extern int Render(DX_Article[] articles, int count, DX_Line[] lines, int line_count, DX_Tilemap[] tilemaps, int tilemap_count);

        [DllImport("D3DContent.dll")]
        static extern void Destroy();

        internal const int
          LBN_SELCHANGE = 0x00000001,
          WM_COMMAND = 0x00000111,
          LB_GETCURSEL = 0x00000188,
          LB_GETTEXTLEN = 0x0000018A,
          LB_ADDSTRING = 0x00000180,
          LB_GETTEXT = 0x00000189,
          LB_DELETESTRING = 0x00000182,
          LB_GETCOUNT = 0x0000018B;

        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Unicode)]
        internal static extern int SendMessage(IntPtr hwnd,
                                               int msg,
                                               IntPtr wParam,
                                               IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Unicode)]
        internal static extern int SendMessage(IntPtr hwnd,
                                               int msg,
                                               int wParam,
                                               [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lParam);

        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Unicode)]
        internal static extern IntPtr SendMessage(IntPtr hwnd,
                                                  int msg,
                                                  IntPtr wParam,
                                                  String lParam);


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

        public static bool LoadImage(string name)
        {
            if (Instance.LoadedImages.ContainsKey(name))
                return Instance.LoadedImages[name].exists;
            Point offset = new Point();
            string loadFile = Path.Combine(Path.GetDirectoryName(ApplicationSettings.Instance.ActiveProject.ProjectPath), "scripts", "load.gml");
            MatchCollection matches = null;
            if(File.Exists(loadFile))
            {
                string lines = File.ReadAllText(loadFile);
                matches = Regex.Matches(lines, "sprite_change_offset\\s*\\(\\s*\"([\\w\\d]+)\",\\s*(\\d+),\\s*(\\d+)\\s*\\)");
            }
            string directory;
            if(ApplicationSettings.Instance.ActiveProject.Type == ProjectType.AdventureMode)
                directory = Path.Combine(Path.GetDirectoryName(ApplicationSettings.Instance.ActiveProject.ProjectPath), "sprites", "articles");
            else
                directory = Path.Combine(Path.GetDirectoryName(ApplicationSettings.Instance.ActiveProject.ProjectPath), "sprites");
            if (File.Exists(Path.Combine(directory, name + ".png")))
            {
                string path = Path.Combine(directory, name + ".png");
                System.Drawing.Bitmap img = null;
                using (FileStream file = new FileStream(path, FileMode.Open))
                {
                    MemoryStream stream = new MemoryStream();
                    file.CopyTo(stream);
                    img = new System.Drawing.Bitmap(stream);
                }
                RegisterTexture(name, path, 1, out int texture);
                if (matches != null)
                {
                    Match match = matches.OfType<Match>().FirstOrDefault(m => m.Groups[1].Value == name);
                    if (match != null)
                    {
                        offset.X = Double.Parse(match.Groups[2].Value);
                        offset.Y = Double.Parse(match.Groups[3].Value);
                    }
                }
                Instance.LoadedImages[name] = new TexData(true, texture, img, offset);
                return true;
            }

            var files = Directory.EnumerateFiles(directory, name + "*.png");
            if (files.Any())
            {
                string file = files.FirstOrDefault(f => Regex.Match(f, name + "_strip(\\d+)").Success);
                if (!string.IsNullOrEmpty(file))
                {
                    Match match = Regex.Match(file, "strip(\\d+)");
                    int count = int.Parse(match.Groups[1].Value);
                    System.Drawing.Bitmap img = null;
                    using (FileStream fstream = new FileStream(file, FileMode.Open))
                    {
                        MemoryStream stream = new MemoryStream();
                        fstream.CopyTo(stream);
                        img = new System.Drawing.Bitmap(stream);
                    }
                    RegisterTexture(name, file, count, out int texture);
                    int index = file.IndexOf("_strip");
                    if (matches != null)
                    {
                        Match offsetMatch = matches.OfType<Match>().FirstOrDefault(m => m.Groups[1].Value == file.Substring(0, index));
                        if (offsetMatch != null)
                        {
                            offset.X = Double.Parse(offsetMatch.Groups[2].Value);
                            offset.Y = Double.Parse(offsetMatch.Groups[3].Value);
                        }
                    }
                    Instance.LoadedImages[name] = new TexData(true, texture, img, offset);
                    return true;
                }
            }
            Instance.LoadedImages[name] = new TexData(false, 0);
            return false;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                CaptureMouse();
            }
            if (ActiveProcedure != null)
                ActiveProcedure.Update();
            else if (e.ChangedButton == MouseButton.Left && SelectedObj != null)
            {
                var offset = Instance.LoadedImages["roaam_arrows"].offset;
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

        private void OnMouseMove(object sender, MouseEventArgs e)
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

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
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

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
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

            if(SelectedObj != null && SelectedObj is Target targ)
            {
                int selectedPoint = -1;
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
                    SelectedPath = selectedPoint;
                }
            }

            if (top == null)
            {
                SelectedPath = -1;
                foreach (var art in articles)
                {
                    var spr = LoadedImages[art.Sprite];
                    if (!spr.exists)
                        spr = LoadedImages["roaam_empty"];
                    var cell_offset = new Vector(-art.CellX * ROAAM_CONST.CELL_WIDTH, art.CellY * ROAAM_CONST.CELL_HEIGHT);
                    var offset = (cell_offset + new Vector(art.X * ROAAM_CONST.GRID_SIZE, art.Y * ROAAM_CONST.GRID_SIZE) - new Vector(spr.offset.X, spr.offset.Y));
                    Point scale = new Point (1, 1);
                    switch (art.ArticleNum)
                    {
                        case ArticleType.Terrain:
                            scale = new Point (2, 2);
                            break;
                        case ArticleType.Zone:
                            if (art is Zone zone)
                            {
                                scale = new Point (zone.TriggerWidth, zone.TriggerHeight);
                            }
                            break;
                    }
                    var box = new Rect(new Point(offset.X, offset.Y), new Size((spr.image?.Width ?? 0) * scale.X, (spr.image?.Height ?? 0) * scale.Y));
                    if (box.Contains(absPoint) && art.Depth < (top?.Depth ?? 100))
                    {
                        if (spr.image == null)
                            top = art;
                        else
                        {
                            var sprPoint = absPoint - offset;
                            sprPoint = new Point((sprPoint.X * 1 / scale.X), (sprPoint.Y * 1 / scale.Y));
                            var px = spr.image.GetPixel((int)sprPoint.X, (int)sprPoint.Y);
                            if (px.A > 0)
                            {
                                top = art;
                            }
                        }
                    }
                }
            }

            SelectedObj = top;
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
            RegisterTexture("roaam_path", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1, out int texture);
            Instance.LoadedImages["roaam_path"] = new TexData (true, texture, BitmapImageToBitmap(spr), new Point (12, 12));

            spr = FindResource("EmptyImage") as BitmapImage;
            RegisterTexture("roaam_empty", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1, out texture);
            Instance.LoadedImages["roaam_empty"] = new TexData(true, texture, BitmapImageToBitmap(spr), new Point(9, 9));

            spr = FindResource("WhitePixel") as BitmapImage;
            RegisterTexture("roaam_zone", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1, out texture);
            Instance.LoadedImages["roaam_zone"] = new TexData(true, texture, BitmapImageToBitmap(spr));

            spr = FindResource("TargetSprite") as BitmapImage;
            RegisterTexture("roaam_target", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1, out texture);
            Instance.LoadedImages["roaam_target"] = new TexData(true, texture, BitmapImageToBitmap(spr));

            spr = FindResource("Arrows") as BitmapImage;
            RegisterTexture("roaam_arrows", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1, out texture);
            Instance.LoadedImages["roaam_arrows"] = new TexData(true, texture, BitmapImageToBitmap(spr), new Point(56, 56));

            spr = FindResource("Square") as BitmapImage;
            RegisterTexture("roaam_square", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1, out texture);
            Instance.LoadedImages["roaam_square"] = new TexData(true, texture, BitmapImageToBitmap(spr), new Point(18, 18));

            spr = FindResource("Respawn") as BitmapImage;
            RegisterTexture("roaam_respawn", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1, out texture);
            Instance.LoadedImages["roaam_respawn"] = new TexData(true, texture, BitmapImageToBitmap(spr), new Point(16, 32));
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

        private System.Drawing.Bitmap BitmapImageToBitmap(BitmapImage bitmapImage)
        {
            // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new System.Drawing.Bitmap(bitmap);
            }
        }

        private void PushArticle(DX_Article article)
        {
            if (articles_count_internal + 1 >= articles_internal.Length)
            {
                DX_Article[] new_array = new DX_Article[articles_internal.Length * 2];
                articles_internal.CopyTo(new_array, 0);
                articles_internal = new_array;
            }

            articles_internal[articles_count_internal++] = article;
        }

        private void PushLine(DX_Line line)
        {
            if (lines_count_internal + 1 >= lines_internal.Length)
            {
                DX_Line[] new_array = new DX_Line[lines_internal.Length * 2];
                lines_internal.CopyTo(new_array, 0);
                lines_internal = new_array;
            }

            lines_internal[lines_count_internal++] = line;
        }

        private void PushTilemap(DX_Tilemap tilemap)
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
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public static void Check(int hr)
        {
            //Marshal.ThrowExceptionForHR(hr);
        }
    }

    public struct TexData
    {
        public TexData(bool _exists, int _texture, System.Drawing.Bitmap _image = null)
        {
            exists = _exists;
            texture = _texture;
            image = _image;
        }

        public TexData(bool _exists, int _texture, System.Drawing.Bitmap _image, Point _offset)
        {
            exists = _exists;
            texture = _texture;
            offset = _offset;
            image = _image;
        }

        public bool exists;
        public int texture;
        public Point offset;
        public System.Drawing.Bitmap image;
    }

    struct DX_Article
    {
        public DX_Article(int texture, Point translate, Point scale, float depth, int color = unchecked((int)0xFFFFFFFF))
        {
            Depth = depth;
            Type = DX_Type.ARTICLE;
            Texture = texture;
            Translate = translate;
            Scale = scale;
            Color = color;
        }

        public float Depth;
        public DX_Type Type;
        public int Texture;
        public Point Translate;
        public Point Scale;
        public int Color;
    }

    struct DX_Line
    {
        public DX_Line(Point start, Point end, float width, float depth, int color = unchecked((int)0xFFFFFFFF))
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
        public int Color;
    }

    struct DX_Tilemap
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

    enum DX_Type
    {
        ARTICLE = 0,
        LINE,
        TILEMAP,
    }
}
