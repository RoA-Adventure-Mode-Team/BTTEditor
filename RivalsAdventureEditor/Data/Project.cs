using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using RivalsAdventureEditor.Operations;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows;

namespace RivalsAdventureEditor.Data
{
    public enum ProjectType
    {
        AdventureMode,
        BreakTheTargets,
    }
    [JsonObject()]
    public class Project : INotifyPropertyChanged
    {
        [JsonIgnore]
        public string ProjectPath { get; set; }
        public string Name { get; set; }
        [JsonIgnore]
        public ObservableCollection<Room> Rooms { get; set; } = new ObservableCollection<Room>();
        public List<Room> RoomsList
        {
            get
            {
                return Rooms.ToList();
            }
            set
            {
                Rooms.Clear();
                foreach(var val in value)
                {
                    Rooms.Add(val);
                }
            }
        }

        public Dictionary<string, Tileset> Tilesets { get; } = new Dictionary<string, Tileset>();

        public Point RespawnPoint { get; set; }

        #region Open
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
        #endregion

        #region Renaming
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
        #endregion

        #region IsActive
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
        #endregion

        #region Unsaved
        [JsonIgnore]
        public bool Unsaved
        {
            get { return _unsaved; }
            set
            {
                _unsaved = value;
                OnPropertyChanged();
            }
        }
        bool _unsaved;
        #endregion

        public ProjectType Type { get; set; }

        [JsonIgnore]
        public Stack<OperationBase> UndoStack = new Stack<OperationBase>();

        [JsonIgnore]
        public Stack<OperationBase> RedoStack = new Stack<OperationBase>();

        [JsonIgnore]
        public int LastSavedOp = 0;

        public void ExecuteOp(OperationBase op)
        {
            if (LastSavedOp > UndoStack.Count)
                LastSavedOp = -1;
            RedoStack.Clear();
            UndoStack.Push(op);
            op.Execute();
            Unsaved = true;

            ApplicationSettings.Instance.SystemLog.Add($"Execute Operation: {op.GetType().Name} with parameter {op.Parameter}.");
        }

        public void Undo()
        {
            if (UndoStack.Any())
            {
                var op = UndoStack.Pop();
                RedoStack.Push(op);
                op.Undo();
                if (UndoStack.Count == LastSavedOp)
                    Unsaved = false;
                else
                    Unsaved = true;

                ApplicationSettings.Instance.SystemLog.Add($"Undo Operation: {op.GetType().Name} with parameter {op.Parameter}.");
            }
        }

        public void Redo()
        {
            if (RedoStack.Any())
            {
                var op = RedoStack.Pop();
                UndoStack.Push(op);
                op.Execute();
                if (UndoStack.Count == LastSavedOp)
                    Unsaved = false;
                else
                    Unsaved = true;

                ApplicationSettings.Instance.SystemLog.Add($"Redo Operation: {op.GetType().Name} with parameter {op.Parameter}.");
            }
        }

        public void Save()
        {
            StringBuilder sb = new StringBuilder();
            using (JsonWriter writer = new JsonTextWriter(new StringWriter(sb)))
            {
                JsonSerializer serializer = new JsonSerializer();
                JObject jObject = JObject.FromObject(this, serializer);
                JArray rooms = new JArray();
                foreach (var room in Rooms)
                {
                    rooms.Add(room.Save(serializer));
                }
                jObject.Add("Rooms", rooms);
                jObject.WriteTo(writer);
                File.WriteAllText(ProjectPath, sb.ToString());
            }
            LastSavedOp = UndoStack.Count;
            Unsaved = false;

            ApplicationSettings.Instance.SystemLog.Add($"Saved Project: {Name}");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName]string property = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public static Project Load(StreamReader reader, JsonSerializer serializer)
        {
            var text = reader.ReadToEnd();
            using (JsonReader textReader = new JsonTextReader(new StringReader(text)))
            {
                JObject jObject = JObject.Load(textReader);
                Project proj = new Project();
                serializer.Populate(jObject.CreateReader(), proj);
                var rooms = jObject.GetValue("Rooms");
                if (rooms is JArray roomArray)
                {
                    foreach (var roomToken in rooms)
                    {
                        Room room = Room.Load(roomToken as JObject, serializer);
                        proj.Rooms.Add(room);
                    }
                }

                ApplicationSettings.Instance.SystemLog.Add($"Loaded Project: {proj.Name}");
                return proj;
            }
        }

        public string GenerateRoomData()
        {
            Dictionary<string, bool> solids = new Dictionary<string, bool>();
            StringBuilder sb = new StringBuilder();
            int targ_count = 0;
            for (int i = 0; i < Rooms.Count; i++)
            {
                var room = Rooms[i];
                sb.Append($"room_add({i + 1}, [\n");
                Dictionary<Tuple<int, int>, List<Article>> cells = new Dictionary<Tuple<int, int>, List<Article>>();
                foreach (var obj in room.Objs)
                {
                    var key = new Tuple<int, int>(obj.CellX, obj.CellY);
                    if (!cells.ContainsKey(key))
                        cells.Add(key, new List<Article>());
                    cells[key].Add(obj);
                }
                for (int j = 0; j < cells.Count; j++)
                {
                    var cell = cells.ElementAt(j);
                    sb.Append("    [ // Cell\n");
                    sb.Append($"      [{cell.Key.Item1}, {cell.Key.Item2}], // Cell Coordinates\n");
                    sb.Append("      [\n");
                    for (int n = 0; n < cell.Value.Count; n++)
                    {
                        var obj = cell.Value[n];
                        sb.Append($"        [{(int)obj.ArticleNum}, {obj.X}, {obj.Y}, {obj.Type}, {obj.Depth}, [");
                        switch (obj.ArticleNum)
                        {
                            case ArticleType.Terrain:
                                var terrain = obj as Terrain;
                                if (!string.IsNullOrEmpty(terrain.Sprite))
                                    sb.Append($"sprite_get(\"{terrain.Sprite}\"), ");
                                else
                                    sb.Append("0, ");
                                sb.Append($"{terrain.AnimationSpeed}, 0, {terrain.StaticInt}, 0, 0, 0, 0");
                                // Solid object
                                if (terrain.Type == 2)
                                {
                                    if (!solids.ContainsKey(terrain.Sprite))
                                        solids.Add(terrain.Sprite, true);
                                }
                                break;
                            case ArticleType.Zone:
                                var zone = obj as Zone;
                                sb.Append($"{zone.EventID}, {zone.ActiveScene}, {zone.TriggerObjType}, {zone.TriggerPlayer}, {(int)zone.TriggerShape}, {zone.TriggerWidth}, {zone.TriggerHeight}, {zone.TriggerNegative}");
                                break;
                            case ArticleType.Target:
                                var targ = obj as Target;
                                StringBuilder moveVel = new StringBuilder();
                                StringBuilder path = new StringBuilder();
                                if (targ.MoveVel.Count == 0)
                                    moveVel.Append("0");
                                else if (targ.MoveVel.Count == 1)
                                    moveVel.Append(targ.MoveVel[0]);
                                else
                                {
                                    moveVel.Append("[");
                                    for (int k = 0; k < targ.MoveVel.Count; k++)
                                    {
                                        moveVel.Append(targ.MoveVel[k]);
                                        if (k != targ.MoveVel.Count - 1)
                                            moveVel.Append(", ");
                                    }
                                    moveVel.Append("]");
                                }
                                if (targ.Path.Count == 0)
                                    path.Append("0");
                                else
                                {
                                    path.Append("[");
                                    for (int k = 0; k < targ.Path.Count; k++)
                                    {
                                        var offsetX = targ.CellX * ROAAM_CONST.CELL_WIDTH / ROAAM_CONST.GRID_SIZE;
                                        var offsetY = targ.CellY * ROAAM_CONST.CELL_HEIGHT / ROAAM_CONST.GRID_SIZE;
                                        path.Append($"[{targ.Path[k].X + offsetX}, {targ.Path[k].Y - offsetY}]");
                                        if (k != targ.Path.Count - 1)
                                            path.Append(", ");
                                    }
                                    path.Append("]");
                                }
                                sb.Append($"{targ_count++}, 0, {moveVel.ToString()}, {path.ToString()}, ");
                                if (!string.IsNullOrEmpty(targ.SpriteOverride) && targ.SpriteOverride != "roaam_target")
                                    sb.Append($"sprite_get(\"{targ.SpriteOverride}\"), ");
                                else
                                    sb.Append("0, ");
                                if (!string.IsNullOrEmpty(targ.DestroySprite))
                                    sb.Append($"sprite_get(\"{targ.DestroySprite}\"), ");
                                else
                                    sb.Append("0, ");
                                sb.Append("0, 0");
                                break;
                        }
                        sb.Append($"], [{obj.TryGetExtraArg<int>(ExtraArgs.SpawnFlag, 0)}]");
                        if (n == cell.Value.Count - 1)
                            sb.Append("]\n");
                        else
                            sb.Append("],\n");
                    }
                    sb.Append("      ]\n");
                    if (j == cells.Count - 1)
                        sb.Append("    ]");
                    else
                        sb.Append("    ],\n");
                }

                sb.Append("]);\n");
            }

            StringBuilder solidString = new StringBuilder();
            foreach (var item in solids)
            {
                solidString.Append(string.Format(change_coll_mask, item.Key));
            }

            return String.Format(formatText, Name, $"[[{RespawnPoint.X}, {RespawnPoint.Y}], [0, 0], 1]", solidString.ToString(), sb.ToString()); ;
        }


        #region STRING_CONSTANTS
        public const string change_coll_mask = "    sprite_change_collision_mask(\"{0}\", true, 1, 0, 0, 0, 0, 0 );\n";

        public const string formatText =
@"#region BTT_DATA
/// WARNING: Don't delete or modify any code in this region
/// Seriously, the BTT tool is gonna replace all the data in this region when you run it
/// If you need additional #defines, put them after '#endregion'
/// Just hit the little arrow on the left of GMEdit so you don't have to see this code

if get_btt_data {{ //Get data for Break The Targets
	course_name = ""{0}"";
	//Set the spawn properties
	respawn_point = {1};
	//Set the collision of the solid sprites to precise
{2}
    {3}
}}

#define room_add(_room_id,room_data) //Adds a new room to the scene
with obj_stage_article if num == 5 {{
	var _room_id_ind = array_find_index(array_room_ID,_room_id);
	if _room_id_ind == - 1 {{
	    if debug print_debug(""[RM] Adding... ""+string(_room_id));
	    array_push(array_room_data, room_data);
        array_push(array_room_ID, _room_id);
    }} else {{
	    array_room_data[_room_id_ind] = room_data;
	    array_room_ID[_room_id_ind] = _room_id;
	}}
}}

#endregion
";

        #endregion
    }
}
