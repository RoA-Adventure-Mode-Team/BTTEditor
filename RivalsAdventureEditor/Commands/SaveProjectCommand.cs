using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Panels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RivalsAdventureEditor.Commands
{
    class SaveProjectCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (parameter == null && ApplicationSettings.Instance.ActiveProject != null)
            {
                ApplicationSettings.Instance.ActiveProject.Save();
            }
            else if (parameter is Project proj)
            {
                proj.Save();
            }
        }
    }
}