using RivalsAdventureEditor.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;

namespace RivalsAdventureEditor.Panels
{
    public class WindowAPI
    {
        [DllImport("D3DContent.dll")]
        public static extern int InitRenderer(IntPtr hwnd, out IntPtr renderer);

        [DllImport("D3DContent.dll")]
        public static extern int DestroyRenderer(IntPtr renderer);

        [DllImport("D3DContent.dll")]
        public static extern int SetSize(IntPtr renderer, int width, int height);

        [DllImport("D3DContent.dll")]
        public static extern int CreateBrush(IntPtr renderer, int color, out IntPtr brush);

        [DllImport("D3DContent.dll")]
        public static extern int RegisterTexture(IntPtr renderer, [MarshalAs(UnmanagedType.LPStr)] string key, [MarshalAs(UnmanagedType.LPWStr)] string fname, int frames, out int texture);

        [DllImport("D3DContent.dll")]
        public static extern int SetCameraTransform(IntPtr renderer, Point pos, float zoom);

        [DllImport("D3DContent.dll")]
        public static extern int PrepareForRender(IntPtr renderer);

        [DllImport("D3DContent.dll")]
        public static extern int Render(IntPtr renderer, DX_Article[] articles, int count, DX_Line[] lines, int line_count, DX_Tilemap[] tilemaps, int tilemap_count);
        
        internal const int
           LBN_SELCHANGE = 0x00000001,
           WM_COMMAND = 0x00000111,
           LB_GETCURSEL = 0x00000188,
           LB_GETTEXTLEN = 0x0000018A,
           LB_ADDSTRING = 0x00000180,
           LB_GETTEXT = 0x00000189,
           LB_DELETESTRING = 0x00000182,
           LB_GETCOUNT = 0x0000018B;

        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Unicode)]
        internal static extern int SendMessage(IntPtr hwnd,
                                               int msg,
                                               IntPtr wParam,
                                               IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Unicode)]
        internal static extern int SendMessage(IntPtr hwnd,
                                               int msg,
                                               int wParam,
                                               [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lParam);

        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Unicode)]
        internal static extern IntPtr SendMessage(IntPtr hwnd,
                                                  int msg,
                                                  IntPtr wParam,
                                                  String lParam);

        public static bool LoadImage(string name, IntPtr renderer, out TexData texData)
        {
            Point offset = new Point();
            string loadFile = Path.Combine(Path.GetDirectoryName(ApplicationSettings.Instance.ActiveProject.ProjectPath), "scripts", "load.gml");
            MatchCollection matches = null;
            if (File.Exists(loadFile))
            {
                string lines = File.ReadAllText(loadFile);
                matches = Regex.Matches(lines, "sprite_change_offset\\s*\\(\\s*\"([\\w\\d]+)\",\\s*(\\d+),\\s*(\\d+)\\s*\\)");
            }
            string directory;
            if (ApplicationSettings.Instance.ActiveProject.Type == ProjectType.AdventureMode)
                directory = Path.Combine(Path.GetDirectoryName(ApplicationSettings.Instance.ActiveProject.ProjectPath), "sprites", "articles");
            else
                directory = Path.Combine(Path.GetDirectoryName(ApplicationSettings.Instance.ActiveProject.ProjectPath), "sprites");
            if (File.Exists(Path.Combine(directory, name + ".png")))
            {
                string path = Path.Combine(directory, name + ".png");
                System.Drawing.Bitmap img = null;
                using (FileStream file = new FileStream(path, FileMode.Open))
                {
                    MemoryStream stream = new MemoryStream();
                    file.CopyTo(stream);
                    img = new System.Drawing.Bitmap(stream);
                }
                WindowAPI.RegisterTexture(renderer, name, path, 1, out int texture);
                if (matches != null)
                {
                    Match match = matches.OfType<Match>().FirstOrDefault(m => m.Groups[1].Value == name);
                    if (match != null)
                    {
                        offset.X = Double.Parse(match.Groups[2].Value);
                        offset.Y = Double.Parse(match.Groups[3].Value);
                    }
                }
                texData = new TexData(true, texture, img, offset);
                return true;
            }

            var files = Directory.EnumerateFiles(directory, name + "*.png");
            if (files.Any())
            {
                string file = files.FirstOrDefault(f => Regex.Match(f, name + "_strip(\\d+)").Success);
                if (!string.IsNullOrEmpty(file))
                {
                    Match match = Regex.Match(file, "strip(\\d+)");
                    int count = int.Parse(match.Groups[1].Value);
                    System.Drawing.Bitmap img = null;
                    using (FileStream fstream = new FileStream(file, FileMode.Open))
                    {
                        MemoryStream stream = new MemoryStream();
                        fstream.CopyTo(stream);
                        img = new System.Drawing.Bitmap(stream);
                    }
                    WindowAPI.RegisterTexture(renderer, name, file, count, out int texture);
                    int index = file.IndexOf("_strip");
                    if (matches != null)
                    {
                        Match offsetMatch = matches.OfType<Match>().FirstOrDefault(m => m.Groups[1].Value == file.Substring(0, index));
                        if (offsetMatch != null)
                        {
                            offset.X = Double.Parse(offsetMatch.Groups[2].Value);
                            offset.Y = Double.Parse(offsetMatch.Groups[3].Value);
                        }
                    }
                    texData = new TexData(true, texture, img, offset);
                    return true;
                }
            }
            texData = new TexData(false, 0);
            return false;
        }

        public static System.Drawing.Bitmap BitmapImageToBitmap(BitmapImage bitmapImage)
        {
            // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new System.Drawing.Bitmap(bitmap);
            }
        }
    }

    public struct TexData
    {
        public TexData(bool _exists, int _texture, System.Drawing.Bitmap _image = null)
        {
            exists = _exists;
            texture = _texture;
            image = _image;
        }

        public TexData(bool _exists, int _texture, System.Drawing.Bitmap _image, Point _offset)
        {
            exists = _exists;
            texture = _texture;
            offset = _offset;
            image = _image;
        }

        public bool exists;
        public int texture;
        public Point offset;
        public System.Drawing.Bitmap image;
    }
}
