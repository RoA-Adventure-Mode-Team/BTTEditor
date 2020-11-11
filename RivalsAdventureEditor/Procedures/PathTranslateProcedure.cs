using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Operations;
using RivalsAdventureEditor.Panels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace RivalsAdventureEditor.Procedures
{
    public class PathTranslateProcedure : ProcedureBase
    {
        public Target Obj { get; set; }
        public Point StartPoint { get; set; }
        public Point Start { get; set; }
        public int Axes { get; set; }
        public bool Finished { get; set; }
        public int SelectedPoint { get; set; }

        public PathTranslateProcedure(Project project, Target obj, int axes, int selectedPoint) : base(project)
        {
            Obj = obj;
            Axes = axes;
            SelectedPoint = selectedPoint;
        }

        public override void Begin()
        {
            Start = Obj.Path[SelectedPoint];
            var transform = RoomEditor.Instance.GetTransform();
            StartPoint = transform.Transform(Mouse.GetPosition(RoomEditor.Instance));
        }

        public override void Cancel()
        {
            Obj.Path[SelectedPoint] = Start;
            base.Cancel();
        }

        public override void End()
        {
            if (Finished)
                return;
            var end = Obj.Path[SelectedPoint];
            Obj.Path[SelectedPoint] = Start;
            Finished = true;
            var op = new PathTranslateOperation(Project, Obj, end, SelectedPoint);
            Project.ExecuteOp(op);
            base.End();
        }

        public override void Update()
        {
            if(Mouse.LeftButton == MouseButtonState.Released)
            {
                End();
            }
            else if(Mouse.RightButton == MouseButtonState.Pressed)
            {
                Cancel();
            }
            else
            {
                var transform = RoomEditor.Instance.GetTransform();
                var offset = transform.Transform(Mouse.GetPosition(RoomEditor.Instance)) - StartPoint;
                var pOffset = new Point(offset.X / ROAAM_CONST.GRID_SIZE, offset.Y / ROAAM_CONST.GRID_SIZE);
                var x = Start.X;
                var y = Start.Y;
                if (Axes != 1)
                {
                    x = Start.X + pOffset.X;
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                        x = (int)x;
                    else
                        x = (int)(x * 16) / 16.0f;
                }
                if (Axes != 2)
                {
                    y = Start.Y + pOffset.Y;
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                        y = (int)y;
                    else
                        y = (int)(y * 16) / 16.0f;
                }
                Obj.Path[SelectedPoint] = new Point(x, y);
            }
        }
    }
}
