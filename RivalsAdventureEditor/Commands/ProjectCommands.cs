using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RivalsAdventureEditor.Commands
{
    public static class ProjectCommands
    {
        public static CommandBase NewProject = new CreateProjectCommand();
        public static CommandBase AddPath = new AddPathCommand();
        public static CommandBase SetRespawnPoint = new SetRespawnPointCommand();
        public static CommandBase ExportRoomData = new ExportRoomDataCommand();
        public static CommandBase GenerateRoomData = new GenerateRoomDataCommand();
        public static CommandBase CreateArticle = new CreateArticleCommand();

        private static PropertyInfo [] properties;

        static ProjectCommands()
        {
            properties = typeof(ProjectCommands).GetProperties(BindingFlags.Static | BindingFlags.Public);
        }

        public static void UpdateCanExecute()
        {
            foreach(var prop in properties)
            {
                if(prop.GetValue(null) is CommandBase command)
                {
                    command.UpdateCanExecute();
                }
            }
        }
    }
}
