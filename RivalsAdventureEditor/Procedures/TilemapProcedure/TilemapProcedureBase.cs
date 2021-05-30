using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;
using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Operations;
using RivalsAdventureEditor.Panels;

namespace RivalsAdventureEditor.Procedures
{
    public abstract class TilemapProcedureBase : ProcedureBase
    {
        public delegate TilemapProcedureBase CreateTmapProcFunction(Project project, Tilemap obj, int paintTile, bool leftMouse);

        public Tilemap Obj { get; set; }
        public int PaintTile { get; set; }
        public bool LeftMouse { get; set; }

        public TilemapProcedureBase(Project project, Tilemap obj, int paintTile, bool leftMouse) : base(project)
        {
            Obj = obj;
            PaintTile = paintTile;
            LeftMouse = leftMouse;
        }

        public abstract override void Begin();

        public abstract override void Update();
    }
}
