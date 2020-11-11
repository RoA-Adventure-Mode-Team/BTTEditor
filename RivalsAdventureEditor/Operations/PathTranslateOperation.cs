using RivalsAdventureEditor.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RivalsAdventureEditor.Operations
{
    public class PathTranslateOperation : OperationBase
    {
        public Target Obj { get; set; }
        public Point StartPos { get; set; }
        public Point EndPos { get; set; }
        int SelectedIndex { get; set; }
        public override string Parameter => $"{Obj.Article.ToString()}({Obj.Name}), Point {SelectedIndex}, ({EndPos.X}, {EndPos.Y})";

        public PathTranslateOperation(Project project, Target obj, Point pos, int selectedIndex) : base(project)
        {
            StartPos = obj.Path[selectedIndex];
            EndPos = pos;
            Obj = obj;
            SelectedIndex = selectedIndex;
        }
        public override void Execute()
        {
            if (Obj != null)
            {
                Obj.Path[SelectedIndex] = EndPos;
            }
        }

        public override void Undo()
        {
            if(Obj != null)
            {
                Obj.Path[SelectedIndex] = StartPos;
            }
        }
    }
}
