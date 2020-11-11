using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Panels;
using RivalsAdventureEditor.Procedures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RivalsAdventureEditor.Commands
{
    public class SetRespawnPointCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var proc = new SetRespawnPointProcedure(ApplicationSettings.Instance.ActiveProject);
            RoomEditor.Instance.SetActiveProcedure(proc);
        }

        public void RaiseCanExecuteChanged()
        {
        }
    }
}
