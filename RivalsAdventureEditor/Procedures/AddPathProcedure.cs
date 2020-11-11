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
    public class AddPathProcedure : ProcedureBase
    {
        public int CurrentIndex { get; set; }
        public Target Obj { get; set; }

        bool leftDown;
        bool rightDown;

        public AddPathProcedure(Project proj, Target obj) : base(proj)
        {
            Obj = obj;
        }

        public override void Begin()
        {
            var transform = RoomEditor.Instance.GetTransform();
            var point = transform.Transform(Mouse.GetPosition(RoomEditor.Instance));
            CurrentIndex = 0;
            Obj.Path.Insert(0, new Point(point.X / ROAAM_CONST.GRID_SIZE, point.Y / ROAAM_CONST.GRID_SIZE));
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
                point = new Point(point.X / ROAAM_CONST.GRID_SIZE, point.Y / ROAAM_CONST.GRID_SIZE);
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    point = new Point((int)point.X, (int)point.Y);
                if (Obj.Path.Count <= 2)
                {
                    Obj.Path[CurrentIndex] = new Point(point.X, point.Y);
                    return;
                }

                Obj.Path.RemoveAt(CurrentIndex);
                double closest = double.MaxValue;
                var closestIndex = 0;
                for(int i = 0; i < Obj.Path.Count; i++)
                {
                    var xDist = point.X - Obj.Path[i].X;
                    var yDist = point.Y - Obj.Path[i].Y;
                    var sqrDist = xDist * xDist + yDist * yDist;
                    if(sqrDist < closest)
                    {
                        closest = sqrDist;
                        closestIndex = i;
                    }
                }
                if (Obj.Path.Count > 1)
                {
                    int next = (closestIndex + 1) % Obj.Path.Count;
                    int prev = (closestIndex - 1);
                    if (prev == -1)
                        prev = Obj.Path.Count - 1;
                    var xDist1 = point.X - Obj.Path[next].X;
                    var yDist1 = point.Y - Obj.Path[next].Y;
                    var sqrDist1 = xDist1 * xDist1 + yDist1 * yDist1;
                    var xDist2 = point.X - Obj.Path[prev].X;
                    var yDist2 = point.Y - Obj.Path[prev].Y;
                    var sqrDist2 = xDist2 * xDist2 + yDist2 * yDist2;
                    if (sqrDist2 < sqrDist1)
                    {
                        closestIndex = prev;
                    }
                }
                Obj.Path.Insert(closestIndex + 1, new Point(point.X, point.Y));
                CurrentIndex = closestIndex + 1;
            }
        }

        public override void Cancel()
        {
            Obj.Path.RemoveAt(CurrentIndex);
            base.Cancel();
        }

        public override void End()
        {
            var op = new AddPathOperation(Project, Obj, CurrentIndex, Obj.Path[CurrentIndex]);
            Obj.Path.RemoveAt(CurrentIndex);
            Project.ExecuteOp(op);
            base.End();
        }
    }
}
