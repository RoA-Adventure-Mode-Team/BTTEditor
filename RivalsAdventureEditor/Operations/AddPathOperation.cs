using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Panels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RivalsAdventureEditor.Operations
{
    public class AddPathOperation : OperationBase
    {
        public Target Obj { get; set; }
        public Point Pos { get; set; }
        int SelectedIndex { get; set; }

        public override string Parameter => $"{Obj.Article.ToString()}({Obj.Name}), Point {SelectedIndex}";

        public AddPathOperation(Project project, Target obj, int selectedIndex, Point pos) : base(project)
        {
            Pos = pos;
            Obj = obj;
            SelectedIndex = selectedIndex;
        }
        public override void Execute()
        {
            if (Obj != null)
            {
                Obj.Path.Insert(SelectedIndex, Pos);
                RoomEditor.Instance.SelectedPath = SelectedIndex;
            }
        }

        public override void Undo()
        {
            if (Obj != null)
            {
                Obj.Path.RemoveAt(SelectedIndex);
                RoomEditor.Instance.SelectedPath = -1;
                RoomEditor.Instance.SelectedObj = Obj;
            }
        }
    }
}
