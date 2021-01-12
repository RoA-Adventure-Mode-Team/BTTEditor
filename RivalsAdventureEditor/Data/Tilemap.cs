using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace RivalsAdventureEditor.Data
{
    public class Tilemap : Article
    {
        public string TilesetID { get; set; } = "";
        [JsonIgnore]
        public Tileset Tileset
        {
            get
            {
                if(!string.IsNullOrEmpty(TilesetID))
                {
                    return (ApplicationSettings.Instance.ActiveProject?.Tilesets.ContainsKey(TilesetID) ?? false) ? ApplicationSettings.Instance.ActiveProject.Tilesets[TilesetID] : null;
                }
                return null;
            }
        }
        public TilegridArray Tilegrid { get; private set; } = new TilegridArray();
    }
}
