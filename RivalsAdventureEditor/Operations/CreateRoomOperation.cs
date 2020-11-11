using RivalsAdventureEditor.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RivalsAdventureEditor.Operations
{
    class CreateRoomOperation : OperationBase
    {
        Room Room { get; set; }
        public override string Parameter => Room.Name;
        public CreateRoomOperation(Project project) : base(project)
        {
            Room = new Room() { Name = "NewRoom" };
        }

        public override void Execute()
        {
            Project.Rooms.Add(Room);
        }

        public override void Undo()
        {
            Project.Rooms.Remove(Room);
        }
    }
}
