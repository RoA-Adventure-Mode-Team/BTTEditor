﻿using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Panels;
using RivalsAdventureEditor.Procedures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RivalsAdventureEditor.Commands
{
    public class SetRespawnPointCommand : CommandBase
    {
        public override bool CanExecute(object parameter)
        {
            return ApplicationSettings.Instance.ActiveProject != null;
        }

        public override void Execute(object parameter)
        {
            if (ApplicationSettings.Instance.ActiveProject != null)
            {
                var proc = new SetRespawnPointProcedure(ApplicationSettings.Instance.ActiveProject);
                RoomEditor.Instance.SetActiveProcedure(proc);
            }
        }
    }
}
