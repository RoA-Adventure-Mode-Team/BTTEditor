using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Panels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RivalsAdventureEditor.Operations
{
    public class ModifyTilesetOperation : OperationBase
    {
        Tileset Obj { get; set; }
        PropertyInfo Property { get; set; }
        object Value { get; set; }
        object OldValue { get; set; }
        public override string Parameter => $"Tileset: {Obj.SpritePath}, {Property.Name}: {Value.ToString()}";

        public ModifyTilesetOperation(Project proj, Tileset obj, string property, object oldvalue, object value) : base(proj)
        {
            Obj = obj;
            Property = obj.GetType().GetProperty(property);
            Value = value;
            OldValue = oldvalue;
        }

        public override void Execute()
        {
            if(Property != null)
            {
                if (TilesetEditor.Instance.Tileset != null)
                    TilesetEditor.Instance.Tileset.NoInvoke = true;
                Property.SetValue(Obj, Value);
                TilesetEditor.Instance.Update();
                if (TilesetEditor.Instance.Tileset != null)
                    TilesetEditor.Instance.Tileset.NoInvoke = false;
            }
        }

        public override void Undo()
        {
            if (Property != null)
            {
                if (TilesetEditor.Instance.Tileset != null)
                    TilesetEditor.Instance.Tileset.NoInvoke = true;
                Property.SetValue(Obj, OldValue);
                TilesetEditor.Instance.Update();
                if (TilesetEditor.Instance.Tileset != null)
                    TilesetEditor.Instance.Tileset.NoInvoke = false;
            }
        }
    }
}
