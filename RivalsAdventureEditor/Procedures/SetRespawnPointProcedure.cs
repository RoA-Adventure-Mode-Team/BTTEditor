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

namespace RivalsAdventureEditor.Procedures
{
    public class SetRespawnPointProcedure : ProcedureBase
    {
        public Point OldPoint { get; set; }

        bool leftDown;
        bool rightDown;

        public SetRespawnPointProcedure(Project proj) : base(proj)
        {
            OldPoint = proj.RespawnPoint;
        }

        public override void Begin()
        {
            Update();
        }

        public override void Update()
        {
            if(leftDown && Mouse.LeftButton == MouseButtonState.Released)
            {
                End();
            }
            else if(Mouse.LeftButton == MouseButtonState.Pressed)
            {
                leftDown = true;
            }
            else if((rightDown && Mouse.RightButton == MouseButtonState.Released))
            {
                Cancel();

            }
            else if(Mouse.RightButton == MouseButtonState.Pressed)
            {
                rightDown = true;
            }
            else
            {
                var transform = RoomEditor.Instance.GetTransform();
                var point = transform.Transform(Mouse.GetPosition(RoomEditor.Instance));
                point = new Point(point.X, point.Y);
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    point = new Point((int)(point.X / 16) * 16, (int)(point.Y / 16) * 16);

                Project.RespawnPoint = point;
            }
        }

        public override void Cancel()
        {
            Project.RespawnPoint = OldPoint;
            base.Cancel();
        }

        public override void End()
        {
            var op = new SetRespawnPointOperation(Project, OldPoint, Project.RespawnPoint);
            Project.ExecuteOp(op);
            base.End();
        }
    }
}
