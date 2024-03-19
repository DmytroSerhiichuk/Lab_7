using AForge.Video;
using Lab_7_Client.Utils;
using System.Windows;

namespace Lab_7_Client.Pages
{
    /// <summary>
    /// Interaction logic for ShareScreen.xaml
    /// </summary>
    public partial class ShareScreen : Window
    {
        private CancellationTokenSource _cts;
        private MeetingPage meetingPage;

        public ShareScreen(MeetingPage page)
        {
            InitializeComponent();

            meetingPage = page;

            _cts = new CancellationTokenSource();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var token = _cts.Token;
            Task.Run(() => GetFrames(token), token);
        }
        private void GetFrames(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var frame = ScreenRecorder.GetFullScreen((int)ActualWidth, (int)ActualHeight);

                Client.SendShareFrame(frame);
            }
        }

        private void OnCloseButtonClicked(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
            App.Current.MainWindow.Close();
            Environment.Exit(0);
        }

        private void OnAudioButtonClicked(object sender, RoutedEventArgs e)
        {
            if (meetingPage.IsAudioRecording)
            {
                meetingPage.AudioOff();
            }
            else
            {
                meetingPage.AudioOn();
            }
            meetingPage.IsAudioRecording = !meetingPage.IsAudioRecording;
        }

        private void OnCameraButtonClicked(object sender, RoutedEventArgs e)
        {
            if (meetingPage.VideoSource != null)
            {
                if (meetingPage.VideoSource.IsRunning)
                {
                    meetingPage.CameraOff();
                }
                else
                {
                    meetingPage.CameraOn();
                }
            }
        }

        private void OnStopShareClicked(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
            Client.SendShareEnd();
            ProgramManager.Instance.StopShare();
            Close();
        }

        private void OnRecordButtonClicked(object sender, RoutedEventArgs e)
        {

        }
    }
}
