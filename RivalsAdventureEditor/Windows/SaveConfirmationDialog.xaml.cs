using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RivalsAdventureEditor.Windows
{
    /// <summary>
    /// Interaction logic for SaveConfirmationDialog.xaml
    /// </summary>
    public partial class SaveConfirmationDialog : Window
    {
        public bool? Result { get; set; }
        public ImageSource WarningIcon => ToImageSource(SystemIcons.Warning);
        public SaveConfirmationDialog()
        {
            InitializeComponent();
        }
        public static ImageSource ToImageSource(Icon icon)
        {
            ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            return imageSource;
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            Result = true;
            Close();
        }

        private void OnNoSave(object sender, RoutedEventArgs e)
        {
            Result = false;
            Close();
        }
    }
}
