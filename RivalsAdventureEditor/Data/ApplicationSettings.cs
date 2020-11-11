using Newtonsoft.Json;
using RivalsAdventureEditor.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RivalsAdventureEditor.Panels;
using System.Windows;

namespace RivalsAdventureEditor.Data
{
    class ApplicationSettings
    {
        public static ApplicationSettings Instance { get; set; }

        [JsonIgnore]
        public ObservableCollection<Project> Projects { get; } = new ObservableCollection<Project>();
        public List<string> ProjectPaths { get; set; }
        [JsonIgnore]
        public Project ActiveProject
        {
            get { return _activeProject; }
            set 
            {
                _activeProject = value;
                if (RoomEditor.Instance != null)
                {
                    RoomEditor.Instance.ResetImages();
                }
            }
        }
        Project _activeProject;
        public string ActiveProjectPath { get; set; }
        [JsonIgnore]
        public Room ActiveRoom { get; set; }
        public int ActiveRoomNum { get; set; }

        [JsonIgnore]
        public List<string> SystemLog { get; } = new List<string>();

        public static void Load()
        {
            var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RivalsAdventureEditor", "AppSettings.cfg");
            if (File.Exists(settingsPath))
            {
                using (StreamReader reader = new StreamReader(settingsPath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    Instance = (ApplicationSettings)serializer.Deserialize(reader, typeof(ApplicationSettings));
                    foreach (string path in Instance.ProjectPaths)
                    {
                        var cmd = new OpenProjectCommand();
                        cmd.Execute(path);
                    }
                    Instance.ActiveProject = Instance.Projects.FirstOrDefault(p => p.ProjectPath == Instance.ActiveProjectPath);
                    if(Instance.ActiveRoomNum != -1)
                        Instance.ActiveRoom = Instance.ActiveProject?.Rooms[Instance.ActiveRoomNum];
                }
            }
            else
            {
                Instance = new ApplicationSettings();
            }
        }

        public static void Save()
        {
            var settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RivalsAdventureEditor");
            var settingsPath = Path.Combine(settingsDir, "AppSettings.cfg");
            if (!Directory.Exists(settingsDir))
                Directory.CreateDirectory(settingsDir);
            using (StreamWriter writer = new StreamWriter(settingsPath))
            {
                Instance.ProjectPaths = Instance.Projects.Select(p => p.ProjectPath).ToList();
                Instance.ActiveProjectPath = Instance.ActiveProject?.ProjectPath;
                if (Instance.ActiveProject != null && Instance.ActiveProject.Rooms.Count > 0)
                    Instance.ActiveRoomNum = Instance.ActiveProject.Rooms.IndexOf(Instance.ActiveRoom);
                else
                    Instance.ActiveRoomNum = -1;
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(writer, Instance, typeof(ApplicationSettings));
            }
        }
    }

    public static class ROAAM_CONST
    {
        public const int GRID_SIZE = 16;
        public const int CELL_WIDTH = 163 * GRID_SIZE;
        public const int CELL_HEIGHT = 85 * GRID_SIZE;
        public static readonly Point[] ZONE_POINTS = new [] { new Point(0, 0), new Point(0.5, 0), new Point(1, 0), new Point(0, 0.5), new Point(1, 0.5), new Point(0, 1), new Point(0.5, 1), new Point(1, 1) };
    }
}
