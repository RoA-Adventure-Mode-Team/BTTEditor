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
    public class TranslateProcedure : ProcedureBase
    {
        public Obj Obj { get; set; }
        public Point StartPoint { get; set; }
        public float StartX { get; set; }
        public float StartY { get; set; }
        public int Axes { get; set; }
        public bool Finished { get; set; }

        public TranslateProcedure(Project project, Obj obj, int axes) : base(project)
        {
            Obj = obj;
            Axes = axes;
        }

        public override void Begin()
        {
            StartX = Obj.X;
            StartY = Obj.Y;
            var transform = RoomEditor.Instance.GetTransform();
            StartPoint = transform.Transform(Mouse.GetPosition(RoomEditor.Instance));
        }

        public override void Cancel()
        {
            Obj.X = StartX;
            Obj.Y = StartY;
            base.Cancel();
        }

        public override void End()
        {
            if (Finished)
                return;
            var endX = Obj.X;
            var endY = Obj.Y;
            Obj.X = StartX;
            Obj.Y = StartY;
            Finished = true;
            var op = new TranslateOperation(Project, Obj, endX, endY);
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
                if (Axes != 1)
                {
                    Obj.X = StartX + (float)pOffset.X;
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                        Obj.X = (int)Obj.X;
                    else
                        Obj.X = (int)(Obj.X * 16) / 16.0f;
                }
                if (Axes != 2)
                {
                    Obj.Y = StartY + (float)pOffset.Y;
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                        Obj.Y = (int)Obj.Y;
                    else
                        Obj.Y = (int)(Obj.Y * 16) / 16.0f;
                }
            }
        }
    }
}
