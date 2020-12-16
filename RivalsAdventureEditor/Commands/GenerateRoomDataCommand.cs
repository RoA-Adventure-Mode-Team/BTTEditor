using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Panels;
using RivalsAdventureEditor.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RivalsAdventureEditor.Commands
{
    class GenerateRoomDataCommand : CommandBase
    {
        public override bool CanExecute(object parameter)
        {
            return ApplicationSettings.Instance.ActiveProject != null;
        }

        public override void Execute(object parameter)
        {
            if (ApplicationSettings.Instance.ActiveProject == null)
                return;
            var dlg = new ExportRoomDataDialogue() { Project = ApplicationSettings.Instance.ActiveProject };
            dlg.ShowDialog();
        }
    }
}