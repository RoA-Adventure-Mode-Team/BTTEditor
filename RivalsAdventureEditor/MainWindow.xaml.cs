using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Windows;
using RivalsAdventureEditor.Commands;
using RivalsAdventureEditor.Panels;
using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace RivalsAdventureEditor
{
    public partial class MainWindow : Window
    {
        public string DefaultLayout { get; set; }

        public MainWindow()
        {
            ApplicationSettings.Load();
            InitializeComponent();
        }

        ~MainWindow()
        {
        }

        private void OpenProject(object sender, ExecutedRoutedEventArgs e)
        {
            var cmd = new OpenProjectCommand();
            cmd.Execute(e.Parameter);
        }

        private void CanUndo(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ApplicationSettings.Instance.ActiveProject?.UndoStack.Any() == true;
        }

        private void UndoOp(object sender, ExecutedRoutedEventArgs e)
        {
            ApplicationSettings.Instance.ActiveProject.Undo();
        }

        private void CanRedo(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ApplicationSettings.Instance.ActiveProject?.RedoStack.Any() == true;
        }

        private void RedoOp(object sender, ExecutedRoutedEventArgs e)
        {
            ApplicationSettings.Instance.ActiveProject.Redo();
        }

        private void SaveProj(object sender, ExecutedRoutedEventArgs e)
        {
            var cmd = new SaveProjectCommand();
            cmd.Execute(e.Parameter);
        }
        private void DeleteItem(object sender, ExecutedRoutedEventArgs e)
        {
            var cmd = new EditorDeleteCommand();
            if (cmd.CanExecute(null))
                cmd.Execute(null);
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            var serializer = new XmlLayoutSerializer(dockingManager);
            StringBuilder sb = new StringBuilder();
            serializer.Serialize(new StringWriter(sb));
            DefaultLayout = sb.ToString();
            var settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RivalsAdventureEditor");
            var settingsPath = Path.Combine(settingsDir, "PanelLayout.xml");
            if(File.Exists(settingsPath))
                serializer.Deserialize(settingsPath);
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var project in ApplicationSettings.Instance.Projects)
            {
                if (project.Unsaved)
                {
                    var dlg = new SaveConfirmationDialog();
                    dlg.ShowDialog();
                    var res = dlg.Result;
                    if (res == true)
                    {
                        foreach (var proj in ApplicationSettings.Instance.Projects)
                            proj.Save();
                    }
                    else if (res == null)
                    {
                        e.Cancel = true;
                    }
                    break;
                }
            }

            ApplicationSettings.Save();
            var serializer = new XmlLayoutSerializer(dockingManager);
            var settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RivalsAdventureEditor");
            var settingsPath = Path.Combine(settingsDir, "PanelLayout.xml");
            serializer.Serialize(settingsPath);
        }

        private void RefreshMenuItem(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(sender is MenuItem item && item.Command != null)
            {
                (item.Command as AddPathCommand).UpdateCanExecute();
            }
        }

        private void CopyItem(object sender, ExecutedRoutedEventArgs e)
        {
            if(RoomEditor.Instance.SelectedObj != null)
            {
                var cmd = new CopyCommand();
                cmd.Execute(RoomEditor.Instance.SelectedObj);
            }
        }

        private void PasteItem(object sender, ExecutedRoutedEventArgs e)
        {
            var cmd = new PasteCommand();
            cmd.Execute(null);
        }

        private void TestCrashDialog(object sender, RoutedEventArgs e)
        {
            (null as Article).ToString();
        }

        private void CheckWindowOpen(object sender, RoutedEventArgs e)
        {
            var menu = sender as MenuItem;
            var content = dockingManager.Layout.Descendents().OfType<LayoutAnchorable>().FirstOrDefault(lc => lc.ContentId == menu.Tag as string);
            if (content != null)
                menu.IsChecked = !content.IsHidden;
        }

        private void ToggleWindowOpen(object sender, RoutedEventArgs e)
        {
            var menu = sender as MenuItem;
            var content = dockingManager.Layout.Descendents().OfType<LayoutAnchorable>().FirstOrDefault(lc => lc.ContentId == menu.Tag as string);
            if (content != null)
            {
                if (content.IsHidden)
                    content.Show();
                else
                    content.Hide();
            }
        }

        private void Refresh(object sender, ExecutedRoutedEventArgs e)
        {
            var cmd = new RefreshCommand();
            cmd.Execute(null);
        }

        private void ResetLayout(object sender, RoutedEventArgs e)
        {
            var serializer = new XmlLayoutSerializer(dockingManager);
            serializer.Deserialize(new StringReader(DefaultLayout));
        }
    }
}