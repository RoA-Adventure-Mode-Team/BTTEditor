using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RivalsAdventureEditor.DrawingObjects
{
    public class ArticleManipulatorAdorner : Adorner
    {
        public static Geometry UpArrow = Geometry.Parse("M 20 0 L 20 50 L 0 50 L 40 100 L 80 50 L 60 50 L 60 0 L 20 0");
        public static Geometry RightArrow = UpArrow.Clone();

        public DrawingArticle SelectedArticle { get; set; } = null;

        static ArticleManipulatorAdorner()
        {
            RightArrow.Transform = new RotateTransform(-90);
        }

        public ArticleManipulatorAdorner(UIElement adornedElement) : base(adornedElement)
        {
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (SelectedArticle != null)
            {
                drawingContext.PushTransform(SelectedArticle.Transform);
                drawingContext.DrawGeometry(Brushes.Green, null, UpArrow);
                drawingContext.DrawGeometry(Brushes.Red, null, RightArrow);
                drawingContext.Close();
            }
        }
    }
}
