using RivalsAdventureEditor.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RivalsAdventureEditor.Operations
{
    class ImportRoomsOperation : OperationBase
    {
        List<Room> Rooms { get; set; }
        List<Room> OldRooms { get; set; }
        public override string Parameter => $"null";
        public ImportRoomsOperation(Project project, List<Room> rooms) : base(project)
        {
            Rooms = rooms;
            OldRooms = new List<Room>(project.Rooms);
        }

        public override void Execute()
        {
            Project.Rooms.Clear();
            foreach (var room in Rooms)
            {
                Project.Rooms.Add(room);
            }
            if (Project.Rooms.Any())
                ApplicationSettings.Instance.ActiveRoom = Project.Rooms.First();
        }

        public override void Undo()
        {
            Project.Rooms.Clear();
            foreach (var room in OldRooms)
            {
                Project.Rooms.Add(room);
            }
            if (Project.Rooms.Any())
                ApplicationSettings.Instance.ActiveRoom = Project.Rooms.First();
        }
    }
}
