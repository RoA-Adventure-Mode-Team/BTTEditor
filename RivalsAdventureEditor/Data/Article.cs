using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Windows;
using RivalsAdventureEditor.Panels;

namespace RivalsAdventureEditor.Data
{
    public enum ExtraArgs
    {
        SpawnFlag
    }

    public enum ArticleType
    {
        Terrain = 1,
        Empty = 2,
        SceneManager = 3,
        Zone = 4,
        RoomManager = 5,
        Enemy = 6,
        CameraController = 7,
        RoomTransition = 8,
        Checkpoint = 9,
        Target = 10,
        Tilemap = 30
    }
    public enum Shape
    {
        Rectangle = 0,
        Circle = 1,
        Sprite = 2
    }

    [JsonObject()]
    public class Article : INotifyPropertyChanged
    {
        public string Name { get; set; }
        [JsonProperty("Article")]
        public ArticleType ArticleNum { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public int Type { get; set; }
        public int Depth { get; set; }
        [JsonIgnore]
        public object[] Args { get; set; } = new object[8];
        [JsonIgnore]
        public List<object> ExtraArgs { get; } = new List<object>();
        public int CellX { get; set; }
        public int CellY { get; set; }
        public virtual string Sprite { get; set; } = "";

        [JsonIgnore]
        public System.Windows.Point RealPoint
        {
            get { return new System.Windows.Point(X * ROAAM_CONST.GRID_SIZE + CellX * ROAAM_CONST.CELL_WIDTH, Y * ROAAM_CONST.GRID_SIZE + CellY * ROAAM_CONST.CELL_HEIGHT); }
        }

        [JsonIgnore]
        public bool Renaming
        {
            get { return _renaming; }
            set
            {
                _renaming = value;
                OnPropertyChanged();
            }
        }
        bool _renaming;

        public T TryGetExtraArg<T>(ExtraArgs arg, T defaultVal)
        {
            var index = (int)arg;
            if (ExtraArgs.Count > index && ExtraArgs[index] is T)
                return (T)ExtraArgs[index];
            return defaultVal;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName]string property = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public virtual bool ContainsPoint(Point point)
        {
            return SpriteContainsPoint(new Point(1, 1), point);
        }

        protected bool SpriteContainsPoint(Point scale, Point point)
        {
            var spr = RoomEditor.Instance.LoadedImages[Sprite];
            if (!spr.exists)
                spr = RoomEditor.Instance.LoadedImages["roaam_empty"];
            var offset = RealPoint - new Point(spr.offset.X, spr.offset.Y);
            var box = new Rect(new Point(offset.X, offset.Y), new Size((spr.image?.Width ?? 0) * scale.X, (spr.image?.Height ?? 0) * scale.Y));
            if (box.Contains(point))
            {
                if (spr.image == null)
                    return true;
                else
                {
                    var sprPoint = point - offset;
                    sprPoint = new Point((sprPoint.X * 1 / scale.X), (sprPoint.Y * 1 / scale.Y));
                    var px = spr.image.GetPixel((int)sprPoint.X, (int)sprPoint.Y);
                    if (px.A > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ArticlePropertyAttribute : Attribute
    {
        public ArticlePropertyAttribute(int argIndex)
        {
            ArgIndex = argIndex;
        }
        public bool ShowInPanel { get; set; }
        public int ArgIndex { get; set; }
    }
}
