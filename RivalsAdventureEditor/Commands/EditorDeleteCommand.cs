using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Operations;
using RivalsAdventureEditor.Panels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RivalsAdventureEditor.Commands
{
    class EditorDeleteCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return RoomEditor.Instance.SelectedObj != null;
        }

        public void Execute(object parameter)
        {
            if (RoomEditor.Instance.SelectedObj == null)
                return;
            if (RoomEditor.Instance.SelectedPath == -1)
            {
                var op = new DeleteArticleOperation(ApplicationSettings.Instance.ActiveProject, RoomEditor.Instance.SelectedObj);
                ApplicationSettings.Instance.ActiveProject.ExecuteOp(op);
            }
            else
            {
                var op = new DeletePathOperation(ApplicationSettings.Instance.ActiveProject, RoomEditor.Instance.SelectedObj as Target, RoomEditor.Instance.SelectedPath);
                ApplicationSettings.Instance.ActiveProject.ExecuteOp(op);
            }
        }
    }
}
