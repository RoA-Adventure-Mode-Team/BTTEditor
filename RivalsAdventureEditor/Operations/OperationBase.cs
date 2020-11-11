using RivalsAdventureEditor.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RivalsAdventureEditor.Operations
{
    public abstract class OperationBase
    {
        protected Project Project { get; set; }
        public OperationBase(Project project)
        {
            Project = project;
        }
        public abstract void Execute();
        public abstract void Undo();

        public abstract string Parameter { get; }
    }
}
