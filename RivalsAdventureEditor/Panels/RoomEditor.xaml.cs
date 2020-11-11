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
            typeof(Obj),
            typeof(RoomEditor),
            new FrameworkPropertyMetadata(SelectedObjPropertyChanged));

        private static void SelectedObjPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (ObjViewer.Instance != null)
                ObjViewer.Instance.Update();
        }

        public Obj SelectedObj
        {
            get { return GetValue(SelectedObjProperty) as Obj; }
            set { SetValue(SelectedObjProperty, value); }
        }
        public int SelectedPath { get; set; } = -1;

        private Point PrevPoint { get; set; }
        private Point RenderOffset { get; set; } = new Point(0, 0);
        private float zoomLevel = 0.5f;

        private readonly List<Obj> articles = new List<Obj>();

        public ProcedureBase ActiveProcedure { get; private set; }

        public RoomEditor()
        {
            InitializeComponent();
            if (Instance == null)
                Instance = this;

            //GeometryDrawing backgroundSquare = new GeometryDrawing(System.Windows.Media.Brushes.White, null, new RectangleGeometry(new Rect(0, 0, 2, 2)));

            //GeometryGroup aGeometryGroup = new GeometryGroup();
            //aGeometryGroup.Children.Add(new RectangleGeometry(new Rect(0, 0, 1, 1)));
            //aGeometryGroup.Children.Add(new RectangleGeometry(new Rect(1, 1, 1, 1)));

            //GeometryDrawing checkers = new GeometryDrawing(System.Windows.Media.Brushes.DarkGray, null, aGeometryGroup);

            //DrawingGroup checkersDrawingGroup = new DrawingGroup();
            //checkersDrawingGroup.Children.Add(backgroundSquare);
            //checkersDrawingGroup.Children.Add(checkers);

            //gridBrush.Drawing = checkersDrawingGroup;
            //gridBrush.Viewport = new Rect(0, 0, 300, 300);
            //gridBrush.ViewportUnits = BrushMappingMode.Absolute;
            //gridBrush.TileMode = TileMode.Tile;
            //gridBrush.Stretch = Stretch.UniformToFill;

            // Set up the initial state for the D3DImage.
            HRESULT.Check(SetSize(512, 512));
            HRESULT.Check(SetAlpha(false));
            HRESULT.Check(SetNumDesiredSamples(4));

            //
            // Optional: Subscribing to the IsFrontBufferAvailableChanged event.
            //
            // If you don't render every frame (e.g. you only render in
            // reaction to a button click), you should subscribe to the
            // IsFrontBufferAvailableChanged event to be notified when rendered content
            // is no longer being displayed. This event also notifies you when
            // the D3DImage is capable of being displayed again.

            // For example, in the button click case, if you don't render again when
            // the IsFrontBufferAvailable property is set to true, your
            // D3DImage won't display anything until the next button click.
            //
            // Because this application renders every frame, there is no need to
            // handle the IsFrontBufferAvailableChanged event.
            //
            CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);

            //
            // Optional: Multi-adapter optimization
            //
            // The surface is created initially on a particular adapter.
            // If the WPF window is dragged to another adapter, WPF
            // ensures that the D3DImage still shows up on the new
            // adapter.
            //
            // This process is slow on Windows XP.
            //
            // Performance is better on Vista with a 9Ex device. It's only
            // slow when the D3DImage crosses a video-card boundary.
            //
            // To work around this issue, you can move your surface when
            // the D3DImage is displayed on another adapter. To
            // determine when that is the case, transform a point on the
            // D3DImage into screen space and find out which adapter
            // contains that screen space point.
            //
            // When your D3DImage straddles two adapters, nothing
            // can be done, because one will be updating slowly.
            //
            _adapterTimer = new DispatcherTimer();
            _adapterTimer.Tick += new EventHandler(AdapterTimer_Tick);
            _adapterTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            _adapterTimer.Start();

            //
            // Optional: Surface resizing
            //
            // The D3DImage is scaled when WPF renders it at a size
            // different from the natural size of the surface. If the
            // D3DImage is scaled up significantly, image quality
            // degrades.
            //
            // To avoid this, you can either create a very large
            // texture initially, or you can create new surfaces as
            // the size changes. Below is a very simple example of
            // how to do the latter.
            //
            // By creating a timer at Render priority, you are guaranteed
            // that new surfaces are created while the element
            // is still being arranged. A 200 ms interval gives
            // a good balance between image quality and performance.
            // You must be careful not to create new surfaces too
            // frequently. Frequently allocating a new surface may
            // fragment or exhaust video memory. This issue is more
            // significant on XDDM than it is on WDDM, because WDDM
            // can page out video memory.
            //
            // Another approach is deriving from the Image class,
            // participating in layout by overriding the ArrangeOverride method, and
            // updating size in the overriden method. Performance will degrade
            // if you resize too frequently.
            //
            // Blurry D3DImages can still occur due to subpixel
            // alignments.
            //
            _sizeTimer = new DispatcherTimer(DispatcherPriority.Render);
            _sizeTimer.Tick += new EventHandler(SizeTimer_Tick);
            _sizeTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            _sizeTimer.Start();
        }

        ~RoomEditor()
        {
            Destroy();
        }

        void AdapterTimer_Tick(object sender, EventArgs e)
        {
            //POINT p = new POINT(imgelt.PointToScreen(new Point(0, 0)));

            //HRESULT.Check(SetAdapter(p));
        }

        void SizeTimer_Tick(object sender, EventArgs e)
        {
            // The following code does not account for RenderTransforms.
            // To handle that case, you must transform up to the root and
            // check the size there.

            // Given that the D3DImage is at 96.0 DPI, its Width and Height
            // properties will always be integers. ActualWidth/Height
            // may not be integers, so they are cast to integers.
            uint actualWidth = (uint)imgelt.ActualWidth;
            uint actualHeight = (uint)imgelt.ActualHeight;
            if ((actualWidth > 0 && actualHeight > 0) &&
                (actualWidth != (uint)d3dimg.Width || actualHeight != (uint)d3dimg.Height))
            {
                HRESULT.Check(SetSize(actualWidth, actualHeight));
            }
        }

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            RenderingEventArgs args = (RenderingEventArgs)e;

            // It's possible for Rendering to call back twice in the same frame
            // so only render when we haven't already rendered in this frame.
            if (d3dimg.IsFrontBufferAvailable && _lastRender != args.RenderingTime)
            {
                IntPtr pSurface = IntPtr.Zero;
                HRESULT.Check(GetBackBufferNoRef(out pSurface));
                if (pSurface != IntPtr.Zero)
                {
                    d3dimg.Lock();
                    // Repeatedly calling SetBackBuffer with the same IntPtr is
                    // a no-op. There is no performance penalty.
                    d3dimg.SetBackBuffer(D3DResourceType.IDirect3DSurface9, pSurface);
                    SetCameraTransform(new Point (RenderOffset.X, RenderOffset.Y), zoomLevel);
                    if(!Instance.LoadedImages.ContainsKey("roaam_path"))
                    {
                        LoadDefaults();
                    }
                    StartDraw();
                    foreach (var art in articles)
                    {
                        var spriteName = art.Sprite;
                        if (!Instance.LoadedImages.ContainsKey(spriteName))
                        {
                            bool hasSprite = false;
                            if (!string.IsNullOrEmpty(spriteName))
                                hasSprite = RoomEditor.LoadImage(spriteName);
                            if (!hasSprite)
                            {
                                var overrideSpr = FindResource("EmptyImage") as BitmapImage;
                                Instance.LoadedImages[spriteName] = new TexData { exists = false };
                                RegisterTexture(spriteName, System.AppDomain.CurrentDomain.BaseDirectory + overrideSpr.UriSource.LocalPath, 1);
                            }
                        }
                        var data = Instance.LoadedImages[spriteName];
                        var offset = art.RealPoint - new Vector(data.offset.X, data.offset.Y);
                        Point scale = new Point (1, 1);
                        System.Drawing.Color color = System.Drawing.Color.White;
                        switch(art.Article)
                        {
                            case ArticleType.Terrain:
                                scale = new Point (2, 2);
                                break;
                            case ArticleType.Zone:
                                if (art is Zone zone)
                                {
                                    scale = new Point (zone.TriggerWidth, zone.TriggerHeight);
                                    //blastzone
                                    switch(zone.EventID)
                                    {
                                        case 4:
                                            color = System.Drawing.Color.Red;
                                            break;
                                    }
                                    color = System.Drawing.Color.FromArgb(64, color.R, color.G, color.B);
                                    if (SelectedObj == zone)
                                    {
                                        var sqrOffset = Instance.LoadedImages["roaam_square"].offset;
                                        foreach (var point in ROAAM_CONST.ZONE_POINTS)
                                        {
                                            PushSprite(new DX_Article { Translate = new Point(zone.RealPoint.X + point.X * zone.TriggerWidth - sqrOffset.X / zoomLevel, zone.RealPoint.Y + point.Y * zone.TriggerHeight - sqrOffset.Y / zoomLevel), Scale = new Point(1 / zoomLevel, 1 / zoomLevel), Depth = -15, name = "roaam_square", Color = System.Drawing.Color.White.ToArgb() });
                                        }
                                    }
                                }
                                break;
                            case ArticleType.Target:
                                if (SelectedObj == art && (art as Target).Path.Any())
                                {
                                    Point prev = (art as Target).Path.Last();
                                    for(int i = 0; i < (art as Target).Path.Count; i++)
                                    {
                                        var point = (art as Target).Path[i];
                                        var targOffset = Instance.LoadedImages["roaam_path"].offset;
                                        PushSprite(new DX_Article { Translate = new Point(point.X * ROAAM_CONST.GRID_SIZE - targOffset.Y, point.Y * ROAAM_CONST.GRID_SIZE - targOffset.Y), Scale = new Point(1, 1), Depth = -10, name = "roaam_path", Color = System.Drawing.Color.White.ToArgb() });
                                        if (SelectedObj == art)
                                        {
                                            var c = System.Drawing.Color.FromArgb(64, 255, 255, 255);
                                            PushSprite(new DX_Article { Translate = new Point(point.X * ROAAM_CONST.GRID_SIZE - data.offset.X, point.Y * ROAAM_CONST.GRID_SIZE - data.offset.Y), Scale = scale, Depth = art.Depth, name = spriteName, Color = c.ToArgb() } );
                                            if(SelectedPath == i)
                                            {
                                                var arrowOffset = Instance.LoadedImages["roaam_arrows"].offset;
                                                PushSprite(new DX_Article { Translate = new Point(point.X * ROAAM_CONST.GRID_SIZE - arrowOffset.X / zoomLevel, point.Y * ROAAM_CONST.GRID_SIZE - arrowOffset.Y / zoomLevel), Scale = new Point(1 / zoomLevel, 1 / zoomLevel), Depth = -15, name = "roaam_arrows", Color = System.Drawing.Color.White.ToArgb() });

                                            }
                                        }
                                        int pathColor = System.Drawing.Color.White.ToArgb();
                                        float width = 2;
                                        if (i == ObjViewer.Instance.HighlightedPath)
                                        {
                                            pathColor = System.Drawing.Color.Orange.ToArgb();
                                            width = 4;
                                        }
                                        PushLine(new Point (prev.X * ROAAM_CONST.GRID_SIZE, prev.Y * ROAAM_CONST.GRID_SIZE), new Point(point.X * ROAAM_CONST.GRID_SIZE, point.Y * ROAAM_CONST.GRID_SIZE), pathColor, width);
                                        prev = point;
                                    }
                                }
                                break;
                        }
                        PushSprite(new DX_Article { Translate = new Point (offset.X, offset.Y), Scale = scale, Depth = art.Depth, name = spriteName, Color = SelectedObj == art ? System.Drawing.Color.Orange.ToArgb() : color.ToArgb() });
                        if (SelectedObj == art && SelectedPath == -1)
                        {
                            var arrowOffset =  Instance.LoadedImages["roaam_arrows"].offset;
                            if (art is Zone zone)
                            {
                                offset = new Point(offset.X + zone.TriggerWidth / 2, offset.Y + zone.TriggerHeight / 2);
                            }
                            PushSprite(new DX_Article { Translate = new Point(offset.X - arrowOffset.X / zoomLevel, offset.Y - arrowOffset.Y / zoomLevel), Scale = new Point(1 / zoomLevel, 1 / zoomLevel), Depth = -15, name = "roaam_arrows", Color = System.Drawing.Color.White.ToArgb() });
                        }
                    }
                    PushSprite(new DX_Article { Translate = ApplicationSettings.Instance.ActiveProject.RespawnPoint - (Vector)Instance.LoadedImages["roaam_respawn"].offset, Depth = 5, name = "roaam_respawn", Scale = new Point(1, 1), Color = System.Drawing.Color.White.ToArgb() });

                    HRESULT.Check(Render());
                    d3dimg.AddDirtyRect(new Int32Rect(0, 0, d3dimg.PixelWidth, d3dimg.PixelHeight));
                    d3dimg.Unlock();

                    _lastRender = args.RenderingTime;
                }
            }
        }

        DispatcherTimer _sizeTimer;
        DispatcherTimer _adapterTimer;
        TimeSpan _lastRender;

        // Import the methods exported by the unmanaged Direct3D content.

        [DllImport("D3DContent.dll")]
        static extern int GetBackBufferNoRef(out IntPtr pSurface);

        [DllImport("D3DContent.dll")]
        static extern int SetSize(uint width, uint height);

        [DllImport("D3DContent.dll")]
        static extern int SetAlpha(bool useAlpha);

        [DllImport("D3DContent.dll")]
        static extern int SetNumDesiredSamples(uint numSamples);

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
        static extern int RegisterTexture([MarshalAs(UnmanagedType.LPStr)]string key, [MarshalAs(UnmanagedType.LPWStr)]string fname, int frames);

        [DllImport("D3DContent.dll")]
        static extern int SetAdapter(POINT screenSpacePoint);

        [DllImport("D3DContent.dll")]
        static extern int SetCameraTransform(Point pos, float zoom);

        [DllImport("D3DContent.dll")]
        static extern int StartDraw();

        [DllImport("D3DContent.dll")]
        static extern int PushSprite(DX_Article art);

        [DllImport("D3DContent.dll")]
        static extern int PushLine(Point start, Point end, int color, float width);

        [DllImport("D3DContent.dll")]
        static extern int Render();

        [DllImport("D3DContent.dll")]
        static extern void Destroy();

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
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
                foreach (var art in ApplicationSettings.Instance.ActiveRoom.Objs)
                {
                    articles.Add(art);
                }
            }
        }

        private void UpdateArticles(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (Obj article in e.NewItems)
                {
                    articles.Add(article);
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (Obj article in e.OldItems)
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
            var loadFile = Path.Combine(Path.GetDirectoryName(ApplicationSettings.Instance.ActiveProject.ProjectPath), "scripts", "load.gml");
            MatchCollection matches = null;
            if(File.Exists(loadFile))
            {
                var lines = File.ReadAllText(loadFile);
                matches = Regex.Matches(lines, "sprite_change_offset\\s*\\(\\s*\"([\\w\\d]+)\",\\s*(\\d+),\\s*(\\d+)\\s*\\)");
            }
            string directory;
            if(ApplicationSettings.Instance.ActiveProject.Type == ProjectType.AdventureMode)
                directory = Path.Combine(Path.GetDirectoryName(ApplicationSettings.Instance.ActiveProject.ProjectPath), "sprites", "articles");
            else
                directory = Path.Combine(Path.GetDirectoryName(ApplicationSettings.Instance.ActiveProject.ProjectPath), "sprites");
            if (File.Exists(Path.Combine(directory, name + ".png")))
            {
                var path = Path.Combine(directory, name + ".png");
                System.Drawing.Bitmap img = null;
                using (FileStream file = new FileStream(path, FileMode.Open))
                {
                    MemoryStream stream = new MemoryStream();
                    file.CopyTo(stream);
                    img = new System.Drawing.Bitmap(stream);
                }
                RegisterTexture(name, path, 1);
                if (matches != null)
                {
                    var match = matches.OfType<Match>().FirstOrDefault(m => m.Groups[1].Value == name);
                    if (match != null)
                    {
                        offset.X = Double.Parse(match.Groups[2].Value);
                        offset.Y = Double.Parse(match.Groups[3].Value);
                    }
                }
                Instance.LoadedImages[name] = new TexData { exists = true, offset = offset, image = img };
                return true;
            }
            var files = Directory.EnumerateFiles(directory, name + "*.png");
            if (files.Any())
            {
                var file = files.FirstOrDefault(f => Regex.Match(f, name + "_strip(\\d+)").Success);
                if (!string.IsNullOrEmpty(file))
                {
                    var match = Regex.Match(file, "strip(\\d+)");
                    var count = int.Parse(match.Groups[1].Value);
                    System.Drawing.Bitmap img = null;
                    using (FileStream fstream = new FileStream(file, FileMode.Open))
                    {
                        MemoryStream stream = new MemoryStream();
                        fstream.CopyTo(stream);
                        img = new System.Drawing.Bitmap(stream);
                    }
                    RegisterTexture(name, file, count);
                    var index = file.IndexOf("_strip");
                    if (matches != null)
                    {
                        var offsetMatch = matches.OfType<Match>().FirstOrDefault(m => m.Groups[1].Value == file.Substring(0, index));
                        if (offsetMatch != null)
                        {
                            offset.X = Double.Parse(offsetMatch.Groups[2].Value);
                            offset.Y = Double.Parse(offsetMatch.Groups[3].Value);
                        }
                    }
                    Instance.LoadedImages[name] = new TexData { exists = true, offset = offset, image = img };
                    return true;
                }
            }
            Instance.LoadedImages[name] = new TexData { exists = false, offset = offset };
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
                    RenderOffset += (point - PrevPoint) * (1 / zoomLevel);
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

            Obj top = null;

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
                    switch (art.Article)
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

        private void OnLayoutUpdate(object sender, EventArgs e)
        {
            var bounds = LayoutInformation.GetLayoutSlot(this);
            HRESULT.Check(SetSize((uint)bounds.Width, (uint)bounds.Height));
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
            RegisterTexture("roaam_path", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1);
            Instance.LoadedImages["roaam_path"] = new TexData { exists = true, offset = new Point (12, 12), image = BitmapImageToBitmap(spr)};

            spr = FindResource("EmptyImage") as BitmapImage;
            RegisterTexture("roaam_empty", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1);
            Instance.LoadedImages["roaam_empty"] = new TexData { exists = true, offset = new Point (9, 9), image = BitmapImageToBitmap(spr) };

            spr = FindResource("WhitePixel") as BitmapImage;
            RegisterTexture("roaam_zone", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1);
            Instance.LoadedImages["roaam_zone"] = new TexData { exists = true, offset = new Point { }, image = BitmapImageToBitmap(spr) };

            spr = FindResource("TargetSprite") as BitmapImage;
            RegisterTexture("roaam_target", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1);
            Instance.LoadedImages["roaam_target"] = new TexData { exists = true, offset = new Point { }, image = BitmapImageToBitmap(spr) };

            spr = FindResource("Arrows") as BitmapImage;
            RegisterTexture("roaam_arrows", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1);
            Instance.LoadedImages["roaam_arrows"] = new TexData { exists = true, offset = new Point(56, 56), image = BitmapImageToBitmap(spr) };

            spr = FindResource("Square") as BitmapImage;
            RegisterTexture("roaam_square", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1);
            Instance.LoadedImages["roaam_square"] = new TexData { exists = true, offset = new Point(18, 18), image = BitmapImageToBitmap(spr) };

            spr = FindResource("Respawn") as BitmapImage;
            RegisterTexture("roaam_respawn", System.AppDomain.CurrentDomain.BaseDirectory + spr.UriSource.LocalPath, 1);
            Instance.LoadedImages["roaam_respawn"] = new TexData { exists = true, offset = new Point(16, 32), image = BitmapImageToBitmap(spr) };
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
        public bool exists;
        public Point offset;
        public System.Drawing.Bitmap image;
    }

    struct DX_Article
    {
        [MarshalAs(UnmanagedType.LPStr)] public string name;
        public Point Translate;
        public Point Scale;
        public float Depth;
        public int Color;
    }
}
