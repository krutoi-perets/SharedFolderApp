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

namespace SharedFolderApp.Client.Views
{
    /// <summary>
    /// Логика взаимодействия для TextViewWindow.xaml
    /// </summary>
    public partial class TextViewWindow : Window
    {
        public TextViewWindow(string fileName, string text)
        {
            InitializeComponent();

            this.Title = fileName;

            textField.Text = text;
            textField.Options.HighlightCurrentLine = true;
            textField.Options.ConvertTabsToSpaces = true;
            textField.Options.IndentationSize = 4;

            textField.TextArea.Caret.PositionChanged += (s, e) =>
            {
                line.Content = $"Line: {textField.TextArea.Caret.Line}";
                column.Content = $"Column: {textField.TextArea.Caret.Column}";
            };
        }
    }
}
