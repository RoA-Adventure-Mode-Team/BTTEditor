using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.IO;
using RivalsAdventureEditor.Operations;
using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Windows;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xceed.Wpf.Toolkit;
using System.Threading;
using Xceed.Wpf.AvalonDock.Layout;

namespace RivalsAdventureEditor.Panels
{
    /// <summary>
    /// Interaction logic for ProjectView.xaml
    /// </summary>
    public partial class ObjViewer : ScrollViewer
    {
        public static DependencyProperty ArticleProperty = DependencyProperty.Register(nameof(Article), 
            typeof(ArticleViewModel), 
            typeof(ObjViewer));
        public ArticleViewModel Article
        {
            get { return GetValue(ArticleProperty) as ArticleViewModel; }
            set { SetValue(ArticleProperty, value); }
        }

        public static ObjViewer Instance { get; set; } = null;
        public Article SelectedObj { get; set; }
        public int HighlightedPath { get; set; }

        public ObjViewer()
        {
            if (Instance == null)
                Instance = this;
            Article = new ArticleViewModel();
            Article.PropertyChangedValue += Article_PropertyChangedValue;
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Update();
        }

        public void Update()
        {
            foreach(var item in editorPanel.Children)
            {
                if(item is FrameworkElement elem)
                {
                    elem.Visibility = Visibility.Collapsed;
                }
            }
            if (RoomEditor.Instance != null && RoomEditor.Instance.SelectedObj != null)
            {
                SelectedObj = RoomEditor.Instance.SelectedObj;
                Article.Article = SelectedObj;
                Article.NoInvoke = true;

                headerGrid.Visibility = Visibility.Visible;
                Article.Name = SelectedObj.Name;

                positionLabel.Visibility = Visibility.Visible;
                positionGrid.Visibility = Visibility.Visible;
                Article.X = (int)(SelectedObj.X * 16);
                Article.Y = (int)(SelectedObj.Y * 16);

                depth.Visibility = Visibility.Visible;
                depthBox.Visibility = Visibility.Visible;
                Article.Depth = SelectedObj.Depth;
                HighlightedPath = -1;

                switch (SelectedObj.ArticleNum)
                {
                    case ArticleType.Terrain:
                        Terrain terrain = SelectedObj as Terrain;
                        spriteName.Visibility = Visibility.Visible;
                        spriteNameBox.Visibility = Visibility.Visible;
                        Article.Sprite = terrain.Sprite;

                        animationSpeed.Visibility = Visibility.Visible;
                        animSpeedBox.Visibility = Visibility.Visible;
                        Article.AnimationSpeed = terrain.AnimationSpeed;

                        collisionType.Visibility = Visibility.Visible;
                        collisionTypeBox.Visibility = Visibility.Visible;
                        Article.Type = terrain.Type;

                        staticPanel.Visibility = Visibility.Visible;
                        staticBox.Visibility = Visibility.Visible;
                        Article.Static = terrain.Static;
                        break;
                    case ArticleType.Zone:
                        Zone zone = SelectedObj as Zone;
                        sizeBox.Visibility = Visibility.Visible;
                        resizeGrid.Visibility = Visibility.Visible;
                        Article.TriggerWidth = zone.TriggerWidth;
                        Article.TriggerHeight = zone.TriggerHeight;
                        break;
                    case ArticleType.Target:
                        Target targ = SelectedObj as Target;
                        spriteName.Visibility = Visibility.Visible;
                        spriteNameBox.Visibility = Visibility.Visible;
                        Article.Sprite = targ.Sprite;

                        destroySprite.Visibility = Visibility.Visible;
                        destroySpriteBox.Visibility = Visibility.Visible;
                        Article.DestroySprite = targ.DestroySprite;

                        if(targ.Path.Count > 0)
                        {
                            targetSpeeds.Visibility = Visibility.Visible;
                            targetSpeedList.Visibility = Visibility.Visible;
                            Article.TargetSpeedList.Clear();
                            for(int i = 0; i < targ.Path.Count; i++)
                            {
                                if (i >= targ.MoveVel.Count)
                                    targ.MoveVel.Add(targ.MoveVel.Last());
                                Article.TargetSpeedList.Add(targ.MoveVel[i]);
                            }
                        }

                        if (RoomEditor.Instance.SelectedPath != -1)
                        {

                        }
                        break;
                    case ArticleType.Tilemap:
                        Tilemap tilemap = SelectedObj as Tilemap;
                        editTilemapButton.Visibility = Visibility.Visible;
                        editTilemapButton.Content = (RoomEditor.Instance.Overlay.Visibility == Visibility.Visible) ? "Stop Editing Tilemap" : "Edit Tilemap";
                        tilesetEditor.Visibility = Visibility.Visible;
                        break;
                }

                Article.NoInvoke = false;
            }
        }


        private void OnAnimationSpeedChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (SelectedObj is Terrain terrain)
            {
                terrain.AnimationSpeed = (float)(e.NewValue as float?);
            }
        }

        private void OnStaticChanged(object sender, RoutedEventArgs e)
        {
            if (SelectedObj is Terrain terrain)
            {
                if (terrain.Static != (staticBox.IsChecked ?? false))
                {
                    var op = new ModifyPropertyOperation(ApplicationSettings.Instance.ActiveProject, SelectedObj, nameof(Terrain.Static), !(staticBox.IsChecked ?? false), staticBox.IsChecked ?? false);
                    ApplicationSettings.Instance.ActiveProject.ExecuteOp(op);
                }
            }
        }

        private void OnCollisionTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedObj != null && SelectedObj.Type != collisionTypeBox.SelectedIndex)
            {
                var op = new ModifyPropertyOperation(ApplicationSettings.Instance.ActiveProject, SelectedObj, nameof(Data.Article.Type), SelectedObj.Type, collisionTypeBox.SelectedIndex);
                ApplicationSettings.Instance.ActiveProject.ExecuteOp(op);
            }
        }

        private void OnDestroySpriteChanged(object sender, RoutedEventArgs e)
        {
            if (SelectedObj != null)
            {
                var window = new SpriteSelectionWindow();
                var obj = SelectedObj;
                window.Sprite = (SelectedObj as Target).DestroySprite;
                window.Closed += (s, args) =>
                {
                    if (obj != null && (obj as Target).DestroySprite != window.Sprite)
                    {
                        var op = new ModifyPropertyOperation(ApplicationSettings.Instance.ActiveProject, obj, nameof(Target.DestroySprite), (SelectedObj as Target).DestroySprite, window.Sprite);
                        ApplicationSettings.Instance.ActiveProject.ExecuteOp(op);
                        Article.NoInvoke = true;
                        Article.DestroySprite = window.Sprite;
                        Article.NoInvoke = false;
                    }
                };
                window.Owner = App.Current.MainWindow;
                window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                window.Show();
            }
        }

        private void OnSpriteNameChanged(object sender, RoutedEventArgs e)
        {
            if (SelectedObj != null)
            {
                var window = new SpriteSelectionWindow();
                var obj = SelectedObj;
                window.Sprite = SelectedObj.Sprite;
                window.Closed += (s, args) =>
                {
                    if (obj != null && obj.Sprite != window.Sprite)
                    {
                        var op = new ModifyPropertyOperation(ApplicationSettings.Instance.ActiveProject, obj, nameof(Data.Article.Sprite), SelectedObj.Sprite, window.Sprite);
                        ApplicationSettings.Instance.ActiveProject.ExecuteOp(op);
                        Article.NoInvoke = true;
                        Article.Sprite = window.Sprite;
                        Article.NoInvoke = false;
                    }
                };
                window.Owner = App.Current.MainWindow;
                window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                window.Show();
            }
        }


        private void OnXChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if(SelectedObj != null && e.NewValue is int val)
                SelectedObj.X = val / 16.0f;
        }

        private void OnYChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (SelectedObj != null && e.NewValue is int val)
                SelectedObj.Y = val / 16.0f;
        }

        private void OnWidthChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (SelectedObj is Zone zone && e.NewValue is int val)
            {
                zone.TriggerWidth = val;
            }
        }

        private void OnHeightChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (SelectedObj is Zone zone && e.NewValue is int val)
            {
                zone.TriggerHeight = val;
            }
        }

        private void Article_PropertyChangedValue(object oldValue, object newValue, string property)
        {
            var op = new ModifyPropertyOperation(ApplicationSettings.Instance.ActiveProject, SelectedObj, property, oldValue, newValue);
            ApplicationSettings.Instance.ActiveProject.ExecuteOp(op);
        }


        private void OnTargetSpeedsChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if((sender as FrameworkElement).Tag is CancellationTokenSource source)
            {
                source.Cancel();
            }
            var obj = SelectedObj;
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            (sender as FrameworkElement).Tag = tokenSource;

            var index = targetSpeedList.ItemContainerGenerator.IndexFromContainer((sender as FrameworkElement).TemplatedParent);

            var project = ApplicationSettings.Instance.ActiveProject;
            Task.Run(() =>
            {
                Thread.Sleep(700);
                if (token.IsCancellationRequested)
                    return;
                Dispatcher.Invoke(() =>
                {
                    var op = new ModifyTargetSpeedOperation(project, (obj as Target), index, e.NewValue as float? ?? 0);
                    project.ExecuteOp(op);
                });
            }, token);
        }

        private void OnTargetFocused(object sender, KeyboardFocusChangedEventArgs e)
        {
            var index = targetSpeedList.ItemContainerGenerator.IndexFromContainer((sender as FrameworkElement).TemplatedParent);
            HighlightedPath = index;
        }

        private void EditTilemapClicked(object sender, RoutedEventArgs e)
        {
            RoomEditor.Instance.Overlay.Visibility = RoomEditor.Instance.Overlay.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            Update();

            TilesetEditor.Instance.Update();
        }
    }

    public class ArticleViewModel : INotifyPropertyChanged
    {
        public Article Article { get; set; }
        public bool NoInvoke { get; set; } = true;

        public string Name
        {
            get { return _name; }
            set
            {
                var temp = _name;
                _name = value;
                OnPropertyChanged(temp, _name);
            }
        }
        string _name;
        public int X
        {
            get { return (int)(_x * 16); }
            set
            {
                var temp = _x;
                _x = value / 16.0f;
                OnPropertyChanged(temp, value / 16.0f);
            }
        }
        private float _x;
        public int Y
        {
            get { return (int)(_y * 16); }
            set
            {
                var temp = _y;
                _y = value / 16.0f;
                OnPropertyChanged(temp, value / 16.0f);
            }
        }
        private float _y;
        public int Depth
        {
            get { return _depth; }
            set
            {
                var temp = _depth;
                _depth = value;
                OnPropertyChanged(temp, value);
            }
        }
        private int _depth;
        public string Sprite
        {
            get { return _sprite; }
            set
            {
                var temp = _sprite;
                _sprite = value;
                OnPropertyChanged(temp, value);
            }
        }
        private string _sprite;
        public string DestroySprite
        {
            get { return _destroySprite; }
            set
            {
                var temp = _destroySprite;
                _destroySprite = value;
                OnPropertyChanged(temp, value);
            }
        }
        private string _destroySprite;
        public float AnimationSpeed
        {
            get { return _animSpeed; }
            set
            {
                var temp = _animSpeed;
                _animSpeed = value;
                OnPropertyChanged(temp, value);
            }
        }
        private float _animSpeed;
        public int Type
        {
            get { return _collisionType; }
            set
            {
                var temp = _collisionType;
                _collisionType = value;
                OnPropertyChanged(temp, value);
            }
        }
        private int _collisionType;
        public bool Static
        {
            get { return _static; }
            set
            {
                var temp = _static;
                _static = value;
                OnPropertyChanged(temp, value);
            }
        }
        private bool _static;
        public int TriggerWidth
        {
            get { return _width; }
            set
            {
                var temp = _width;
                _width = value;
                OnPropertyChanged(temp, value);
            }
        }
        private int _width;
        public int TriggerHeight
        {
            get { return _height; }
            set
            {
                var temp = _height;
                _height = value;
                OnPropertyChanged(temp, value);
            }
        }
        private int _height;

        public ObservableCollection<float> TargetSpeedList { get; set; } = new ObservableCollection<float>();

        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangedValueEventHandler PropertyChangedValue;

        public void OnPropertyChanged(object oldValue, object newValue, [CallerMemberName]string property = null)
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
            if(!NoInvoke)
                PropertyChangedValue?.Invoke(oldValue, newValue, property);
        }
    }

    public delegate void PropertyChangedValueEventHandler(object oldValue, object newValue, string property);
}
