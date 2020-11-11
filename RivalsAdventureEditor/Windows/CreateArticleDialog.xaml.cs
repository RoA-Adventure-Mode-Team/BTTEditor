using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Operations;
using RivalsAdventureEditor.Panels;
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
using System.Windows.Shapes;

namespace RivalsAdventureEditor.Windows
{
    /// <summary>
    /// Interaction logic for CreateArticleDialog.xaml
    /// </summary>
    public partial class CreateArticleDialog : Window
    {
        public CreateArticleDialog()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var type = (ArticleType)(articleType.SelectedItem as ComboBoxItem).Tag;
            Obj obj = null;
            switch(type)
            {
                case ArticleType.Terrain:
                    obj = new Terrain();
                    break;
                case ArticleType.Zone:
                    obj = new Zone();
                    break;
                case ArticleType.Target:
                    obj = new Target();
                    (obj as Target).MoveVel.Add(0);
                    break;
            }
            if (obj == null)
                return;
            obj.Article = type;
            var point = RoomEditor.Instance.GetTransform().Transform(new Point(RoomEditor.Instance.ActualWidth / 2, RoomEditor.Instance.ActualHeight / 2));
            // Round to nearest 1/16
            obj.X = (int)point.X / (float)ROAAM_CONST.GRID_SIZE;
            obj.Y = (int)point.Y / (float)ROAAM_CONST.GRID_SIZE;
            obj.Name = obj.Article.ToString();

            var op = new CreateArticleOperation(ApplicationSettings.Instance.ActiveProject, obj);
            ApplicationSettings.Instance.ActiveProject.ExecuteOp(op);
            Close();
        }
    }
}
