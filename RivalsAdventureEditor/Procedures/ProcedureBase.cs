using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Panels;

namespace RivalsAdventureEditor.Procedures
{
    public abstract class ProcedureBase
    {
        protected Project Project { get; set; }
        public ProcedureBase(Project project)
        {
            Project = project;
        }
        public abstract void Begin();
        public abstract void Update();
        public virtual void End()
        {
            OnEnd?.Invoke();
            ObjViewer.Instance.Update();
        }
        public virtual void Cancel()
        {
            OnEnd?.Invoke();
            ObjViewer.Instance.Update();
        }
        public event Action OnEnd;
    }
}
