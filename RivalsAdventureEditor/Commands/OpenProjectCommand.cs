using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using RivalsAdventureEditor.Panels;
using RivalsAdventureEditor.Data;

namespace RivalsAdventureEditor.Commands
{
    class OpenProjectCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (parameter == null)
            {
                var dialog = new OpenFileDialog();
                dialog.Filter = "All Project Formats | *.aprj;*.btts";
                dialog.DefaultExt = ".aprj";
                dialog.FileName = "Project";
                dialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RivalsofAether", "workshop");
                var result = dialog.ShowDialog();
                if (result == true)
                {
                    ProjectView.LoadProject(dialog.FileName);
                }
            }
            else if(parameter is Project proj)
            {
                ProjectView.SetActiveProject(proj);
            }
            else if(parameter is string projPath)
            {
                ProjectView.LoadProject(projPath);
            }
        }
    }
}
