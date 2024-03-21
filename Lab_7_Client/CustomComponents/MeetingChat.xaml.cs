using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

namespace Lab_7_Client.CustomComponents
{
    /// <summary>
    /// Interaction logic for MeetingChat.xaml
    /// </summary>
    public partial class MeetingChat : UserControl
    {
        public MeetingParticipant Receiver { get; private set; } = null;

        public MeetingChat()
        {
            InitializeComponent();

            Client.ClientListUpdated += OnClientListUpdated;

            Client.MessageReceived += OnMessageReceived;

            Client.FileUploaded += OnFileUploaded;
        }

        private void OnFileUploaded(string senderName, long fileSize, string fileName, string idName, string receiver)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var fi = new ChatFileDownloadItem(senderName, fileSize, fileName, idName, receiver);
                MessagesContainer.Children.Add(fi);
            });
        }

        private void OnMessageReceived(string name, string message, string receiver)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var sp = new StackPanel();
                var l = new Label()
                {
                    Content = $"{name} to {receiver}"
                };
                var tb = new TextBox()
                {
                    Margin = new Thickness(10, 0, 0, 0),
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.WrapWithOverflow,
                    Text = message
                };

                sp.Children.Add(l);
                sp.Children.Add(tb);

                MessagesContainer.Children.Add(sp);
            });
        }

        private void OnClientListUpdated()
        {
            UpdateReceiverList();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (Placeholder.Visibility != Visibility.Hidden)
            {
                Placeholder.Visibility = Visibility.Hidden;
            }

            if (e.Key == Key.Enter && MyInput.Text.Length > 0)
            {
                if (Receiver == null)
                {
                    Client.SendMessageToAll(MyInput.Text);
                    ShowOwnMessage("Everyone");
                }
                else
                {
                    Client.SendMessage(MyInput.Text, Receiver.IpEndPoint);
                    ShowOwnMessage(Receiver.Name);
                }
                MyInput.Text = String.Empty;
                Placeholder.Visibility = Visibility.Visible;
            }
        }

        private void ShowOwnMessage(string receiver)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var sp = new StackPanel();
                var l = new Label()
                {
                    Content = $"You to {receiver}"
                };
                var tb = new TextBox()
                {
                    Margin = new Thickness(10, 0, 0, 0),
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.WrapWithOverflow,
                    Text = MyInput.Text
                };

                sp.Children.Add(l);
                sp.Children.Add(tb);

                MessagesContainer.Children.Add(sp);
            });
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Back && MyInput.Text.Length == 1) || (e.Key == Key.Back && MyInput.Text.Length == MyInput.SelectedText.Length))
            {
                Placeholder.Visibility = Visibility.Visible;
            }
        }

        private void OnReceiverSwitcherClicker(object sender, RoutedEventArgs e)
        {
            if (ReceiverListContainer.Visibility == Visibility.Hidden)
            {
                UpdateReceiverList();
                ReceiverListContainer.Visibility = Visibility.Visible;
            }
            else
            {
                ReceiverListContainer.Visibility = Visibility.Hidden;
            }
        }

        public void UpdateReceiverList()
        {
            ReceiverList.Children.Clear();

            var topItem = new ChatReceiverListItem();
            ReceiverList.Children.Add(topItem);
            topItem.OnReceiverSet += OnReceiverSet;

            foreach (var participant in Client.Participants)
            {
                if (!Equals(participant.IpEndPoint, Client.LocalIpEndPoint))
                {
                    var item = new ChatReceiverListItem(participant);
                    item.OnReceiverSet += OnReceiverSet;
                    ReceiverList.Children.Add(item);
                }
            }
        }

        private void OnReceiverSet(MeetingParticipant meetingParticipant)
        {
            Receiver = meetingParticipant;
            ReceiverListContainer.Visibility = Visibility.Hidden;

            if (Receiver != null)
            {
                ReceiverSwitcher.Content = Receiver.Name;
            }
            else
            {
                ReceiverSwitcher.Content = "Everyone";
            }
        }

        private void OnFileBtnClicked(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog();

            var path = "";

            if (d.ShowDialog() == true)
            {
                path = d.FileName;
            }
                
            if (path != "")
            {
                var file = new FileInfo(path);

                if (file.Length < 128 * 1024 * 1024)
                {
                    var name = "Everyone";
                    if (Receiver != null)
                    {
                        name = Receiver.Name;
                    }

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        var cfi = new ChatFileItem(file, Receiver);
                        cfi.Deleted += OnFileDeleted;
                        cfi.Uploaded += OnFileUploaded;
                        MessagesContainer.Children.Add(cfi);
                    });
                }
                else
                {
                    MessageBox.Show("Max file size - 128 MB", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }                
            }

        }

        private void OnFileUploaded(ChatFileItem chatFileItem)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                chatFileItem.Uploaded -= OnFileDeleted;
                for (var i = 0; i < MessagesContainer.Children.Count; i++)
                {
                    if (MessagesContainer.Children[i] == chatFileItem)
                    {
                        MessagesContainer.Children.RemoveAt(i);
                        MessagesContainer.Children.Insert(i, new ChatFileDownloadItem(
                            Path.GetFileName(chatFileItem.FilePath),
                            chatFileItem.FileLength, 
                            chatFileItem.IdName, 
                            chatFileItem.ReceiverName));
                        break;
                    }
                }
            });
        }

        private void OnFileDeleted(ChatFileItem chatFileItem)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                MessagesContainer.Children.Remove(chatFileItem);
                chatFileItem.Deleted -= OnFileDeleted;
            });
        }
    }
}
