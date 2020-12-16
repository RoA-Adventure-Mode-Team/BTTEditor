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
    public class AddPathCommand : CommandBase
    { 
        public override bool CanExecute(object parameter)
        {
            return RoomEditor.Instance.SelectedObj as Target != null;
        }

        public override void Execute(object parameter)
        {
            if (!(RoomEditor.Instance.SelectedObj is Target target))
                return;
            var proc = new AddPathProcedure(ApplicationSettings.Instance.ActiveProject, target);
            RoomEditor.Instance.SetActiveProcedure(proc);
        }
    }
}
