using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;

namespace FiZZ
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void ReadTorrentFile(string contents)
        {
            char[] data = contents.ToCharArray();

            var bencode = new BencodeParser(data);

            var dict = bencode.Read();
            if (dict is null)
            {
                var errors = bencode.ErrorMessages;
                StringBuilder sb = new();
                while (errors.TryPop(out var error))
                {
                    sb.Append($"{error}\n");
                }
                ShowError($"Bencode read error:\n{sb}");
                return;
            }

            ShowInfo($"Announce: {dict["announce"] as string}");
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new()
            {
                Filter = "Torrent Files (*.torrent)|*.torrent",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                string path = dlg.FileName;

                try
                {
                    if (File.Exists(path))
                    {
                        using StreamReader reader = new(path);
                        string contents = reader.ReadToEnd();
                        ReadTorrentFile(contents);
                    }
                }
                catch (IOException ex)
                {
                    ShowError($"Failed to read file: {ex.Message}");
                }
            } 
        }

        private void ShowInfo(string msg)
        {
            MessageBox.Show(msg, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowError(string msg)
        {
            MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}