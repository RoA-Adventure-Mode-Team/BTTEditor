using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RivalsAdventureEditor.Data
{
    [JsonObject()]
    public class Room : INotifyPropertyChanged
    {
        public string Name { get; set; }
        [JsonIgnore]
        public string Filename { get; set; }
        [JsonIgnore]
        public ObservableCollection<Article> Objs { get; set; } = new ObservableCollection<Article>();
        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }
        bool _isActive;
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

        public JObject Save(JsonSerializer serializer)
        {
            JObject jObject = JObject.FromObject(this, serializer);
            JArray objs = JArray.FromObject(Objs);
            jObject.Add("Objs", objs);
            return jObject;
        }

        public static Room Load(JObject json, JsonSerializer serializer)
        {
            var room = new Room();
            serializer.Populate(json.CreateReader(), room);
            foreach(JObject obj in json.GetValue("Objs") as JArray)
            {
                Article article;
                ArticleType type = (ArticleType)obj.Value<int>("Article");
                switch(type)
                {
                    case ArticleType.Terrain:
                        article = new Terrain();
                        break;
                    case ArticleType.Zone:
                        article = new Zone();
                        break;
                    case ArticleType.Target:
                        article = new Target();
                        break;
                    case ArticleType.Tilemap:
                        article = new Tilemap();
                        break;
                    default:
                        article = new Article();
                        break;
                }
                serializer.Populate(obj.CreateReader(), article);
                room.Objs.Add(article);
            }
            return room;
        }
    }
}
