using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RivalsAdventureEditor.Data
{
    [JsonObject()]
    class Terrain : Obj
    {
        public Terrain()
        {
            Type = 2;
        }

        [ArticleProperty(0, ShowInPanel = true)]
        public override string Sprite { get => base.Sprite; set => base.Sprite = value; }
        [ArticleProperty(1, ShowInPanel = true)]
        public float AnimationSpeed { get; set; }
        [ArticleProperty(3)]
        [JsonIgnore]
        public int StaticInt
        {
            get { return Static ? 1 : 0; }
            set { Static = value != 0; }
        }
        [ArticleProperty(-1, ShowInPanel = true)]
        public bool Static { get; set; }
    }
}
