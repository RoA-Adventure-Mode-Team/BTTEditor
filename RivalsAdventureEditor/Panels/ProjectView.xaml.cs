using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.IO;
using RivalsAdventureEditor.Operations;
using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Windows;
using Newtonsoft.Json;

namespace RivalsAdventureEditor.Panels
{
    /// <summary>
    /// Interaction logic for ProjectView.xaml
    /// </summary>
    public partial class ProjectView : ScrollViewer
    {
        public static ProjectView Instance { get; set; } = null;

        public ProjectView()
        {
            if (Instance == null)
                Instance = this;
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
        }

        public static void LoadProject(string filename)
        {
            if(ApplicationSettings.Instance?.Projects.Any(p => p.ProjectPath == filename) == true)
            {
                SetActiveProject(filename);
            }
            else if(File.Exists(filename))
            {
                using (StreamReader reader = new StreamReader(filename))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    Project proj = Project.Load(reader, serializer); 
                    proj.ProjectPath = filename;
                    ApplicationSettings.Instance.Projects.Add(proj);
                    SetActiveProject(proj);
                }
            }
        }

        public static void NewProject(string filename)
        {
            var ext = Path.GetExtension(filename);
            var proj = new Project() { ProjectPath = filename, Name = Path.GetFileNameWithoutExtension(filename), Type = (ext == ".btts" ? ProjectType.BreakTheTargets : ProjectType.AdventureMode) };
            ApplicationSettings.Instance.Projects.Add(proj);
            using (StreamWriter writer = new StreamWriter(filename))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(writer, proj, typeof(Project));
            }
        }

        public static void SetActiveProject(string filename)
        {
            var proj = ApplicationSettings.Instance.Projects.FirstOrDefault(p => p.ProjectPath == filename);
            SetActiveProject(proj);
        }
        public static void SetActiveProject(Project proj)
        {
            if (proj != null)
            {
                foreach (var project in ApplicationSettings.Instance.Projects)
                {
                    project.IsActive = project == proj;
                }
                ApplicationSettings.Instance.ActiveProject = proj;
                if (ObjectHierarchy.Instance != null)
                {
                    ObjectHierarchy.Instance.Rooms = ApplicationSettings.Instance.ActiveProject.Rooms;
                    ObjectHierarchy.Instance.SetActiveRoom(ApplicationSettings.Instance.ActiveProject.Rooms.Any() ? ApplicationSettings.Instance.ActiveProject.Rooms[0] : null);
                }
            }
        }

        private void CanCreateRoom(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ApplicationSettings.Instance.ActiveProject != null || e.Parameter != null;
        }

        private void CreateRoom(object sender, ExecutedRoutedEventArgs e)
        {
            var proj = (e.Parameter as Project) ?? ApplicationSettings.Instance.ActiveProject;
            var op = new CreateRoomOperation(proj);
            proj.ExecuteOp(op);
            SetActiveProject(proj);
        }

        private void ImportRooms(object sender, RoutedEventArgs e)
        {
            var dlg = new ImportRoomDataDialogue() { Project = ((FrameworkElement)sender).DataContext as Project };
            dlg.ShowDialog();
            RoomEditor.Instance.Reload();
        }
    }
}
