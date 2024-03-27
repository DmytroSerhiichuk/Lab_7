using Lab_7_Client.Utils;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace Lab_7_Client
{
    internal class Client
    {
        public static event Action? Connected;
        public static event Action? ClientListUpdated;
        public static event Action<MeetingParticipant>? AnotherCameraStarted;
        public static event Action<MeetingParticipant>? AnotherCameraUpdated;
        public static event Action<MeetingParticipant>? AnotherCameraStopped;
        public static event Action<MeetingParticipant>? AnotherAudioStarted;
        public static event Action<MeetingParticipant>? AnotherAudioStopped;
        public static event Action<bool>? ShareResultReceived;
        public static event Action? AnotherShareStarted;
        public static event Action<MeetingParticipant> AnotherShareUpdated;
        public static event Action? AnotherShareStopped;
        public static event Action<string, string, string>? MessageReceived;
        public static event Action<string, long, string, string, string>? FileUploaded;
        public static event Action<string, byte[]>? FilePartDownloaded;


        public static readonly string ScheduleFile = ".\\.schedule.txt";

        public static readonly IPEndPoint REMOTE_END_POINT;
        public static UdpClient Instance { get; private set; }
        public static string LocalName { get; private set; }
        public static readonly IPEndPoint LocalIpEndPoint;
        private static long _serverLastPong;

        public static long MeetingId { get; private set; }
        public static List<MeetingParticipant> Participants { get; private set; }

        static Client()
        {
            if (!File.Exists(ScheduleFile))
            {
                File.Create(ScheduleFile);
            }

            Instance = new UdpClient();
            REMOTE_END_POINT = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3000);

            Instance.Send(new byte[0], 0, REMOTE_END_POINT);

            var port = ((IPEndPoint)Instance.Client.LocalEndPoint).Port;
            LocalIpEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
            Instance = new UdpClient(LocalIpEndPoint);

            Task.Run(() => ListenServer());

            _serverLastPong = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            Task.Run(() => PingRemote());
        }

        private static async void PingRemote()
        {
            try
            {
                byte[] data;

                using (var ms = new MemoryStream(8))
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write("PING");
                    data = ms.ToArray();
                }

                while (true)
                {
                    if (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - _serverLastPong > 21000)
                    {
                        MessageBox.Show("Server is not responding", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(0);
                    }
                    else
                    {
                        await Instance.SendAsync(data, data.Length, REMOTE_END_POINT);
                    }

                    await Task.Delay(4000);
                }
            }
            catch { }
        }

        private static async void ListenServer()
        {
            while (true)
            {
                try
                {
                    var response = await Instance.ReceiveAsync();

                    using (var ms = new MemoryStream(response.Buffer))
                    using (var br = new BinaryReader(ms))
                    {
                        var method = br.ReadString();

                        if (method == "CONNECTED")
                        {
                            MeetingId = br.ReadInt64();

                            Participants = new List<MeetingParticipant>() { new MeetingParticipant(LocalName, LocalIpEndPoint) };

                            Connected?.Invoke();
                        }
                        else if (method == "CONNECT_ERROR")
                        {
                            MessageBox.Show("Meeting not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else if (method == "UPDATE_LIST_ONCONNECT")
                        {
                            Participants.Clear();

                            var isScreenShared = br.ReadBoolean();
                            var clientsCount = br.ReadInt32();

                            for (var i = 0; i < clientsCount; i++)
                            {
                                var name = br.ReadString();
                                var address = br.ReadBytes(sizeof(int));
                                var port = br.ReadInt32();
                                var isUsingCamera = br.ReadBoolean();
                                var isUsingAudio = br.ReadBoolean();

                                var newParticipant = new MeetingParticipant(name, new IPEndPoint(new IPAddress(address), port));
                                newParticipant.UpdateCamera(isUsingCamera);
                                newParticipant.UpdateAudio(isUsingAudio);
                                Participants.Add(newParticipant);
                            }

                            App.Current.Dispatcher.Invoke(() =>
                            {
                                ClientListUpdated?.Invoke();
                            });

                            if (isScreenShared)
                            {
                                AnotherShareStarted?.Invoke();
                            }

                            for (var i = 0; i < Participants.Count; i++)
                            {
                                if (Participants[i].IsUsingCamera)
                                {
                                    AnotherCameraStarted?.Invoke(Participants[i]);
                                }
                                if (Participants[i].IsUsingAudio)
                                {
                                    AnotherAudioStarted?.Invoke(Participants[i]);
                                }
                            }
                        }
                        else if (method == "UPDATE_LIST")
                        {
                            Participants.Clear();

                            var clientsCount = br.ReadInt32();

                            for (var i = 0; i < clientsCount; i++)
                            {
                                var name = br.ReadString();
                                var address = br.ReadBytes(sizeof(int));
                                var port = br.ReadInt32();

                                Participants.Add(new MeetingParticipant(name, new IPEndPoint(new IPAddress(address), port)));
                            }

                            App.Current.Dispatcher.Invoke(() =>
                            {
                                ClientListUpdated?.Invoke();
                            });
                        }
                        else if (method == "PING")
                        {
                            using (var ping_ms = new MemoryStream(8))
                            using (var bw = new BinaryWriter(ping_ms))
                            {
                                bw.Write("PONG");

                                Instance.Send(ping_ms.ToArray(), (int)ping_ms.Length, REMOTE_END_POINT);
                            }
                        }
                        else if (method == "PONG")
                        {
                            _serverLastPong = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        }
                        else if (method == "CAMERA_START")
                        {
                            var address = br.ReadBytes(sizeof(int));
                            var port = br.ReadInt32();
                            var sender = new IPEndPoint(new IPAddress(address), port);

                            var client = Participants.Find(c => Equals(c.IpEndPoint, sender));
                            client.UpdateCamera(true);

                            AnotherCameraStarted?.Invoke(client);
                        }
                        else if (method == "FRAME_FIRST")
                        {
                            var address = br.ReadBytes(sizeof(int));
                            var port = br.ReadInt32();

                            var sender = new IPEndPoint(new IPAddress(address), port);

                            var frameLength = br.ReadInt32();
                            var chunkIndex = br.ReadInt32();
                            var chunkSize = br.ReadInt32();
                            var chunkData = br.ReadBytes(chunkSize);

                            var client = Participants.Find(c => Equals(c.IpEndPoint, sender));

                            client.CreateFrame(frameLength);
                            client.AddFrameData(chunkData, chunkIndex);
                        }
                        else if (method == "FRAME")
                        {
                            var address = br.ReadBytes(sizeof(int));
                            var port = br.ReadInt32();

                            var sender = new IPEndPoint(new IPAddress(address), port);

                            var chunkIndex = br.ReadInt32();
                            var chunkSize = br.ReadInt32();
                            var chunkData = br.ReadBytes(chunkSize);

                            var client = Participants.Find(c => Equals(c.IpEndPoint, sender));

                            client.AddFrameData(chunkData, chunkIndex);
                        }
                        else if (method == "FRAME_LAST")
                        {
                            var address = br.ReadBytes(sizeof(int));
                            var port = br.ReadInt32();

                            var sender = new IPEndPoint(new IPAddress(address), port);

                            var chunkIndex = br.ReadInt32();
                            var chunkSize = br.ReadInt32();
                            var chunkData = br.ReadBytes(chunkSize);

                            var client = Participants.Find(c => Equals(c.IpEndPoint, sender));

                            client.AddFrameData(chunkData, chunkIndex);

                            AnotherCameraUpdated?.Invoke(client);
                        }
                        else if (method == "CAMERA_END")
                        {
                            var address = br.ReadBytes(sizeof(int));
                            var port = br.ReadInt32();

                            var sender = new IPEndPoint(new IPAddress(address), port);

                            var client = Participants.Find(c => Equals(c.IpEndPoint, sender));
                            client.UpdateCamera(false);

                            AnotherCameraStopped?.Invoke(client);
                        }
                        else if (method == "AUDIO_START")
                        {
                            var address = br.ReadBytes(sizeof(int));
                            var port = br.ReadInt32();

                            var sender = new IPEndPoint(new IPAddress(address), port);

                            var client = Participants.Find(c => Equals(c.IpEndPoint, sender));
                            client.UpdateAudio(true);

                            AnotherAudioStarted?.Invoke(client);
                        }
                        else if (method == "AUDIO")
                        {
                            var bufferSize = br.ReadInt32();
                            var buffer = br.ReadBytes(bufferSize);

                            AudioPlayer.Instanse.Play(buffer);

                        }
                        else if (method == "AUDIO_END")
                        {
                            var address = br.ReadBytes(sizeof(int));
                            var port = br.ReadInt32();

                            var client = Participants.Find(c => Equals(c.IpEndPoint.Port, port));
                            client.UpdateAudio(false);

                            AnotherAudioStopped?.Invoke(client);

                            AudioPlayer.Instanse.Stop();
                        }
                        else if (method == "SHARE_RESULT")
                        {
                            var res = br.ReadBoolean();

                            ShareResultReceived?.Invoke(res);
                        }
                        else if (method == "SHARE_START")
                        {
                            AnotherShareStarted?.Invoke();
                        }
                        else if (method == "SHARE_FRAME_FIRST")
                        {
                            var address = br.ReadBytes(sizeof(int));
                            var port = br.ReadInt32();

                            var sender = new IPEndPoint(new IPAddress(address), port);

                            var frameLength = br.ReadInt32();
                            var chunkIndex = br.ReadInt32();
                            var chunkSize = br.ReadInt32();
                            var chunkData = br.ReadBytes(chunkSize);

                            var client = Participants.Find(c => Equals(c.IpEndPoint, sender));

                            client.CreateShareFrame(frameLength);
                            client.AddShareFrameData(chunkData, chunkIndex);
                        }
                        else if (method == "SHARE_FRAME")
                        {
                            var address = br.ReadBytes(sizeof(int));
                            var port = br.ReadInt32();

                            var sender = new IPEndPoint(new IPAddress(address), port);

                            var chunkIndex = br.ReadInt32();
                            var chunkSize = br.ReadInt32();
                            var chunkData = br.ReadBytes(chunkSize);

                            var client = Participants.Find(c => Equals(c.IpEndPoint, sender));

                            client.AddShareFrameData(chunkData, chunkIndex);
                        }
                        else if (method == "SHARE_FRAME_LAST")
                        {
                            var address = br.ReadBytes(sizeof(int));
                            var port = br.ReadInt32();

                            var sender = new IPEndPoint(new IPAddress(address), port);

                            var chunkIndex = br.ReadInt32();
                            var chunkSize = br.ReadInt32();
                            var chunkData = br.ReadBytes(chunkSize);

                            var client = Participants.Find(c => Equals(c.IpEndPoint, sender));

                            client.AddShareFrameData(chunkData, chunkIndex);

                            AnotherShareUpdated?.Invoke(client);
                        }
                        else if (method == "SHARE_END")
                        {
                            AnotherShareStopped?.Invoke();
                        }
                        else if (method == "CHAT")
                        {
                            var name = br.ReadString();
                            var message = br.ReadString();

                            MessageReceived?.Invoke(name, message, "You");
                        }
                        else if (method == "CHAT_ALL")
                        {
                            var name = br.ReadString();
                            var message = br.ReadString();

                            MessageReceived?.Invoke(name, message, "Everyone");
                        }
                        else if (method == "FILE_UPLOADED")
                        {
                            var senderName = br.ReadString();
                            var fileName = br.ReadString();
                            var fileSize = br.ReadInt64();
                            var idName = br.ReadString();

                            FileUploaded?.Invoke(senderName, fileSize, fileName, idName, "You");
                        }
                        else if (method == "FILE_UPLOADED_ALL")
                        {
                            var senderName = br.ReadString();
                            var fileName = br.ReadString();
                            var fileSize = br.ReadInt64();
                            var idName = br.ReadString();

                            FileUploaded?.Invoke(senderName, fileSize, fileName, idName, "Everyone");
                        }
                        else if (method == "DOWNLOAD_FILE")
                        {
                            var fileId = br.ReadString();
                            var dataSize = br.ReadInt32();
                            var data = br.ReadBytes(dataSize);

                            FilePartDownloaded?.Invoke(fileId, data);
                        }
                    }
                }
                catch { }
            }
        }


        public static async void CreateMeeting(string name)
        {
            LocalName = name;

            var method = "CREATE";

            using (var ms = new MemoryStream(16))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(method);        // Method Type
                bw.Write(name);          // Name

                await Instance.SendAsync(ms.ToArray(), (int)ms.Length, REMOTE_END_POINT);
            }
        }
        public static async void Connect(string name, long meetingId)
        {
            LocalName = name;

            var method = "CONNECT";

            using (var ms = new MemoryStream(24))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(method);        // Method Type
                bw.Write(meetingId);            // Meeting MeetingId
                bw.Write(name);          // Name

                await Instance.SendAsync(ms.ToArray(), (int)ms.Length, REMOTE_END_POINT);
            }
        }

        public static void Disconnect()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write("DISCONNECT");
                bw.Write(MeetingId);

                Instance.Send(ms.ToArray(), (int)ms.Length, REMOTE_END_POINT);
            }

            Connected = null;
            ClientListUpdated = null;
            AnotherCameraStarted = null;
            AnotherCameraUpdated = null;
            AnotherCameraStopped = null;
            AnotherAudioStarted = null;
            AnotherAudioStopped = null;
            ShareResultReceived = null;
            AnotherShareStarted = null;
            AnotherShareUpdated = null;
            AnotherShareStopped = null;
            MessageReceived = null;
            FileUploaded = null;
            FilePartDownloaded = null;


            ProgramManager.Instance.Navigate(PageType.MainPage);
        }

        public static void SendCameraStarted()
        {
            using (var ms = new MemoryStream(16))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write("CAMERA_START");
                bw.Write(MeetingId);

                Instance.Send(ms.ToArray(), (int)ms.Length, REMOTE_END_POINT);
            }
        }
        public static void SendCameraFrame(Bitmap bitmap)
        {
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Jpeg);

                byte[] imageData = memoryStream.ToArray();

                int bufferSize = 32768;
                int chunks = (int)Math.Ceiling((double)imageData.Length / bufferSize);

                for (var i = 0; i < chunks; i++)
                {
                    using (var ms2 = new MemoryStream(new byte[32900]))
                    using (var bw = new BinaryWriter(ms2))
                    {
                        if (i == 0)
                        {
                            bw.Write("FRAME_FIRST");    // HEADER
                            bw.Write(imageData.Length); // Frame Size
                        }
                        else if (i + 1 == chunks)
                        {
                            bw.Write("FRAME_LAST");     // HEADER
                        }
                        else
                        {
                            bw.Write("FRAME");          // HEADER
                        }

                        bw.Write(MeetingId);                   // Meeting MeetingId
                        bw.Write(i);                    // Chunk Index

                        int bytesToSend = Math.Min(bufferSize, imageData.Length - i * bufferSize);
                        byte[] chunkData = new byte[bytesToSend];
                        Buffer.BlockCopy(imageData, i * bufferSize, chunkData, 0, bytesToSend);

                        bw.Write(bytesToSend);          // Chunk Size
                        bw.Write(chunkData);            // Chunk Data

                        Instance.Send(ms2.ToArray(), (int)ms2.Length, REMOTE_END_POINT);
                    }
                }
            }
        }
        public static void SendCameraEnded()
        {
            using (var ms = new MemoryStream(8))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write("CAMERA_END");
                bw.Write(MeetingId);

                Instance.Send(ms.ToArray(), (int)ms.Length, REMOTE_END_POINT);
            }
        }

        public static void SendAudioStarted()
        {
            using (var ms = new MemoryStream(16))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write("AUDIO_START");
                bw.Write(MeetingId);

                Instance.Send(ms.ToArray(), (int)ms.Length, REMOTE_END_POINT);
            }
        }
        public static void SendAudio(byte[] buffer, int bytesRecorded)
        {
            using (var ms = new MemoryStream(bytesRecorded + 20))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write("AUDIO");
                bw.Write(MeetingId);
                bw.Write(bytesRecorded);
                bw.Write(buffer);

                Instance.Send(ms.ToArray(), (int)ms.Length, REMOTE_END_POINT);
            }
        }
        public static void SendAudioEnded()
        {
            using (var ms = new MemoryStream(16))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write("AUDIO_END");
                bw.Write(MeetingId);

                Instance.Send(ms.ToArray(), (int)ms.Length, REMOTE_END_POINT);
            }
        }

        public static void SendShareStart()
        {
            using (var ms = new MemoryStream(16))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write("SHARE_START");
                bw.Write(MeetingId);

                Instance.Send(ms.ToArray(), (int)ms.Length, REMOTE_END_POINT);
            }
        }
        public static void SendShareFrame(Bitmap bitmap)
        {
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Jpeg);

                byte[] imageData = memoryStream.ToArray();

                int bufferSize = 32768;
                int chunks = (int)Math.Ceiling((double)imageData.Length / bufferSize);

                for (var i = 0; i < chunks; i++)
                {
                    using (var ms2 = new MemoryStream(new byte[32900]))
                    using (var bw = new BinaryWriter(ms2))
                    {
                        if (i == 0)
                        {
                            bw.Write("SHARE_FRAME_FIRST");
                            bw.Write(imageData.Length);
                        }
                        else if (i + 1 == chunks)
                        {
                            bw.Write("SHARE_FRAME_LAST");
                        }
                        else
                        {
                            bw.Write("SHARE_FRAME");
                        }

                        bw.Write(MeetingId);
                        bw.Write(i);

                        int bytesToSend = Math.Min(bufferSize, imageData.Length - i * bufferSize);
                        byte[] chunkData = new byte[bytesToSend];
                        Buffer.BlockCopy(imageData, i * bufferSize, chunkData, 0, bytesToSend);

                        bw.Write(bytesToSend);
                        bw.Write(chunkData);

                        Instance.Send(ms2.ToArray(), (int)ms2.Length, REMOTE_END_POINT);
                    }
                }
            }
        }
        public static void SendShareEnd()
        {
            using (var ms = new MemoryStream(16))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write("SHARE_END");
                bw.Write(MeetingId);

                Instance.Send(ms.ToArray(), (int)ms.Length, REMOTE_END_POINT);
            }
        }

        public static void SendMessage(string message, IPEndPoint receiver)
        {
            using (var ms = new MemoryStream(32))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write("CHAT");
                bw.Write(MeetingId);
                bw.Write(LocalName);
                bw.Write(receiver.Address.GetAddressBytes());
                bw.Write(receiver.Port);
                bw.Write(message);

                Instance.Send(ms.ToArray(), (int)ms.Length, REMOTE_END_POINT);
            }
        }
        public static void SendMessageToAll(string message)
        {
            using (var ms = new MemoryStream(32))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write("CHAT_ALL");
                bw.Write(LocalName);
                bw.Write(MeetingId);
                bw.Write(message);

                Instance.Send(ms.ToArray(), (int)ms.Length, REMOTE_END_POINT);
            }
        }

        public static void SendFileDelete(string idName)
        {
            using (var ms = new MemoryStream(16))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write("FILE_DELETE");
                bw.Write(MeetingId);
                bw.Write(idName);

                Instance.Send(ms.ToArray(), (int)ms.Length, REMOTE_END_POINT);
            }
        }
        public static bool SendFile(string path, string idName, IPEndPoint receiver, CancellationToken token)
        {
            using (var file = File.OpenRead(path))
            {
                int bufferSize = 32768;
                int chunks = (int)Math.Ceiling((double)file.Length / bufferSize);

                for (var i = 0; i < chunks && !token.IsCancellationRequested; i++)
                {
                    using (var ms2 = new MemoryStream(new byte[32900]))
                    using (var bw = new BinaryWriter(ms2))
                    {
                        if (i + 1 == chunks)
                        {
                            bw.Write("FILE_LAST");
                            bw.Write(Path.GetFileName(file.Name));
                            bw.Write(LocalName);
                            bw.Write(receiver.Address.GetAddressBytes());
                            bw.Write(receiver.Port);
                        }
                        else
                        {
                            bw.Write("FILE");
                        }

                        bw.Write(MeetingId);
                        bw.Write(idName);
                        bw.Write(i * bufferSize);

                        byte[] data = new byte[bufferSize];
                        var bytesCount = file.Read(data, 0, bufferSize);

                        bw.Write(bytesCount);
                        bw.Write(data);

                        Instance.Send(ms2.ToArray(), (int)ms2.Length, REMOTE_END_POINT);
                    }
                }
                if (token.IsCancellationRequested)
                {
                    SendFileDelete(idName);
                    return true;
                }
                return true;
            }
        }
        public static bool SendFileToAll(string path, string idName, CancellationToken token)
        {
            using (var file = File.OpenRead(path))
            {
                int bufferSize = 32768;
                int chunks = (int)Math.Ceiling((double)file.Length / bufferSize);

                for (var i = 0; i < chunks && !token.IsCancellationRequested; i++)
                {
                    using (var ms2 = new MemoryStream(new byte[32900]))
                    using (var bw = new BinaryWriter(ms2))
                    {
                        if (i + 1 == chunks)
                        {
                            bw.Write("FILE_LAST_ALL");
                            bw.Write(Path.GetFileName(file.Name));
                            bw.Write(LocalName);
                        }
                        else
                        {
                            bw.Write("FILE");
                        }

                        bw.Write(MeetingId);
                        bw.Write(idName);
                        bw.Write(i * bufferSize);

                        byte[] data = new byte[bufferSize];
                        var bytesCount = file.Read(data, 0, bufferSize);

                        bw.Write(bytesCount);
                        bw.Write(data);

                        Instance.Send(ms2.ToArray(), (int)ms2.Length, REMOTE_END_POINT);
                    }
                }
                if (token.IsCancellationRequested)
                {
                    SendFileDelete(idName);
                    return true;
                }
                return true;
            }
        }

        public static void DownloadFile(string idName, long byteIndex)
        {
            using (var ms = new MemoryStream(16))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write("DOWNLOAD_FILE");
                bw.Write(MeetingId);
                bw.Write(idName);
                bw.Write(byteIndex);

                Instance.Send(ms.ToArray(), (int)ms.Length, REMOTE_END_POINT);
            }
        }

        public static void AddSchedule(DateTime date)
        {
            using (var sw = new StreamWriter(ScheduleFile, true))
            {
                sw.WriteLine($"Date: {date.Day}.{date.Month}.{date.Year}");
            }
        }
        public static List<string> GetSchedule()
        {
            var res = new List<string>();

            using (var sw = new StreamReader(ScheduleFile))
            {
                res.Add(sw.ReadLine());
            }

            return res;
        }
        public static void DeleteSchedule(string str)
        {
            var lines = File.ReadAllLines(ScheduleFile);
            var newLines = new List<string>();

            bool deleted = false;

            foreach (var line in lines)
            {
                if (line == str && !deleted)
                {
                    deleted = true;
                }
                else
                {
                    newLines.Add(line);
                }
            }

            File.WriteAllLines(ScheduleFile, newLines);
        }
    }
}
