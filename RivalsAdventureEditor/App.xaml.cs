using RivalsAdventureEditor.Windows;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RivalsAdventureEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                var dlg = new CrashDialog();
                dlg.Exception = e.Exception;
                dlg.SaveLog();
                dlg.ShowDialog();
            }
            catch
            {
                foreach(var project in Data.ApplicationSettings.Instance.Projects)
                {
                    project.ProjectPath = project.ProjectPath + ".autosave";
                    project.Save();
                }
            }
        }
    }
}
