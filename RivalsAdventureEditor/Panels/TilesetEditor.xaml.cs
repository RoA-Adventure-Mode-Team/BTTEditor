using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Operations;
using RivalsAdventureEditor.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
    /// Interaction logic for TilesetEditor.xaml
    /// </summary>
    public partial class TilesetEditor : Grid
    {
        public static TilesetEditor Instance { get; private set; }
        public Tileset SelectedTileset => (RoomEditor.Instance.SelectedObj as Tilemap)?.Tileset;
        public int SelectedTile = 0;

        #region Tileset
        public static DependencyProperty TilesetProperty = DependencyProperty.Register(nameof(Tileset),
            typeof(TilesetViewModel),
            typeof(TilesetEditor));
        public TilesetViewModel Tileset
        {
            get { return GetValue(TilesetProperty) as TilesetViewModel; }
            set { SetValue(TilesetProperty, value); }
        }
        #endregion

        public TilesetEditor()
        {
            Instance = this;
            InitializeComponent();
        }

        private void OnSpriteNameChanged(object sender, RoutedEventArgs e)
        {
            if (SelectedTileset != null)
            {
                var tileset = SelectedTileset;
                var window = new SpriteSelectionWindow();
                window.Sprite = SelectedTileset.SpritePath;
                window.Closed += (s, args) =>
                {
                    if (tileset != null && tileset.SpritePath != window.Sprite)
                    {
                        var op = new ModifyTilesetOperation(ApplicationSettings.Instance.ActiveProject, tileset, nameof(Tileset.SpritePath), SelectedTileset.SpritePath, window.Sprite);
                        ApplicationSettings.Instance.ActiveProject.ExecuteOp(op);
                        TexData spr = viewer.LoadImage(Tileset.SpritePath);
                        SelectedTileset.TileWidth = Clamp(SelectedTileset.TileWidth, 1, spr.image.Width);
                        SelectedTileset.TileHeight = Clamp(SelectedTileset.TileHeight, 1, spr.image.Height);
                        Update();
                    }
                };
                window.Owner = App.Current.MainWindow;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Show();
            }
        }

        public void Update()
        {
            if(Tileset?.Tileset != SelectedTileset)
            {
                if (SelectedTileset == null)
                    Tileset = null;
                else
                {
                    Tileset = new TilesetViewModel(SelectedTileset);
                    Tileset.PropertyChangedValue += Tileset_PropertyChangedValue;
                }
            }
            else if(SelectedTileset != null)
            {
                Tileset.TileWidth = SelectedTileset.TileWidth;
                Tileset.TileHeight = SelectedTileset.TileHeight;
                Tileset.SpritePath = SelectedTileset.SpritePath;
            }
            if (!string.IsNullOrEmpty(SelectedTileset?.SpritePath))
            {
                TexData spr = viewer.LoadImage(Tileset.SpritePath);
                widthBox.Maximum = spr.image.Width;
                heightBox.Maximum = spr.image.Height;
                int tileCount = (spr.image.Width / Tileset.TileWidth) * (spr.image.Height / Tileset.TileHeight);
                SelectedTile = SelectedTile >= tileCount ? tileCount - 1 : SelectedTile;
            }

            tilesetPicker.Items.Clear();
            foreach(var tileset in ApplicationSettings.Instance.ActiveProject.Tilesets)
            {
                tilesetPicker.Items.Add(tileset.Value);
            }
            tilesetPicker.Items.Add(null);
            tilesetPicker.Items.Add(new TilesetDummy { Name = "New Tileset..." });

            if (SelectedTileset != null)
            {
                tilesetPicker.SelectedValuePath = "Name";
                tilesetPicker.SelectedValue = SelectedTileset.Name;
            }
            else
                tilesetPicker.SelectedIndex = 0;

            spriteButton.Content = Tileset?.SpritePath ?? "Select Sprite";
        }

        private void tilesetPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                return;
            if (e.AddedItems[0] is Tileset tileset && RoomEditor.Instance.SelectedObj is Tilemap tilemap)
            {
                ChangeTileset(tileset);
            }
            else
                AddTileset();
        }

        public void ChangeTileset(Tileset t)
        {
            if (RoomEditor.Instance.SelectedObj is Tilemap tilemap && tilemap.TilesetID != t.Name)
            {
                var op = new ModifyPropertyOperation(ApplicationSettings.Instance.ActiveProject, RoomEditor.Instance.SelectedObj, nameof(tilemap.TilesetID), tilemap.TilesetID, t.Name);
                ApplicationSettings.Instance.ActiveProject.ExecuteOp(op);
                Update();
            }
        }

        public void AddTileset()
        {
            var dialog = new TextEntryDialog { DisplayText = "Enter name for new tileset:" };
            dialog.Verification = (string name, out string error) =>
                {
                    error = "";
                    if(string.IsNullOrWhiteSpace(name))
                    {
                        error = "Please enter a name for this tileset.";
                        return false;
                    }
                    else if(ApplicationSettings.Instance.ActiveProject.Tilesets.ContainsKey(name))
                    {
                        error = "A tileset with that name already exists in this project. Please enter a unique name.";
                        return false;
                    }
                    return true;
                };
            Point startupLocation = PointToScreen(new Point(0, 0));
            dialog.Left = startupLocation.X;
            dialog.Top = startupLocation.Y;
            dialog.Title = "New Tilemap...";

            if(dialog.ShowDialog() == true)
            {
                var op = new AddTilesetOperation(ApplicationSettings.Instance.ActiveProject, dialog.InputText);
                ApplicationSettings.Instance.ActiveProject.ExecuteOp(op);
                if (RoomEditor.Instance.SelectedObj is Tilemap tilemap)
                {
                    var tmapOp = new ModifyPropertyOperation(ApplicationSettings.Instance.ActiveProject, RoomEditor.Instance.SelectedObj, nameof(tilemap.TilesetID), tilemap.TilesetID, dialog.InputText);
                    ApplicationSettings.Instance.ActiveProject.ExecuteOp(tmapOp);
                }
            }
            Update();
        }

        private void Tileset_PropertyChangedValue(object oldValue, object newValue, string property)
        {
            var op = new ModifyTilesetOperation(ApplicationSettings.Instance.ActiveProject, SelectedTileset, property, oldValue, newValue);
            ApplicationSettings.Instance.ActiveProject.ExecuteOp(op);
        }

        private void OnWidthChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is int value)
            {
                SelectedTileset.TileWidth = value;
            }
        }
        private void OnHeightChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is int value)
            {
                SelectedTileset.TileHeight = value;
            }
        }

        int Clamp(int value, int min, int max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }

    public class TilesetDummy
    {
        public object Name { get; set; }
    }

    public class TilesetViewModel : INotifyPropertyChanged
    {
        public Tileset Tileset { get; set; }
        public bool NoInvoke { get; set; }

        public TilesetViewModel(Tileset tileset)
        {
            Tileset = tileset;
            TileWidth = tileset.TileWidth;
            TileHeight = tileset.TileHeight;
            SpritePath = tileset.SpritePath;
            Name = tileset.Name;
        }

        #region TileWidth
        public int TileWidth
        {
            get { return _tileWidth; }
            set
            {
                var temp = _tileWidth;
                _tileWidth = value;
                OnPropertyChanged(temp, value);
            }
        }
        private int _tileWidth;
        #endregion

        #region TileHeight
        public int TileHeight
        {
            get { return _tileHeight; }
            set
            {
                var temp = _tileHeight;
                _tileHeight = value;
                OnPropertyChanged(temp, value);
            }
        }
        private int _tileHeight;
        #endregion

        #region SpritePath
        public string SpritePath
        {
            get { return _spritePath; }
            set
            {
                var temp = _spritePath;
                _spritePath = value;
                OnPropertyChanged(temp, value);
            }
        }
        private string _spritePath;
        #endregion

        #region Name
        public string Name { get; private set; }
        #endregion


        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangedValueEventHandler PropertyChangedValue;

        public void OnPropertyChanged(object oldValue, object newValue, [CallerMemberName] string property = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
            if (!NoInvoke)
                PropertyChangedValue?.Invoke(oldValue, newValue, property);
        }
    }
}
