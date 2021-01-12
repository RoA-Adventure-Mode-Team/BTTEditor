using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Panels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RivalsAdventureEditor.Operations
{
    public class TranslateOperation : OperationBase
    {
        public Article Obj { get; set; }
        public float StartX { get; set; }
        public float StartY { get; set; }
        public float EndX { get; set; }
        public float EndY { get; set; }
        public int OldCellX { get; set; }
        public int OldCellY { get; set; }
        public int CellX { get; set; }
        public int CellY { get; set; }

        public override string Parameter => $"{Obj.ArticleNum.ToString()}({Obj.Name}), Cell ({CellX}, {CellY}), Pos ({EndX}, {EndY})";

        public TranslateOperation(Project project, Article obj, float x, float y) : base(project)
        {
            StartX = obj.X;
            StartY = obj.Y;
            OldCellX = obj.CellX;
            OldCellY = obj.CellY;
            EndX = x;
            EndY = y;

            var offX = ((int)EndX / (ROAAM_CONST.CELL_WIDTH / ROAAM_CONST.GRID_SIZE));
            var offY = (int)EndY / (ROAAM_CONST.CELL_HEIGHT / ROAAM_CONST.GRID_SIZE);

            CellX = OldCellX - offX;
            CellY = OldCellY + offY;
            EndX -= offX * (ROAAM_CONST.CELL_WIDTH / ROAAM_CONST.GRID_SIZE);
            EndY -= offY * (ROAAM_CONST.CELL_HEIGHT / ROAAM_CONST.GRID_SIZE);

            Obj = obj;
        }
        public override void Execute()
        {
            if (Obj != null)
            {
                Obj.X = EndX;
                Obj.Y = EndY;
                Obj.CellX = CellX;
                Obj.CellY = CellY;

                RoomEditor.Instance.SelectedObj = Obj;
            }
        }

        public override void Undo()
        {
            if(Obj != null)
            {
                Obj.X = StartX;
                Obj.Y = StartY;
                Obj.CellX = OldCellX;
                Obj.CellY = OldCellY;

                RoomEditor.Instance.SelectedObj = Obj;
            }
        }
    }
}
