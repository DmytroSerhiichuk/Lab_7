using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace Lab_7_Client.CustomComponents
{
    /// <summary>
    /// Interaction logic for ChatReceiverListItem.xaml
    /// </summary>
    public partial class ChatReceiverListItem : UserControl
    {
        public event Action<MeetingParticipant> OnReceiverSet;

        public MeetingParticipant Receiver { get; private set; }

        public ChatReceiverListItem(MeetingParticipant meetingParticipant)
        {
            InitializeComponent();

            Receiver = meetingParticipant;
            ReceiverName.Content = meetingParticipant.Name;
        }

        public ChatReceiverListItem()
        {
            InitializeComponent();

            Receiver = null;
            ReceiverName.Content = "Everyone";
        }

        private void OnClicked(object sender, RoutedEventArgs e)
        {
            OnReceiverSet?.Invoke(Receiver);
        }
    }
}
