using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;

namespace FiZZ
{
    public partial class MainWindow : Window
    {
        private MainWindowModel _model;
        
        public MainWindow()
        {
            InitializeComponent();
            _model = new MainWindowModel(ShowInfo, ShowError);
            DataContext = _model;
        }

        private async Task ReadTorrentFile(byte[] data)
        {
            var bencode = new BencodeParser(data);

            var dict = bencode.Read();
            if (dict is null)
            {
                ShowError($"bencode read error");
                return;
            }

            var infoDictData = bencode.InfoDictData;
            if (infoDictData is null)
            {
                ShowError("bencode read error");
                return;
            }

            var torrent = TorrentBuilder.BuildTorrent(dict);
            if (torrent is null)
            {
                ShowError("error while building torrent");
                return;
            }
            
            await _model.AddTorrentAsync(infoDictData, torrent);
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new()
            {
                Filter = "Torrent Files (*.torrent)|*.torrent",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            var result = dlg.ShowDialog();

            if (result != true) return;
            
            var path = dlg.FileName;

            try
            {
                if (!File.Exists(path)) return;
                var contents = File.ReadAllBytes(path);
                Task.Run(async () =>
                {
                    await ReadTorrentFile(contents);
                });
            }
            catch (IOException ex)
            {
                ShowError($"Failed to read file: {ex.Message}");
            }
        }

        private static void ShowInfo(string msg)
        {
            MessageBox.Show(msg, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static void ShowError(string msg)
        {
            MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}