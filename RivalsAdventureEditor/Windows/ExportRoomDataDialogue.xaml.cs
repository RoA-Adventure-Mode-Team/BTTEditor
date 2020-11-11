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
    /// Interaction logic for ExportRoomDataDialogue.xaml
    /// </summary>
    public partial class ExportRoomDataDialogue : Window
    {
        public Project Project { get; set; }

        public ExportRoomDataDialogue()
        {
            InitializeComponent();
        }

        private void ExportText(object sender, RoutedEventArgs e)
		{

            InputBox.Text = Project.GenerateRoomData();
        }
    }
}
