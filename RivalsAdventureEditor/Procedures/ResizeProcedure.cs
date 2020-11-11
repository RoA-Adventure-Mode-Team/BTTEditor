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
    public class ResizeProcedure : ProcedureBase
    {
        public Zone Zone { get; set; }
        public Point StartPoint { get; set; }
        public float StartX { get; set; }
        public float StartY { get; set; }
        public int StartWidth { get; set; }
        public int StartHeight { get; set; }
        public int Index { get; set; }
        public bool Finished { get; set; }

        public ResizeProcedure(Project project, Zone zone, int index) : base(project)
        {
            Zone = zone;
            Index = index;
        }

        public override void Begin()
        {
            StartX = Zone.X;
            StartY = Zone.Y;
            StartWidth = Zone.TriggerWidth;
            StartHeight = Zone.TriggerHeight;
            var transform = RoomEditor.Instance.GetTransform();
            StartPoint = transform.Transform(Mouse.GetPosition(RoomEditor.Instance));
        }

        public override void Cancel()
        {
            Zone.X = StartX;
            Zone.Y = StartY;
            Zone.TriggerWidth = StartWidth;
            Zone.TriggerHeight = StartHeight;
            base.Cancel();
        }

        public override void End()
        {
            if (Finished)
                return;
            var endX = Zone.X;
            var endY = Zone.Y;
            Zone.X = StartX;
            Zone.Y = StartY;
            var width = Zone.TriggerWidth;
            var height = Zone.TriggerHeight;
            Zone.TriggerWidth = StartWidth;
            Zone.TriggerHeight = StartHeight;
            if(width < 0)
            {
                endX += width / ROAAM_CONST.GRID_SIZE;
                width *= -1;
            }
            if(height < 0)
            {
                endY += height / ROAAM_CONST.GRID_SIZE;
                height *= -1;
            }
            Finished = true;
            var op = new ResizeOperation(Project, Zone, endX, endY, width, height);
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
                var p = ROAAM_CONST.ZONE_POINTS[Index];
                var x = (int)((p.X - 0.5) * 2);
                var y = (int)((p.Y - 0.5) * 2);
                Zone.TriggerWidth = StartWidth + (int)(pOffset.X) * x * ROAAM_CONST.GRID_SIZE;
                Zone.TriggerHeight = StartHeight + (int)(pOffset.Y) * y * ROAAM_CONST.GRID_SIZE;
                switch (x)
                {
                    case -1:
                        Zone.X = StartX + (float)pOffset.X;
                        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                            Zone.X = (int)Zone.X;
                        else
                            Zone.X = (int)(Zone.X * 16) / 16.0f;
                        goto case 1;
                    case 1:
                        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                            Zone.TriggerWidth = (Zone.TriggerWidth / 16) * 16;
                        break;
                }
                switch (y)
                {
                    case -1:
                        Zone.Y = StartY + (float)pOffset.Y;
                        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                            Zone.Y = (int)Zone.Y;
                        else
                            Zone.Y = (int)(Zone.Y * 16) / 16.0f;
                        goto case 1;
                    case 1:
                        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                            Zone.TriggerHeight = (Zone.TriggerHeight / 16) * 16;
                        break;
                }
            }
        }
    }
}
