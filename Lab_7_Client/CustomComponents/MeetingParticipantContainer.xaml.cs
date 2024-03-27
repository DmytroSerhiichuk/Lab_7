using System.Net;
using System.Security.Cryptography;
using System.Text;
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
        private WriteableBitmap _color;

        public IPEndPoint ClientIpEP { get; private set; }
        public bool IsCameraWorking { get; private set; } = false;        

        public MeetingParticipantContainer(string name, IPEndPoint ipEndPoint)
        {
            InitializeComponent();

            ClientName.Text = name;

            ClientIpEP = ipEndPoint;

            byte[] c;

            using (HashAlgorithm algorithm = SHA256.Create())
                c = algorithm.ComputeHash(Encoding.UTF8.GetBytes(name));

            _color = new WriteableBitmap(1, 1, 1, 1, PixelFormats.Rgb24, null);
            _color.WritePixels(new Int32Rect(0, 0, 1, 1), new byte[] { c[0], c[1], c[2] }, 3, 0);

            ClientCamera.Source = _color;
        }

        public void CameraOn()
        {
            IsCameraWorking = true;
        }
        public void CameraOff()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ClientCamera.Source = _color;
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
