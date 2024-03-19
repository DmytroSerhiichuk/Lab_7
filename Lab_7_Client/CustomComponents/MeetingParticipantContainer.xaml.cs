using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Lab_7_Client.CustomComponents
{
    /// <summary>
    /// Interaction logic for MeetingParticipantContainer.xaml
    /// </summary>
    public partial class MeetingParticipantContainer : UserControl
    {
        public IPEndPoint ClientIpEP { get; private set; }

        public bool IsCameraWorking { get; private set; } = false;        

        public MeetingParticipantContainer(string name, IPEndPoint ipEndPoint)
        {
            InitializeComponent();

            ClientName.Text = name;

            ClientIpEP = ipEndPoint;
        }

        public void CameraOn()
        {
            IsCameraWorking = true;
        }
        public void CameraOff()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ClientCamera.Source = null;
                IsCameraWorking = false;
            });
        }
        public void UpdateCamera(BitmapImage frame)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (IsCameraWorking)
                {
                    ClientCamera.Source = frame;
                }
            });
        }

        public void AudioOn()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                MainBorder.BorderThickness = new Thickness(3);
                MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(15, 66, 218));
            });
        }
        public void AudioOff()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                MainBorder.BorderThickness = new Thickness(1);
                MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(171, 173, 179));
            });
        }
    }
}
