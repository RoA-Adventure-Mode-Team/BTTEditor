using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Panels;
using System;
using System.Collections.Generic;
using System.Text;

namespace RivalsAdventureEditor.Operations
{
    class AddTilesetOperation : OperationBase
    {
        Tileset Obj { get; set; }
        string Name { get; set; }
        public override string Parameter => $"Create Tileset with Name: {Name}";

        public AddTilesetOperation(Project proj, string name) : base(proj)
        {
            Obj = new Tileset { Name = name };
            Name = name;
        }

        public override void Execute()
        {
            Project.Tilesets.Add(Name, Obj);
            TilesetEditor.Instance.Update();
        }

        public override void Undo()
        {
            Project.Tilesets.Remove(Name);
            TilesetEditor.Instance.Update();
        }
    }
}
