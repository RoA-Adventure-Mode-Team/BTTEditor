using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using RivalsAdventureEditor.Data;
using RivalsAdventureEditor.Operations;

namespace RivalsAdventureEditor.Windows
{
    /// <summary>
    /// Interaction logic for ImportRoomDataDialogue.xaml
    /// </summary>
    public partial class ImportRoomDataDialogue : Window
    {
        public Project Project { get; set; }

        public ImportRoomDataDialogue()
        {
            InitializeComponent();
        }

        private void ProcessText(object sender, RoutedEventArgs e)
        {
            try
            {
                var rooms = ParseRooms(InputBox.Text, out string error, Project.Type);
                var op = new ImportRoomsOperation(Project, rooms);
                Project.ExecuteOp(op);
                Close();
            }
            catch (Exception)
            {
                ErrorMsg.Visibility = Visibility.Visible;
            }
        }

        private List<Room> ParseRooms(string input, out string error, ProjectType project)
        {
            error = "";
            StringBuilder sb = new StringBuilder();
            foreach (var line in input.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                sb.Append(line.Split(new string[] { "//" }, StringSplitOptions.RemoveEmptyEntries)[0]);
                sb.Append(" ");
            }
            var text = sb.ToString();
            var start = text.IndexOf("room_add");
            if(start == -1)
            {
                error = "Error: No room data found in pasted text";
            }
            var room_text = new List<string>();
            while (start != -1)
            {
                var end = text.IndexOf(';', start);
                if (end == -1)
                {
                    end = text.Length;
                }
                room_text.Add(text.Substring(start, end - start));
                start = text.IndexOf("room_add", end);
            }
            var rooms = new List<Room>();
            for (int i = 0; i < room_text.Count; i++)
            {
                var rt = room_text[i];
                var data = ExtractData(rt);
                var room = new Room { Name = "Room" + i };
                rooms.Add(room);
                for(int j = 0; j < data.Count; j++)
                {
                    RoomObj cell_data = (RoomObj)data[j];
                    RoomObj coords = (RoomObj)cell_data[0];
                    RoomObj objs_array = (RoomObj)cell_data[1];
                    for(int k = 0; k < objs_array.Count; k++)
                    {
                        RoomObj obj_data = (RoomObj)objs_array[k];
                        Article obj;
                        switch((ArticleType)obj_data[0])
                        {
                            case ArticleType.Terrain:
                                obj = new Terrain();
                                break;
                            case ArticleType.Zone:
                                obj = new Zone();
                                break;
                            case ArticleType.Target:
                                obj = new Target();
                                break;
                            default:
                                obj = new Article();
                                break;
                        }
                        obj.ArticleNum = (ArticleType)obj_data[0];
                        obj.Name = string.IsNullOrEmpty(obj_data.name) ? obj.ArticleNum.ToString() : obj_data.name;
                        if (obj_data[1] is float)
                            obj.X = (float)obj_data[1];
                        else
                            obj.X = (int)obj_data[1];
                        if (obj_data[2] is float)
                            obj.Y = (float)obj_data[2];
                        else
                            obj.Y = (int)obj_data[2];
                        obj.Type = (int)obj_data[3];
                        obj.Depth = (int)obj_data[4];

                        RoomObj arg_data = (RoomObj)obj_data[5];
                        for (int n = 0; n < 8; n++)
                        {
                            if (arg_data[n] is string)
                            {
                                var match = Regex.Match(arg_data[n] as string, "sprite_get\\s*\\(\\s*\"([^\"]+)\"\\s*\\)");
                                if (match.Success)
                                    obj.Args[n] = match.Groups[1].Value;
                                else
                                    obj.Args[n] = arg_data[n];
                            }
                            else
                                obj.Args[n] = arg_data[n];

                        }
                        var properties = obj.GetType().GetProperties();
                        foreach(var prop in properties)
                        {
                            var attrs = prop.GetCustomAttributes(typeof(ArticlePropertyAttribute), true);
                            if(attrs.Any() && (attrs[0] as ArticlePropertyAttribute).ArgIndex != -1)
                            {
                                ArticlePropertyAttribute attr = attrs[0] as ArticlePropertyAttribute;
                                if (prop.GetValue(obj) is IList list)
                                {
                                    if (prop.PropertyType.GetGenericArguments()[0] == typeof(Point))
                                    {
                                        if (obj.Args[attr.ArgIndex] is RoomObj item)
                                        {
                                            if (!(item.data[0] is RoomObj))
                                            {
                                                Point vec = new Point((double)item.data[0], (double)item.data[1]);
                                                list.Add(vec);
                                            }
                                            else
                                            {

                                                foreach (RoomObj vectorData in (obj.Args[attr.ArgIndex] as RoomObj).data)
                                                {
                                                    Point vec = new Point(Convert.ToDouble(vectorData.data[0]), Convert.ToDouble(vectorData.data[1]));
                                                    list.Add(vec);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (obj.Args[attr.ArgIndex] is RoomObj)
                                        {
                                            foreach (var item in (obj.Args[attr.ArgIndex] as RoomObj).data)
                                            {
                                                if (list.GetType().GetGenericArguments()[0] == typeof(int))
                                                    list.Add((int)item);
                                                else if (list.GetType().GetGenericArguments()[0] == typeof(float))
                                                    list.Add(Convert.ToSingle(item));
                                                else
                                                    list.Add(item);
                                            }
                                        }
                                        else
                                        {
                                            var item = obj.Args[attr.ArgIndex];
                                            if (list.GetType().GetGenericArguments()[0] == typeof(int))
                                                list.Add((int)item);
                                            else if (list.GetType().GetGenericArguments()[0] == typeof(float))
                                                list.Add(Convert.ToSingle(item));
                                            else
                                                list.Add(item);
                                        }
                                    }
                                }
                                else
                                {
                                    if (prop.GetValue(obj) is string)
                                        prop.SetValue(obj, obj.Args[attr.ArgIndex].ToString());
                                    else
                                        prop.SetValue(obj, obj.Args[attr.ArgIndex]);
                                }
                            }
                        }
                        obj.ExtraArgs.AddRange(((RoomObj)obj_data[6]).data);
                        room.Objs.Add(obj);
                        obj.CellX = (int)coords[0];
                        obj.CellY = (int)coords[1];
                        if(obj is Target targ)
                        {
                            for(int n = 0; n < targ.Path.Count; n++)
                            {
                                targ.Path[n] = new Point(targ.Path[n].X - obj.CellX * (ROAAM_CONST.CELL_WIDTH / ROAAM_CONST.GRID_SIZE), targ.Path[n].Y - obj.CellY * (ROAAM_CONST.CELL_HEIGHT / ROAAM_CONST.GRID_SIZE));
                            }
                        }
                    }
                }
            }
            return rooms;
        }

        private RoomObj ExtractData(string text)
        {
            var root_obj = new RoomObj();
            var obj_stack = new Stack<RoomObj>();
            obj_stack.Push(root_obj);
            var start = text.IndexOf('[') + 1;
            string next_name = "";
            while(start != -1 && start < text.Length)
            {
                char next_token = text[start];
                if(next_token == '[')
                {
                    var obj = new RoomObj();
                    obj_stack.Peek().data.Add(obj);
                    obj_stack.Push(obj);
                    obj.name = next_name;
                    next_name = "";
                }
                else if(next_token == ']')
                {
                    obj_stack.Pop();
                }
                else if (next_token == '\"')
                {
                    var close = text.IndexOf('\"', start + 1);
                    obj_stack.Peek().data.Add(text.Substring(start + 1, close - (start + 1)));
                    start = close;
                }
                else if (char.IsDigit(next_token) || next_token == '-')
                {
                    var close = text.IndexOfAny(new char[] { ',', ']', ' ', '\t' }, start);
                    var trim = text.Substring(start, close - start);
                    try
                    {
                        if (trim.Contains('.'))
                            obj_stack.Peek().data.Add(float.Parse(trim));
                        else
                            obj_stack.Peek().data.Add(int.Parse(trim));
                    }
                    catch
                    {
                        obj_stack.Peek().data.Add(0.0f);
                    }
                    start = close - 1;
                }
                else if (next_token == '/' && text[start + 1] == '*')
                {
                    var close = text.IndexOf("*/", start);
                    // Get just the name portion
                    var match = Regex.Match(text.Substring(start, close - start), "([\\w\\d]+)");
                    if (match.Success)
                        next_name = match.Captures[0].Value;
                    start = close + 1;
                }
                else if(char.IsLetter(next_token))
                {
                    var close = text.IndexOfAny(new char[] { ',', ']'}, start + 1);
                    obj_stack.Peek().data.Add(text.Substring(start, close - start));
                    start = close - 1;
                }

                start++;
            }

            return root_obj;
        }

        private void OnFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ErrorMsg.Visibility = Visibility.Collapsed;
        }

        class RoomObj
        {
            public List<object> data = new List<object>();
            public string name;

            public object this[int i]
            {
                get { return data[i]; }
            }

            public int Count => data.Count;
        }
    }
}
