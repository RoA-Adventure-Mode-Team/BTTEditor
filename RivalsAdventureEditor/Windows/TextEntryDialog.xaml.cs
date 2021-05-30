using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RivalsAdventureEditor.Windows
{
    /// <summary>
    /// Interaction logic for TextEntryDialog.xaml
    /// </summary>
    public partial class TextEntryDialog : Window
    {
        #region DisplayTextProperty
        public static DependencyProperty DisplayTextProperty = DependencyProperty.Register(nameof(DisplayText),
            typeof(string),
            typeof(TextEntryDialog));
        public string DisplayText
        {
            get { return GetValue(DisplayTextProperty) as string; }
            set { SetValue(DisplayTextProperty, value); }
        }
        #endregion

        #region InputTextProperty
        public static DependencyProperty InputTextProperty = DependencyProperty.Register(nameof(InputText),
            typeof(string),
            typeof(TextEntryDialog));
        public string InputText
        {
            get { return GetValue(InputTextProperty) as string; }
            set { SetValue(InputTextProperty, value); }
        }
        #endregion

        public delegate bool VerifyText(string text, out string error);
        public VerifyText Verification { get; set; }

        public TextEntryDialog()
        {
            InitializeComponent();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if(Verification != null && !Verification(DisplayText, out string errorText))
            {
                errorTextLabel.Content = errorText;
                errorTextLabel.Visibility = Visibility.Visible;
                okButton.IsEnabled = false;
                return;
            }

            DialogResult = true;
            Close();
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            errorTextLabel.Visibility = Visibility.Collapsed;
            okButton.IsEnabled = true;
        }
    }
}
