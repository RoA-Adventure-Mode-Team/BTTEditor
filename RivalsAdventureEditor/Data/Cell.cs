using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RivalsAdventureEditor.Data
{
    [JsonObject()]
    public class Cell : INotifyPropertyChanged
    {
        public string Name { get; set; }
        // public ObservableCollection<Obj> Objs { get; set; } = new ObservableCollection<Obj>();
        // Probably won't keep this
        public bool Open
        {
            get { return _open; }
            set
            {
                _open = value;
                OnPropertyChanged();
            }
        }
        bool _open;

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName]string property = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
