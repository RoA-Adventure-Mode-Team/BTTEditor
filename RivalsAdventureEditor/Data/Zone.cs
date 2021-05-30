using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RivalsAdventureEditor.Data
{
    [JsonObject()]
    public class Zone : Article
    {
        [ArticleProperty(0)]
        public int EventID { get; set; } = 4;
        [ArticleProperty(1)]
        public int ActiveScene { get; set; }
        [ArticleProperty(2)]
        public int TriggerObjType { get; set; }
        [ArticleProperty(3)]
        public int TriggerPlayer { get; set; }
        [ArticleProperty(4, ShowInPanel = true)]
        public Shape TriggerShape { get; set; }
        [ArticleProperty(5, ShowInPanel = true)]
        public int TriggerWidth { get; set; }
        [ArticleProperty(6, ShowInPanel = true)]
        public int TriggerHeight { get; set; }
        [ArticleProperty(7)]
        public int TriggerNegative { get; set; }

        public Zone()
        {
            Sprite = "roaam_zone";
        }

        public override bool ContainsPoint(Point point)
        {
            var box = new Rect(RealPoint, new Size(TriggerWidth, TriggerHeight));
            return box.Contains(point);
        }
    }
}
