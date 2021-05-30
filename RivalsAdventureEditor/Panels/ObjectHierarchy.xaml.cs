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
using System.Globalization;
using System.Drawing;

namespace RivalsAdventureEditor.Panels
{
    /// <summary>
    /// Interaction logic for ProjectView.xaml
    /// </summary>
    public partial class ObjectHierarchy : ScrollViewer
    {
        public static ObjectHierarchy Instance { get; set; } = null;
        public DependencyProperty RoomsProperty = DependencyProperty.Register(nameof(Rooms), typeof(ObservableCollection<Room>), typeof(ObjectHierarchy));
        public ObservableCollection<Room> Rooms
        {
            get { return GetValue(RoomsProperty) as ObservableCollection<Room>; }
            set { SetValue(RoomsProperty, value); }
        }

        public ObjectHierarchy()
        {
            if (Instance == null)
                Instance = this;
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (ApplicationSettings.Instance.ActiveProject != null)
            {
                Rooms = ApplicationSettings.Instance.ActiveProject.Rooms;
                SetActiveRoom(ApplicationSettings.Instance.ActiveProject.Rooms.FirstOrDefault());
            }
        }

        public void SetActiveRoom(Room room)
        {
            ApplicationSettings.Instance.ActiveRoom = room;
            foreach(var r in Rooms)
            {
                r.IsActive = r == ApplicationSettings.Instance.ActiveRoom;
            }
            RoomEditor.Instance.Reload();
        }

        private void BindArticleToSelected(object sender, RoutedEventArgs e)
        {
            if(sender is TextBox textBox)
            {
                textBox.SetBinding(TextBox.BackgroundProperty, new Binding("SelectedObj") { 
                    Source = RoomEditor.Instance, 
                    Converter = new SelectedObjConverter { Article = textBox.DataContext } ,
                    Mode = BindingMode.OneWay,
                });
            }
        }

        private void SelectArticle(object sender, MouseButtonEventArgs e)
        {
            if(sender is TextBox textBox)
            {
                RoomEditor.Instance.SelectedObj = textBox.DataContext as Article;
            }
        }
    }

    public class SelectedObjConverter : IValueConverter
    {
        public object Article { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == Article)
                return new SolidBrush(Color.FromArgb(102, 219, 255));
            else
                return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
