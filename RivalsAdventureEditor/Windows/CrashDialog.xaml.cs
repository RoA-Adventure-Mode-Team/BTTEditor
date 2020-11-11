using RivalsAdventureEditor.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RestSharp;
using System.IO;

namespace RivalsAdventureEditor.Windows
{
    /// <summary>
    /// Interaction logic for CrashDialog.xaml
    /// </summary>
    public partial class CrashDialog : Window
    {
        public ImageSource ErrorIcon
        {
            get
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                          SystemIcons.Error.ToBitmap().GetHbitmap(), IntPtr.Zero, Int32Rect.Empty,
                          BitmapSizeOptions.FromEmptyOptions());
            }
        }

        public Exception Exception { get; set; }

        public CrashDialog()
        {
            InitializeComponent();
        }

        public void SaveLog()
        {
            // Print the error to appdata
            StringBuilder sb = new StringBuilder();
            sb.Append(Exception.ToString());
            sb.Append("\n--------------------------\n");
            var count = ApplicationSettings.Instance.SystemLog.Count;
            if (count > 50)
                count = 50;
            foreach (var entry in ApplicationSettings.Instance.SystemLog.GetRange(ApplicationSettings.Instance.SystemLog.Count - count, count))
            {
                sb.AppendLine(entry);
            }

            var crashDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RivalsAdventureEditor", "crashlogs");
            var crashFile = Path.Combine(crashDir, $"crash_{DateTime.Now.ToString("MM-dd-yyyy_H.mm.ss")}.txt");
            if (!Directory.Exists(crashDir))
                Directory.CreateDirectory(crashDir);
            File.WriteAllText(crashFile, sb.ToString());
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ReportBug(object sender, RoutedEventArgs e)
        {
            crashInfo.Visibility = Visibility.Collapsed;
            bugReport.Visibility = Visibility.Visible;
        }

        private void OnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (saveCheckbox.IsChecked == true)
            {
                foreach (var project in ApplicationSettings.Instance.Projects)
                {
                    project.Save();
                }
            }
        }

        private void SubmitReport(object sender, RoutedEventArgs e)
        {
            var client = new RestClient("https://docs.google.com/forms/u/0/d/e/1FAIpQLSf-eRjb0uJxiwwpz1Hq_PwFNhLolC8fq3gtumVP9PF221BbyQ/formResponse");
            var request = new RestRequest(Method.POST);
            StringBuilder sb = new StringBuilder();
            sb.Append(Exception.ToString());
            if (systemLogBox.IsChecked == true)
            {
                sb.Append("\n--------------------------\n");
                var count = ApplicationSettings.Instance.SystemLog.Count;
                if (count > 50)
                    count = 50;
                foreach (var entry in ApplicationSettings.Instance.SystemLog.GetRange(ApplicationSettings.Instance.SystemLog.Count - count, count))
                {
                    sb.AppendLine(entry);
                }
            }
            request.AddParameter("entry.85915008", nameField.Text);
            request.AddParameter("entry.2061625189", detailsField.Text);
            request.AddParameter("entry.1573175819", sb.ToString());

            var response = client.Execute(request);

            Close();
        }
    }
}
