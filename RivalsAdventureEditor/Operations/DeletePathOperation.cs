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
    public class DeletePathOperation : OperationBase
    {
        public Target Obj { get; set; }
        public Point Pos { get; set; }
        int SelectedIndex { get; set; }
        public override string Parameter => $"{Obj.Article.ToString()}({Obj.Name}), Point {SelectedIndex}";
        public DeletePathOperation(Project project, Target obj, int selectedIndex) : base(project)
        {
            Pos = obj.Path[selectedIndex];
            Obj = obj;
            SelectedIndex = selectedIndex;
        }
        public override void Execute()
        {
            if (Obj != null)
            {
                Obj.Path.RemoveAt(SelectedIndex);
                RoomEditor.Instance.SelectedPath = -1;
            }
        }

        public override void Undo()
        {
            if(Obj != null)
            {
                Obj.Path.Insert(SelectedIndex, Pos);
                RoomEditor.Instance.SelectedPath = SelectedIndex;
                RoomEditor.Instance.SelectedObj = Obj;
            }
        }
    }
}
