using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using RivalsAdventureEditor.Panels;

namespace RivalsAdventureEditor.Commands
{
    class CreateProjectCommand : CommandBase
    {
        public override bool CanExecute(object parameter)
        {
            return true;
        }

        public override void Execute(object parameter)
        {
            var dialog = new SaveFileDialog();
            dialog.Filter = "Rivals Adventure Mode Projects | *.aprj|Rivals Break The Targets Stage | *.btts";
            dialog.DefaultExt = ".aprj";
            dialog.FileName = "Project";
            dialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RivalsofAether", "workshop");
            var result = dialog.ShowDialog();
            if (result == true)
            {
                ProjectView.NewProject(dialog.FileName);
            }
        }
    }
}
