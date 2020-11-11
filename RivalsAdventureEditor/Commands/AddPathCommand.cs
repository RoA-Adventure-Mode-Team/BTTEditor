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
    public class AddPathCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return RoomEditor.Instance.SelectedObj as Target != null;
        }

        public void Execute(object parameter)
        {
            if (!(RoomEditor.Instance.SelectedObj is Target target))
                return;
            var proc = new AddPathProcedure(ApplicationSettings.Instance.ActiveProject, target);
            RoomEditor.Instance.SetActiveProcedure(proc);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
