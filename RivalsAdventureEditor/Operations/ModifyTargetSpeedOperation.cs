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
    public class ModifyTargetSpeedOperation : OperationBase
    {
        public Target Obj { get; set; }
        public float OldSpeed { get; set; }
        public float NewSpeed { get; set; }
        int SelectedIndex { get; set; }

        public override string Parameter => $"{Obj.ArticleNum.ToString()}({Obj.Name}), Point {SelectedIndex}, New Speed: {NewSpeed}";

        public ModifyTargetSpeedOperation(Project project, Target obj, int selectedIndex, float speed) : base(project)
        {
            Obj = obj;
            SelectedIndex = selectedIndex;
            OldSpeed = obj.MoveVel[SelectedIndex];
            NewSpeed = speed;
        }
        public override void Execute()
        {
            if (Obj != null)
            {
                Obj.MoveVel[SelectedIndex] = NewSpeed;
                RoomEditor.Instance.SelectedObj = Obj;
                ObjViewer.Instance.Update();
            }
        }

        public override void Undo()
        {
            if (Obj != null)
            {
                Obj.MoveVel[SelectedIndex] = OldSpeed;
                RoomEditor.Instance.SelectedObj = Obj;
                ObjViewer.Instance.Update();
            }
        }
    }
}
