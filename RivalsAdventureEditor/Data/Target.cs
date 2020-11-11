using Newtonsoft.Json;
using RivalsAdventureEditor.Panels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RivalsAdventureEditor.Data
{
    [JsonObject()]
    public class Target : Obj
    {
        [ArticleProperty(0)]
        public int TargID { get; set; }
        [ArticleProperty(1)]
        public int EventID { get; set; }
        [ArticleProperty(2, ShowInPanel = true)]
        public List<float> MoveVel { get; set; } = new List<float>();
        [ArticleProperty(3, ShowInPanel = true)]
        public List<Point> Path { get; set; } = new List<Point>();
        [ArticleProperty(4, ShowInPanel = true)]
        public string SpriteOverride
        {
            get { return Sprite; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    Sprite = value;
            }
        }
        [ArticleProperty(5, ShowInPanel = true)]
        public string DestroySprite { get; set; }

        public Target()
        {
            Sprite = "roaam_target";
        }
    }
}
