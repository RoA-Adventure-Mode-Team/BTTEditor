using RivalsAdventureEditor.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace RivalsAdventureEditor.DrawingObjects
{
    public class EditorCanvas : FrameworkElement
    {
        private VisualCollection _children;
        private List<Obj> _articles = new List<Obj>();
        private DepthComparer comparer;

        public EditorCanvas()
        {
            _children = new VisualCollection(this);
            comparer = new DepthComparer();
        }

        public void SetBackground(Visual visual)
        {
            _children.Add(visual);
        }

        public void InsertArticle(Obj article)
        {
            var index = _articles.BinarySearch(article, comparer);
            if (index < 0)
                index = ~index;
            _articles.Insert(index, article);
            var art = new DrawingArticle() { Article = article };
            art.UpdateDrawing();
            _children.Insert(index + 1, art);
        }

        public void RemoveArticle(Obj article)
        {
            _articles.Remove(article);
            for(int i = 0; i < _children.Count; i++)
            {
                if (_children[i] is DrawingArticle art && art.Article == article)
                    _children.Remove(art);
            }
        }

        public void ClearArticles()
        {
            _articles.Clear();
            _children.RemoveRange(1, _children.Count - 1);
        }

        protected override int VisualChildrenCount => _children.Count;

        protected override Visual GetVisualChild(int index)
        {
            return _children[index];
        }
    }

    public class DepthComparer : IComparer<Obj>
    {
        public int Compare(Obj x, Obj y)
        {
            return x.Depth.CompareTo(y.Depth);
        }
    }
}
