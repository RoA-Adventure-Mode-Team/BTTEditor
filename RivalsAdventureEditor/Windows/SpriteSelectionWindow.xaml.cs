using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Panels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RivalsAdventureEditor.Windows
{
    /// <summary>
    /// Interaction logic for SpriteSelectionWindow.xaml
    /// </summary>
    public partial class SpriteSelectionWindow : Window
    {
        public ObservableCollection<string> Sprites { get; } = new ObservableCollection<string>();
        public string Sprite { get; set; }

        public SpriteSelectionWindow()
        {
            InitializeComponent();
        }

        private void FindSprites(object sender, RoutedEventArgs e)
        {
            var directory = Path.Combine(Path.GetDirectoryName(ApplicationSettings.Instance.ActiveProject.ProjectPath), "sprites");
            Sprites.Clear();
            foreach(var spriteFile in Directory.GetFiles(directory))
            {
                var shortname = Path.GetFileNameWithoutExtension(spriteFile);
                var match = Regex.Match(shortname, "_strip\\d+");
                if (match.Success)
                    shortname = shortname.Remove(match.Index);
                if (!RoomEditor.Instance.LoadedImages.ContainsKey(shortname))
                {
                    WindowAPI.LoadImage(shortname, RoomEditor.Instance.renderer, out TexData data);
                    RoomEditor.Instance.LoadedImages.Add(shortname, data);
                }
                Sprites.Add(shortname);
            }
        }

        private void GetSource(object sender, RoutedEventArgs e)
        {
            (sender as Image).Source = BitmapToImage(RoomEditor.Instance.GetImage((sender as Image).DataContext as string));
        }

        public BitmapImage BitmapToImage(System.Drawing.Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        private void SetImage(object sender, RoutedEventArgs e)
        {
            Sprite = (sender as FrameworkElement).DataContext as string;
            if (IsVisible)
                Close();
        }

        private void OnLostFocus(object sender, EventArgs e)
        {
            if (IsVisible)
                Close();
        }
    }
}
