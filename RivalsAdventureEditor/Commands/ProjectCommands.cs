using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RivalsAdventureEditor.Commands
{
    public static class ProjectCommands
    {
        public static ICommand NewProject = new CreateProjectCommand();
        public static ICommand AddPath = new AddPathCommand();
        public static ICommand SetRespawnPoint = new SetRespawnPointCommand();
        public static ICommand ExportRoomData = new ExportRoomDataCommand();
    }
}
