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
    public class ModifyPropertyOperation : OperationBase
    {
        Obj Obj { get; set; }
        PropertyInfo Property { get; set; }
        object Value { get; set; }
        object OldValue { get; set; }
        public override string Parameter => $"{Obj.Article.ToString()}({Obj.Name}), {Property.Name}: {Value.ToString()}";

        public ModifyPropertyOperation(Project proj, Obj obj, string property, object oldvalue, object value) : base(proj)
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
                Property.SetValue(Obj, Value);
                RoomEditor.Instance.SelectedObj = Obj;
                ObjViewer.Instance.Update();
            }
        }

        public override void Undo()
        {
            if (Property != null)
            {
                Property.SetValue(Obj, OldValue);
                RoomEditor.Instance.SelectedObj = Obj;
                ObjViewer.Instance.Update();
            }
        }
    }
}
