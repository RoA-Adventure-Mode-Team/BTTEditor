using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Panels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RivalsAdventureEditor.Commands
{
    class ExportRoomDataCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Project proj = null;
            if (parameter == null && ApplicationSettings.Instance.ActiveProject != null)
            {
                proj = ApplicationSettings.Instance.ActiveProject;
            }
            else if (parameter is Project p)
            {
                proj = p;
            }
            if (proj == null)
                return;

            var projectDir = Path.GetDirectoryName(proj.ProjectPath);
            var updatePath = Path.Combine(projectDir, "scripts", "update.gml");
            if (!File.Exists(updatePath))
                File.Create(updatePath).Close();
            var text = File.ReadAllText(updatePath);
            var sb = new StringBuilder();
            // Use regex to make sure it's not in a comment
            var m = Regex.Match(text, "^\\s*#region BTT_DATA", RegexOptions.Multiline);
            int idx, endIdx;
            if(m.Success)
            {
                idx = m.Index;
                // Use regex to make sure it's not in a comment
                var match = Regex.Match(text.Substring(idx, text.Length - idx), "^\\s*#endregion", RegexOptions.Multiline);
                if (match.Success)
                    endIdx = match.Index + match.Length + idx;
                else
                    endIdx = text.Length;
            }
            else
            {
                // Use regex to make sure it's not in a comment
                var match = Regex.Match(text, "^\\s*#define", RegexOptions.Multiline);
                if (match.Success)
                    idx = match.Index;
                else
                    idx = text.Length;
                endIdx = idx;
            }
            sb.Append(text.Substring(0, idx));
            sb.Append(proj.GenerateRoomData());
            sb.Append(text.Substring(endIdx, text.Length - endIdx));

            File.WriteAllText(updatePath, sb.ToString());
        }
    }
}