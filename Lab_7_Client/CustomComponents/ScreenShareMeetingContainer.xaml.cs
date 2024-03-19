using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Lab_7_Client.CustomComponents
{
    /// <summary>
    /// Interaction logic for ScreenShareMeetingContainer.xaml
    /// </summary>
    public partial class ScreenShareMeetingContainer : UserControl, IMeetingContainer
    {
        public UIElementCollection Collection { get; private set; }
        public List<MeetingParticipantContainer> ParticipantsContainers { get; private set; }

        public ScreenShareMeetingContainer(List<MeetingParticipantContainer> participantsContainers)
        {
            InitializeComponent();

            ParticipantsContainers = participantsContainers;

            Collection = ParticipantsPanel.Children;

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            InitContainers();
        }

        public void UpdateContainers()
        {
            for (var i = 0; i < ParticipantsContainers.Count; i++)
            {
                if (!Client.Participants.Any(x => Equals(x.IpEndPoint, ParticipantsContainers[i].ClientIpEP)))
                {
                    ParticipantsContainers.RemoveAt(i);
                    Collection.RemoveAt(i);
                    i--;
                }
            }

            for (var i = 0; i < Client.Participants.Count; i++)
            {
                if (!ParticipantsContainers.Any(x => Equals(x.ClientIpEP, Client.Participants[i].IpEndPoint)))
                {
                    var newClientContainer = new MeetingParticipantContainer(Client.Participants[i].Name, Client.Participants[i].IpEndPoint);
                    newClientContainer.Width = 100;
                    newClientContainer.Height = 100;
                    ParticipantsContainers.Add(newClientContainer);
                    Collection.Add(newClientContainer);
                }
            }
        }

        public void InitContainers()
        {
            foreach (var container in ParticipantsContainers)
            {
                if (!Collection.Contains(container))
                {
                    container.Width = 100;
                    container.Height = 100;
                    Collection.Add(container);
                }
            }
        }

        public void UpdateShareScreen(BitmapImage frame)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                SharedContent.Source = frame;
            });
        }
    }
}
