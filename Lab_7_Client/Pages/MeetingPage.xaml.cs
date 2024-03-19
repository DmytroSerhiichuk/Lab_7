using AForge.Video;
using AForge.Video.DirectShow;
using Lab_7_Client.CustomComponents;
using Lab_7_Client.Utils;
using NAudio.Wave;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Lab_7_Client.Pages
{
    /// <summary>
    /// Interaction logic for MeetingPage.xaml
    /// </summary>
    public partial class MeetingPage : Page
    {
        private IMeetingContainer _meetingContainer;
        public WaveInEvent WaveIn { get; private set; }
        public VideoCaptureDevice VideoSource { get; private set; }
        public bool IsAudioRecording { get; set; } = false;

        public List<MeetingParticipantContainer> ParticipantsContainers { get; private set; }

        public MeetingPage()
        {
            InitializeComponent();

            MyTextBox.Text = $"Meeting ID: {Client.MeetingId}";

            ParticipantsContainers = new List<MeetingParticipantContainer>();

            _meetingContainer = new MainMeetingContainer(ParticipantsContainers);
            _meetingContainer.UpdateContainers();
            MeetingContainerBorder.Child = (UIElement)_meetingContainer;

            Client.ClientListUpdated += OnClientListUpdated;

            Client.AnotherCameraStarted += OnAnotherCameraStarted;
            Client.AnotherCameraUpdated += OnAnotherCameraUpdated;
            Client.AnotherCameraStopped += OnAnotherCameraStopped;

            Client.AnotherAudioStarted += OnAnotherAudioStarted;
            Client.AnotherAudioStopped += OnAnotherAudioStopped;

            Client.ShareResultReceived += OnShareResultReceived;
            Client.AnotherShareStarted += OnAnotherShareStarted;
            Client.AnotherShareStopped += OnAnotherShareStopped;
            Client.AnotherShareUpdated += OnAnotherShareUpdated;

            // Camera
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            VideoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
            VideoSource.NewFrame += OnNewFrame;

            // Microphone
            WaveIn = new WaveInEvent();
            WaveIn.BufferMilliseconds = 600;
            WaveIn.WaveFormat = new WaveFormat(44100, 16, 1);
            WaveIn.DataAvailable += OnWaveDataAvaible;
        }

        private void OnClientListUpdated()
        {
            _meetingContainer.UpdateContainers();
        }

        private void OnAnotherCameraStarted(MeetingParticipant participant)
        {
            var participantContainer = FindParticipantContainer(participant);
            participantContainer.CameraOn();
        }
        private void OnAnotherCameraUpdated(MeetingParticipant participant)
        {
            var participantContainer = FindParticipantContainer(participant);
            participantContainer.UpdateCamera(participant.GetFrame());
        }
        private void OnAnotherCameraStopped(MeetingParticipant participant)
        {
            var participantContainer = FindParticipantContainer(participant);
            participantContainer.CameraOff();
        }
        private void OnCameraButtonClicked(object sender, RoutedEventArgs e)
        {
            if (VideoSource != null)
            {
                if (VideoSource.IsRunning)
                {
                    CameraOff();
                }
                else
                {
                    CameraOn();
                }
            }
        }
        public void CameraOn()
        {
            ParticipantsContainers[0].CameraOn();
            Client.SendCameraStarted();
            VideoSource.Start();
        }
        public void CameraOff()
        {
            try
            {
                VideoSource.SignalToStop();
                VideoSource.Stop();
            }
            catch { }
            finally
            {
                ParticipantsContainers[0].CameraOff();
                Client.SendCameraEnded();
            }
        }
        private void OnNewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            var bitmap = (Bitmap)eventArgs.Frame.Clone();

            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Jpeg);
                memory.Position = 0;

                App.Current.Dispatcher.Invoke(() =>
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();

                    ParticipantsContainers[0].UpdateCamera(bitmapImage);
                });

                // Send video frame to server
                Client.SendCameraFrame(bitmap);
            }
        }


        private void OnAnotherAudioStarted(MeetingParticipant participant)
        {
            var participantContainer = FindParticipantContainer(participant);
            participantContainer.AudioOn();
        }
        private void OnAnotherAudioStopped(MeetingParticipant participant)
        {
            var participantContainer = FindParticipantContainer(participant);
            participantContainer.AudioOff();
        }
        private void OnMicrophoneButtonClicked(object sender, RoutedEventArgs e)
        {
            if (IsAudioRecording)
            {
                AudioOff();
            }
            else
            {
                AudioOn();
            }
            IsAudioRecording = !IsAudioRecording;
        }
        public void AudioOn()
        {
            WaveIn.StartRecording();
            ParticipantsContainers[0].AudioOn();
            Client.SendAudioStarted();
        }
        public void AudioOff()
        {
            WaveIn.StopRecording();
            ParticipantsContainers[0].AudioOff();
            Client.SendAudioEnded();
        }
        private void OnWaveDataAvaible(object? sender, WaveInEventArgs e)
        {
            Client.SendAudio(e.Buffer, e.BytesRecorded);
        }

        private void OnRecordButtonClicked(object sender, RoutedEventArgs e)
        {

        }

        private void OnShareScreen(object sender, RoutedEventArgs e)
        {
            Client.SendShareStart();
        }
        private void OnShareResultReceived(bool res)
        {
            if (res)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    ProgramManager.Instance.StartShare(this);
                });
            }
        }
        private void OnAnotherShareStarted()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _meetingContainer.Clear();
                _meetingContainer = new ScreenShareMeetingContainer(ParticipantsContainers);
                MeetingContainerBorder.Child = (UIElement)_meetingContainer;
            });
            
        }
        private void OnAnotherShareUpdated(MeetingParticipant participant)
        {
            if (_meetingContainer is ScreenShareMeetingContainer)
            {
                var mc = _meetingContainer as ScreenShareMeetingContainer;
                mc.UpdateShareScreen(participant.GetShareFrame());
            }
        }
        private void OnAnotherShareStopped()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _meetingContainer.Clear();
                _meetingContainer = new MainMeetingContainer(ParticipantsContainers);
                MeetingContainerBorder.Child = (UIElement)_meetingContainer;
            });
        }


        public void Close()
        {
            try
            {
                VideoSource.SignalToStop();
                VideoSource.Stop();
            }
            catch { }
        }

        private void OnDisconnect(object sender, RoutedEventArgs e)
        {
            Client.Disconnect();

            WaveIn.StopRecording();
            ParticipantsContainers[0].AudioOff();
            Client.SendAudioEnded();

            Close();

            WaveIn = null;
            VideoSource = null;
        }

        private MeetingParticipantContainer FindParticipantContainer(MeetingParticipant participant)
        {
            foreach (var item in ParticipantsContainers)
            {
                if (Equals(item.ClientIpEP, participant.IpEndPoint))
                {
                    return item;
                }
            }
            return null;
        }
    }
}
