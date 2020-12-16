using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Panels;
using RivalsAdventureEditor.Procedures;
using RivalsAdventureEditor.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RivalsAdventureEditor.Commands
{
    public class CreateArticleCommand : CommandBase
    {
        public override bool CanExecute(object parameter)
        {
            return ApplicationSettings.Instance.ActiveProject != null;
        }

        public override void Execute(object parameter)
        {
            if (ApplicationSettings.Instance.ActiveProject != null)
            {
                var dlg = new CreateArticleDialog
                {
                    Owner = App.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                dlg.ShowDialog();
            }
        }
    }
}
