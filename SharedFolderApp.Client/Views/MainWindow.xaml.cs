using SharedFolderApp.Client.Services;
using SharedFolderApp.Shared.Models;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.ComponentModel;
using System.Linq.Expressions;
using SharedFolderApp.Client.Views;
using SharedFolderApp.Shared.Utils;

namespace SharedFolderApp.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _currentServerPath = "";
        private string _currentClientPath = "";

        private readonly Queue<FileItem> _downloadQueue = new();
        private bool _isDownloading;

        private readonly ServerService _serverService = new();
        private readonly ClientFileService _fileService = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void mainForm_Loaded(object sender, RoutedEventArgs e)
        {
            var files = _fileService.GetItems();
            clientExplorer.ItemsSource = files;
            RefreshClientExplorer();
        }

        private async void connectButton_Click(object sender, RoutedEventArgs e)
        {
            serverExplorer.ItemsSource = null;
            serverExplorer.IsEnabled = false;

            var address = serverIpTb.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(address))
            {
                MessageBox.Show("Введите адрес сервера",
                                "Адрес пустой",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            connectButton.IsEnabled = false;

            connectionStatusLabel.Content = "Подключение...";
            var connected = await _serverService.ConnectAsync(address);

            connectButton.IsEnabled = true;

            if (connected)
            {
                connectionStatusLabel.Content = "Подключено";
                serverExplorer.IsEnabled = true;
                await LoadFilesAsync();
            }
            else
            {
                connectionStatusLabel.Content = "Не удалось подключиться";
            }
        }

        private async Task LoadFilesAsync(string path = "")
        {
            try
            {
                var files = await _serverService.GetFilesAsync(path);

                serverExplorer.ItemsSource = files;
                
                UpdateServerPathBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки файлов: {ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private async void serverExplorer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (serverExplorer.SelectedItem is not FileItem item)
                return;

            OpenServer(item);
        }

        private void clientExplorer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (clientExplorer.SelectedItem is not FileItem item)
                return;

            OpenClient(item);
        }

        private void UpdateServerPathBar()
        {
            serverPathPanel.Children.Clear();

            var rootButton = new Button
            {
                Content = "Server",
                Style = (Style)FindResource("PathButtonStyle"),
                Margin = new Thickness(2)
            };

            rootButton.Click += async (s, e) =>
            {
                _currentServerPath = "";
                await RefreshServerExplorer();
            };

            serverPathPanel.Children.Add(rootButton);

            if (string.IsNullOrEmpty(_currentServerPath))
                return;

            var parts = _currentServerPath.Split(System.IO.Path.DirectorySeparatorChar,
                                           System.IO.Path.AltDirectorySeparatorChar);

            string currentPath = "";

            foreach (var part in parts)
            {
                currentPath = string.IsNullOrEmpty(currentPath) ? part : System.IO.Path.Combine(currentPath, part);

                var path = currentPath;

                serverPathPanel.Children.Add(new TextBlock
                {
                    Text = ">",
                    VerticalAlignment = VerticalAlignment.Center
                });

                var button = new Button
                {
                    Content = part,
                    Style = (Style)FindResource("PathButtonStyle"),
                    Margin = new Thickness(2)
                };

                button.Click += async (s, e) =>
                {
                    _currentServerPath = path;
                    await RefreshServerExplorer();
                };

                serverPathPanel.Children.Add(button);
            }

            serverPathScrollViewer.ScrollToRightEnd();
        }

        private void UpdateClientPathBar()
        {
            clientPathPanel.Children.Clear();

            var rootButton = new Button
            {
                Content = "Client",
                Style = (Style)FindResource("PathButtonStyle"),
                Margin = new Thickness(2)
            };

            rootButton.Click += (s, e) =>
            {
                _currentClientPath = "";
                RefreshClientExplorer();
            };

            clientPathPanel.Children.Add(rootButton);

            if (string.IsNullOrEmpty(_currentClientPath))
                return;

            var parts = _currentClientPath.Split(System.IO.Path.DirectorySeparatorChar,
                                           System.IO.Path.AltDirectorySeparatorChar);

            string currentPath = "";

            foreach (var part in parts)
            {
                currentPath = string.IsNullOrEmpty(currentPath) ? part : System.IO.Path.Combine(currentPath, part);

                var path = currentPath;

                clientPathPanel.Children.Add(new TextBlock
                {
                    Text = ">",
                    VerticalAlignment = VerticalAlignment.Center
                });

                var button = new Button
                {
                    Content = part,
                    Style = (Style)FindResource("PathButtonStyle"),
                    Margin = new Thickness(2)
                };

                button.Click += (s, e) =>
                {
                    _currentClientPath = path;
                    RefreshClientExplorer();
                };

                clientPathPanel.Children.Add(button);
            }

            clientPathScrollViewer.ScrollToRightEnd();
        }

        private async void RefreshServerExplorer_Click(object sender, RoutedEventArgs e)
        {
            await RefreshServerExplorer();
        }

        private void RefreshClientExplorer_Click(object sender, RoutedEventArgs e)
        {
            RefreshClientExplorer();
        }

        private async Task RefreshServerExplorer()
        {
            await LoadFilesAsync(_currentServerPath);
        }

        private void RefreshClientExplorer()
        {
            UpdateClientPathBar();
            clientExplorer.ItemsSource = _fileService.GetItems(_currentClientPath);
        }

        private async void Upload_Click(object sender, RoutedEventArgs e)
        {
            var items = clientExplorer.SelectedItems.Cast<FileItem>().ToList();

            bool uploaded = false;

            uploadProgressBar.Visibility = Visibility.Visible;
            uploadStatusLabel.Visibility = Visibility.Visible;
            uploadStatusLabel.Margin = new Thickness(0, 0, 155, 0);
            uploadProgressBar.Value = 0;

            var progress = new Progress<double>(value =>
            {
                uploadProgressBar.Value = value;
            });

            try
            {
                foreach (var item in items)
                {
                    var fullPath = _fileService.GetFullPath(item.Path);

                    var serverItems = await _serverService.GetFilesAsync(_currentServerPath);
                    var existingItem = serverItems.FirstOrDefault(x => x.Name == item.Name);

                    if (existingItem != null)
                    {
                        var result = MessageBox.Show($"Файл с именем {item.Name} уже существует. Заменить его?",
                                                     "Файл уже существует",
                                                     MessageBoxButton.YesNoCancel,
                                                     MessageBoxImage.Question);

                        if (result == MessageBoxResult.No)
                            continue;

                        if (result == MessageBoxResult.Cancel)
                            break;

                        await _serverService.DeleteAsync(existingItem.Path);
                    }

                    uploadStatusLabel.Content = $"Загрузка {item.Name}";

                    if (item.IsDirectory)
                        await _serverService.UploadDirectoryAsync(fullPath,
                            System.IO.Path.Combine(_currentServerPath, item.Name),
                            progress);
                    else
                        await _serverService.UploadFileAsync(fullPath, _currentServerPath, progress);

                    uploaded = true;

                    await RefreshServerExplorer();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить: {ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            finally
            {
                uploadProgressBar.Visibility = Visibility.Collapsed;
                uploadStatusLabel.Margin = new Thickness(0);
                uploadStatusLabel.Content = uploaded ? "Загрузка завершена" : "Ошибка загрузки";
            }
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            var items = serverExplorer.SelectedItems.Cast<FileItem>().ToList();

            foreach (var item in items)
            {
                _downloadQueue.Enqueue(item);
            }

            if (!_isDownloading)
                _ = ProcessDownloadQueueAsync();
        }

        private async Task ProcessDownloadQueueAsync()
        {
            _isDownloading = true;

            while (_downloadQueue.Count > 0)
            {
                var item = _downloadQueue.Dequeue();

                await DownloadItemAsync(item);
            }

            _isDownloading = false;
        }

        private async Task DownloadItemAsync(FileItem item)
        {
            bool downloaded = false;

            downloadProgressBar.Value = 0;
            downloadProgressBar.Visibility = Visibility.Visible;
            downloadStatusLabel.Visibility = Visibility.Visible;
            downloadStatusLabel.Margin = new Thickness(0, 0, downloadProgressBar.Width + downloadProgressBar.Margin.Left, 0);
            if (item.IsDirectory)
            {
                downloadStatusLabel.Content = $"Архивация {item.Name}...";
                downloadProgressBar.IsIndeterminate = true;
            }
            else
            {
                downloadStatusLabel.Content = $"Скачивание {item.Name}";
                downloadProgressBar.IsIndeterminate = false;
            }

            bool progressStarted = false;
            var progress = new Progress<double>(value =>
            {
                if (!progressStarted)
                {
                    progressStarted = true;
                    downloadProgressBar.IsIndeterminate = false;
                    downloadStatusLabel.Content = $"Скачивание {item.Name}";
                }

                downloadProgressBar.Value = value;
            });

            try
            {
                if (item.IsDirectory)
                {
                    if (_fileService.Exists(item.Path))
                    {
                        var result = MessageBox.Show(
                            $"Папка {item.Name} уже существует. Удалить все существующие файлы в папке?",
                            "Папка уже существует",
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            _fileService.ClearDirectory(item.Path);
                        }
                        else if (result == MessageBoxResult.Cancel)
                            return;
                    }
                }
                else
                {
                    if (_fileService.Exists(item.Path))
                    {
                        var result = MessageBox.Show(
                            $"Файл {item.Name} уже существует. Заменить?",
                            "Файл уже существует",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                        if (result == MessageBoxResult.No)
                            return;
                    }
                }

                var (stream, totalBytes) = await _serverService.DownloadAsync(item.Path);

                using (stream)
                {
                    if (item.IsDirectory)
                        await _fileService.SaveZipAsync(item.Path, stream, totalBytes, progress);
                    else
                        await _fileService.SaveFileAsync(item.Path, stream, totalBytes, progress);
                }

                RefreshClientExplorer();

                downloaded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка скачивания: {ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            finally
            {
                await Task.Delay(100);
                downloadProgressBar.Visibility = Visibility.Collapsed;
                downloadStatusLabel.Margin = new Thickness(0);
                downloadStatusLabel.Content = downloaded ? "Установка завершена" : "Ошибка установки";
            }
        }

        private async void OpenServer_Click(object sender, RoutedEventArgs e)
        {
            if (serverExplorer.SelectedItem is not FileItem item)
                return;

            OpenServer(item);
        }

        private void OpenClient_Click(object sender, RoutedEventArgs e)
        {
            if (clientExplorer.SelectedItem is not FileItem item)
                return;

            OpenClient(item);
        }

        private async void OpenServer(FileItem item)
        {
            if (item.IsDirectory)
            {
                _currentServerPath = item.Path;
                await LoadFilesAsync(_currentServerPath);
                return;
            }

            if (!FileTypeHelper.IsTextFile(item.Name))
            {
                MessageBox.Show("Посмотреть можно только текстовые файлы",
                                "Внимание",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                return;
            }

            try
            {
                var text = await _serverService.GetTextAsync(item.Path);

                var window = new TextViewWindow(item.Name, text)
                {
                    Owner = this
                };

                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть файл: {ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }
        
        private void OpenClient(FileItem item)
        {
            if (item.IsDirectory)
            {
                _currentClientPath = item.Path;
                clientExplorer.ItemsSource = _fileService.GetItems(_currentClientPath);
                RefreshClientExplorer();
                return;
            }

            try
            {
                _fileService.Open(item.Path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть файл: {ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void OpenInExplorer_Click(object sender, RoutedEventArgs e)
        {
            _fileService.OpenInExplorer(_currentClientPath);

            if (!_fileService.Exists(_currentClientPath))
                _currentClientPath = "";

            RefreshClientExplorer();
        }

        private async void CreateFolderServer_Click(object sender, RoutedEventArgs e)
        {
            var window = new InputNameWindow("Новая папка")
            {
                Owner = this
            };

            if (window.ShowDialog() != true)
                return;

            try
            {
                await _serverService.CreateFolderAsync(System.IO.Path.Combine(_currentServerPath, window.NewName));

                await RefreshServerExplorer();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось создать папку: {ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private async void CreateFileServer_Click(object sender, RoutedEventArgs e)
        {
            var window = new InputNameWindow("Новый файл")
            {
                Owner = this
            };

            if (window.ShowDialog() != true)
                return;

            try
            {
                await _serverService.CreateFileAsync(System.IO.Path.Combine(_currentServerPath, window.NewName));

                await RefreshServerExplorer();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось создать файл: {ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void CreateFolderClient_Click(object sender, RoutedEventArgs e)
        {
            var window = new InputNameWindow("Новая папка")
            {
                Owner = this
            };

            if (window.ShowDialog() != true)
                return;

            try
            {
                _fileService.CreateFolder(_currentClientPath, window.NewName);

                RefreshClientExplorer();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось создать папку: {ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void CreateFileClient_Click(object sender, RoutedEventArgs e)
        {
            var window = new InputNameWindow("Новый файл")
            {
                Owner = this
            };

            if (window.ShowDialog() != true)
                return;

            try
            {
                _fileService.CreateFile(_currentClientPath, window.NewName);

                RefreshClientExplorer();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось создать файл: {ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private async void RenameServer_Click(object sender, RoutedEventArgs e)
        {
            if (serverExplorer.SelectedItems.Count != 1)
                return;

            var item = (FileItem)serverExplorer.SelectedItem;

            var renameWindow = new InputNameWindow(item.Name)
            {
                Owner = this
            };

            if (renameWindow.ShowDialog() != true)
                return;

            if (renameWindow.NewName == item.Name)
                return;

            try
            {
                await _serverService.RenameAsync(item.Path, renameWindow.NewName);
                await RefreshServerExplorer();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось переименовать: {ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void RenameClient_Click(object sender, RoutedEventArgs e)
        {
            if (clientExplorer.SelectedItems.Count != 1)
                return;

            var item = (FileItem)clientExplorer.SelectedItem;

            var renameWindow = new InputNameWindow(item.Name)
            {
                Owner = this
            };

            if (renameWindow.ShowDialog() != true)
                return;

            if (renameWindow.NewName == item.Name)
                return;

            try
            {
                _fileService.Rename(item.Path, renameWindow.NewName);
                RefreshClientExplorer();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось переименовать: {ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private async void DeleteServer_Click(object sender, RoutedEventArgs e)
        {
            var items = serverExplorer.SelectedItems.Cast<FileItem>().ToList();

            var result = MessageBox.Show($"Вы действительно хотите удалить {items.Count} элементов?",
                                         "Удаление",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
                return;

            foreach (var item in items)
            {
                await _serverService.DeleteAsync(item.Path);
            }

            await RefreshServerExplorer();
        }

        private void DeleteClient_Click(object sender, RoutedEventArgs e)
        {
            var items = clientExplorer.SelectedItems.Cast<FileItem>().ToList();

            var result = MessageBox.Show($"Вы действительно хотите удалить {items.Count} элементов?",
                                         "Удаление",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
                return;

            foreach (var item in items)
            {
                _fileService.Delete(item.Path);
            }

            RefreshClientExplorer();
        }

        private void serverExplorer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = serverExplorer.SelectedItems.Count > 0;

            openServerButton.IsEnabled = hasSelection;
            renameServerButton.IsEnabled = hasSelection;
            deleteServerButton.IsEnabled = hasSelection;
            downloadServerButton.IsEnabled = hasSelection;
        }

        private void clientExplorer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = clientExplorer.SelectedItems.Count > 0;

            openClientButton.IsEnabled = hasSelection;
            renameClientButton.IsEnabled = hasSelection;
            deleteClientButton.IsEnabled = hasSelection;
            uploadClientButton.IsEnabled = hasSelection;
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            if (_serverService.IsConnected)
                await _serverService.DisconnectAsync();

            base.OnClosing(e);
        }
    }
}