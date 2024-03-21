using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Lab_7_Client.CustomComponents
{
    /// <summary>
    /// Interaction logic for ChatFileDownloadItem.xaml
    /// </summary>
    public partial class ChatFileDownloadItem : UserControl
    {
        public string FileName { get; private set; }
        public string IdName { get; private set; }
        public long FileLength { get; private set; }

        public string OutputPath { get; private set; }

        public bool IsDownloading { get; private set; } = false;

        private long _byteIndex = 0;
        private const int _bufferSize = 32768;

        public ChatFileDownloadItem(string sender, long fileLength, string fileName, string idName, string receiver)
        {
            InitializeComponent();

            MessageLabel.Content = $"{sender} to {receiver}";

            string size;
            if (fileLength > 1024 * 1024)
                size = $"{Math.Round((double)fileLength / 1024 / 1024, 1)} MB";
            else
                size = $"{Math.Round((double)fileLength / 1024, 1)} KB";

            FileLength = fileLength;

            var name = fileName;
            if (name.Length > 30)
                name = name.Substring(0, 30) + "...";

            FileInfo.Content = $"{name} - {size}";

            FileName = fileName;
            IdName = idName;

            Client.FilePartDownloaded += OnFilePartDownloaded;
        }

        public ChatFileDownloadItem(string fileName, long fileLength, string idName, string receiver)
        {
            InitializeComponent();

            MessageLabel.Content = $"You to {receiver}";

            string size;
            if (fileLength > 1024 * 1024)
                size = $"{Math.Round((double)fileLength / 1024 / 1024, 1)} MB";
            else
                size = $"{Math.Round((double)fileLength / 1024, 1)} KB";

            FileLength = fileLength;

            var name = fileName;
            if (name.Length > 30)
                name = name.Substring(0, 30) + "...";

            FileInfo.Content = $"{name} - {size}";

            FileName = fileName;
            IdName = idName;

            Client.FilePartDownloaded += OnFilePartDownloaded;
        }


        private void OnBtnClicked(object sender, RoutedEventArgs e)
        {
            if (!IsDownloading)
            {
                var sfd = new SaveFileDialog();
                sfd.FileName = FileName;
                var ext = Path.GetExtension(FileName);
                sfd.Filter = $"File (*{ext})|*{ext}";

                var path = "";

                if (sfd.ShowDialog() == true)
                {
                    path = sfd.FileName;
                }

                if (path != "")
                {
                    OutputPath = path;
                    IsDownloading = true;

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        FileBtn.Content = "Downloading...";
                    });

                    _byteIndex = 0;

                    Download();
                }
            }
            else
            {
                IsDownloading = false;

                if (File.Exists(OutputPath))
                {
                    File.Delete(OutputPath);
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    FileBtn.Content = "Download";
                });
            }
        }

        private void Download()
        {
            Client.DownloadFile(IdName, _byteIndex);
        }

        private void OnFilePartDownloaded(string fileId, byte[] data)
        {
            if (fileId == IdName && IsDownloading)
            {
                using (var file = File.OpenWrite(OutputPath))
                {
                    file.Seek(_byteIndex, SeekOrigin.Begin);

                    file.Write(data, 0, data.Length);
                }
                _byteIndex += _bufferSize;

                if (_byteIndex >= FileLength)
                {
                    IsDownloading = false;
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        FileBtn.Content = "Download";
                    });
                }
                else
                {
                    Download();
                }
            }
        }
    }
}
