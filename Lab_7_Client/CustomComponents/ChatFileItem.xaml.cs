using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Lab_7_Client.CustomComponents
{
    /// <summary>
    /// Interaction logic for ChatFileItem.xaml
    /// </summary>
    public partial class ChatFileItem : UserControl
    {
        public event Action<ChatFileItem>? Deleted;
        public event Action<ChatFileItem>? Uploaded;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        private MeetingParticipant _receiver;
        public string ReceiverName { get; private set; }
        public long FileLength { get; private set; }
        public string FilePath { get; private set; } = "";
        public string IdName { get; private set; }

        public ChatFileItem(FileInfo file, MeetingParticipant receiver)
        {
            InitializeComponent();

            if (receiver == null)
            {
                MessageLabel.Content = $"You to Everyone";
                ReceiverName = "Everyone";
            }
            else
            {
                MessageLabel.Content = $"You to {receiver.Name}";
                ReceiverName = receiver.Name;
            }

            string size;
            if (file.Length > 1024 * 1024)
                size = $"{Math.Round((double)file.Length / 1024 / 1024, 1)} MB";
            else
                size = $"{Math.Round((double)file.Length / 1024, 1)} KB";

            FileLength = file.Length;

            var name = file.Name;
            if (name.Length > 30)
                name = name.Substring(0, 30) + "...";

            FileInfo.Content = $"{name} - {size}";

            FilePath = file.FullName;
            IdName = $"{DateTime.Now.Ticks}{Path.GetExtension(FilePath)}";

            _receiver = receiver;

            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            var token = _cts.Token;
            await Task.Run(() =>
            {
                bool res;
                if (_receiver == null)
                {
                    res = Client.SendFileToAll(FilePath, IdName, token);
                }
                else
                {
                    res = Client.SendFile(FilePath, IdName, _receiver.IpEndPoint, token);
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    if (res)
                        Uploaded?.Invoke(this);
                    else
                        Deleted?.Invoke(this);
                });
            }, token);
        }

        private void OnBtnClicked(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
        }
    }
}
