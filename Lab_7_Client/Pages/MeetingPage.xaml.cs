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
        public WaveInEvent WaveIn { get; private set; }
        public VideoCaptureDevice VideoSource { get; private set; }
        public bool IsAudioRecording { get; private set; } = false;

        public List<MeetingClientContainer> ClientsContainers { get; private set; }

        public MeetingPage()
        {
            InitializeComponent();

            MyTextBox.Text = $"Meeting ID: {Client.MeetingId}";

            ClientsContainers = new List<MeetingClientContainer>();

            Client.ClientListUpdated += OnClientListUpdated;

            Client.AnotherCameraStarted += OnAnotherCameraStarted;
            Client.AnotherCameraUpdated += OnAnotherCameraUpdated;
            Client.AnotherCameraClosed += OnAnotherCameraClosed;

            Client.AnotherAudioStarted += OnAnotherAudioStarted;
            Client.AnotherAudioStopped += OnAnotherAudioStopped;

            OnClientListUpdated();

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
            for (var i = 0; i < ClientsContainers.Count; i++)
            {
                if (!Client.Participants.Any(x => Equals(x.IpEndPoint, ClientsContainers[i].ClientIpEP)))
                {
                    ClientsContainers.RemoveAt(i);
                    ClientsList.Children.RemoveAt(i);
                    i--;
                }
            }

            for (var i = 0; i < Client.Participants.Count; i++)
            {
                if (!ClientsContainers.Any(x => Equals(x.ClientIpEP, Client.Participants[i].IpEndPoint)))
                {
                    var newClientContainer = new MeetingClientContainer(Client.Participants[i].Name, Client.Participants[i].IpEndPoint);
                    ClientsContainers.Add(newClientContainer);
                    ClientsList.Children.Add(newClientContainer);
                }
            }
        }

        private void OnAnotherCameraStarted(MeetingParticipant client)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                MeetingClientContainer meetingClientContainer = null;

                foreach (MeetingClientContainer item in ClientsList.Children)
                {
                    if (Equals(item.ClientIpEP, client.IpEndPoint))
                    {
                        meetingClientContainer = item;
                        break;
                    }
                }

                try
                {
                    meetingClientContainer.CameraOn();
                }
                catch { }
            });
        }
        private void OnAnotherCameraUpdated(MeetingParticipant client)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                MeetingClientContainer meetingClientContainer = null;

                foreach (MeetingClientContainer item in ClientsList.Children)
                {
                    if (Equals(item.ClientIpEP, client.IpEndPoint))
                    {
                        meetingClientContainer = item;
                        break;
                    }
                }

                try
                {
                    meetingClientContainer.UpdateCamera(client.GetFrame());
                }
                catch { }
            });
        }
        private void OnAnotherCameraClosed(MeetingParticipant client)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                MeetingClientContainer meetingClientContainer = null;

                foreach (MeetingClientContainer item in ClientsList.Children)
                {
                    if (Equals(item.ClientIpEP, client.IpEndPoint))
                    {
                        meetingClientContainer = item;
                        break;
                    }
                }

                meetingClientContainer.CameraOff();
            });
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
            ((MeetingClientContainer)ClientsList.Children[0]).CameraOn();
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
                ((MeetingClientContainer)ClientsList.Children[0]).CameraOff();
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

                    var meetingClientContainer = ClientsList.Children[0] as MeetingClientContainer;
                    meetingClientContainer.UpdateCamera(bitmapImage);
                });

                // Send video frame to server
                Client.SendCameraFrame(bitmap);
            }
        }


        private void OnAnotherAudioStopped(MeetingParticipant client)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                MeetingClientContainer meetingClientContainer = null;

                foreach (MeetingClientContainer item in ClientsList.Children)
                {
                    if (Equals(item.ClientIpEP, client.IpEndPoint))
                    {
                        meetingClientContainer = item;
                        break;
                    }
                }

                meetingClientContainer.AudioOff();
            });
        }
        private void OnAnotherAudioStarted(MeetingParticipant client)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                MeetingClientContainer meetingClientContainer = null;

                foreach (MeetingClientContainer item in ClientsList.Children)
                {
                    if (Equals(item.ClientIpEP, client.IpEndPoint))
                    {
                        meetingClientContainer = item;
                        break;
                    }
                }

                meetingClientContainer.AudioOn();
            });
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
            ((MeetingClientContainer)ClientsList.Children[0]).AudioOn();
            Client.SendAudioStarted();
        }
        public void AudioOff()
        {
            WaveIn.StopRecording();
            ((MeetingClientContainer)ClientsList.Children[0]).AudioOff();
            Client.SendAudioEnded();
        }
        private void OnWaveDataAvaible(object? sender, WaveInEventArgs e)
        {
            Client.SendAudio(e.Buffer, e.BytesRecorded);
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
            ((MeetingClientContainer)ClientsList.Children[0]).AudioOff();
            Client.SendAudioEnded();

            Close();

            WaveIn = null;
            VideoSource = null;
        }


        private void OnRecordButtonClicked(object sender, RoutedEventArgs e)
        {

        }

        private void OnShareScreen(object sender, RoutedEventArgs e)
        {
            ProgramManager.Instance.StartShare(this);
        }
    }
}
