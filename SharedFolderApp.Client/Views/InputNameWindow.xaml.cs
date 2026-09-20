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
    /// Логика взаимодействия для RenameWindow.xaml
    /// </summary>
    public partial class InputNameWindow : Window
    {
        public string NewName { get; private set; } = string.Empty;

        public InputNameWindow(string oldName)
        {
            InitializeComponent();

            renameTextBox.Text = oldName;

            Loaded += (s, e) =>
            {
                renameTextBox.Focus();

                var extension = System.IO.Path.GetExtension(oldName);
                renameTextBox.Select(0, oldName.Length - extension.Length);
            };
        }

        private void renameButton_Click(object sender, RoutedEventArgs e)
        {
            CompleteRenaming(true);
        }

        private void renameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                CompleteRenaming(true);
            else if (e.Key == Key.Escape)
                CompleteRenaming(false);
        }

        private void CompleteRenaming(bool completed)
        {
            if (string.IsNullOrWhiteSpace(renameTextBox.Text))
                return;

            NewName = renameTextBox.Text;

            DialogResult = true;
        }
    }
}
