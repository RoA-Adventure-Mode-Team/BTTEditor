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
    public class SetRespawnPointOperation : OperationBase
    {
        public Point StartPos { get; set; }
        public Point EndPos { get; set; }

        public override string Parameter => $"({EndPos.X}, {EndPos.Y})";

        public SetRespawnPointOperation(Project project, Point startPos, Point endPos) : base(project)
        {
            StartPos = startPos;
            EndPos = endPos;
        }
        public override void Execute()
        {
            Project.RespawnPoint = EndPos;
        }

        public override void Undo()
        {
            Project.RespawnPoint = StartPos;
        }
    }
}
