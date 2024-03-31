using System.Net;
using System.Net.Sockets;
using System.IO;

namespace Lab_7_Server
{
    internal class Server
    {
        private static object _locker = new object();
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        public static bool IsWorking { get; private set; }

        public static UdpServer Instance { get; private set; }
        public static List<MeetingData> Meetings { get; private set; }

        static Server()
        {
            Instance = new UdpServer(IPAddress.Parse("192.168.31.82"), 3000);
            Meetings = new List<MeetingData>();
        }

        public static bool Start()
        {
            try
            {
                FileManager.ClearTempFolder();

                Instance.Start();

                IsWorking = true;

                var token = _cancellationTokenSource.Token;

                Task.Run(() => HandleRequests(token), token);
                Task.Run(() => PingClients(token), token);

                return true;
            }
            catch 
            { 
                IsWorking = false;
                return false;
            }
        }
        public static bool Stop()
        {
            try
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
                Instance.Close();
                IsWorking = false;
                return true;
            }
            catch
            {
                IsWorking = true;
                return false;
            }
        }

        private static async void HandleRequests(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var res = await Instance.ReceiveAsync(token);

                    Instance.HandleRequest(res, ProcessRequest);
                }
                catch { }
            }
        }

        private static async void ProcessRequest(UdpReceiveResult request)
        {
            try
            {
                var clientEP = request.RemoteEndPoint;

                using (var ms = new MemoryStream(request.Buffer))
                using (var br = new BinaryReader(ms))
                {
                    var method = br.ReadString();

                    if (method == "CREATE")
                    {
                        var name = br.ReadString();

                        var meeting = new MeetingData();
                        Meetings.Add(meeting);

                        FileManager.CreateMeetingCatalog(meeting.Id);

                        meeting.AddClient(new MeetingUser(clientEP, name));

                        using (var response_ms = new MemoryStream(16))
                        using (var bw = new BinaryWriter(response_ms))
                        {
                            bw.Write("CONNECTED");
                            bw.Write(meeting.Id);
                            bw.Write(clientEP.Address.GetAddressBytes());
                            bw.Write(clientEP.Port);

                            await Instance.SendAsync(response_ms.ToArray(), clientEP);
                        }
                    }
                    else if (method == "CONNECT")
                    {
                        var meetingId = br.ReadInt64();
                        var name = br.ReadString();

                        var meeting = Meetings.Find(x => x.Id == meetingId);

                        if (meeting != null)
                        {
                            meeting.AddClient(new MeetingUser(clientEP, name));

                            using (var response_ms = new MemoryStream(8))
                            using (var bw = new BinaryWriter(response_ms))
                            {
                                bw.Write("CONNECTED");
                                bw.Write(meeting.Id);
                                bw.Write(clientEP.Address.GetAddressBytes());
                                bw.Write(clientEP.Port);

                                await Instance.SendAsync(response_ms.ToArray(), clientEP);
                            }

                            using (var response_ms = new MemoryStream(64))
                            using (var bw = new BinaryWriter(response_ms))
                            {
                                bw.Write("UPDATE_LIST_ONCONNECT");  // Header
                                bw.Write(meeting.IsScreenShared);
                                bw.Write(meeting.Clients.Count);    // Clients Count

                                for (var i = 0; i < meeting.Clients.Count; i++)
                                {
                                    bw.Write(meeting.Clients[i].Name);                                      // Client Name
                                    bw.Write(meeting.Clients[i].IpEndPoint.Address.GetAddressBytes());      // Client Address (4 bytes)
                                    bw.Write(meeting.Clients[i].IpEndPoint.Port);                           // Client Port
                                    bw.Write(meeting.Clients[i].IsUsingCamera);
                                    bw.Write(meeting.Clients[i].IsUsingAudio);
                                }

                                await Instance.SendAsync(response_ms.ToArray(), clientEP);
                            }

                            using (var broadcast_ms = new MemoryStream(64))
                            using (var bw = new BinaryWriter(broadcast_ms))
                            {
                                bw.Write("UPDATE_LIST");            // Header
                                bw.Write(meeting.Clients.Count);    // Clients Count

                                for (var i = 0; i < meeting.Clients.Count; i++)
                                {
                                    bw.Write(meeting.Clients[i].Name);                                      // Client Name
                                    bw.Write(meeting.Clients[i].IpEndPoint.Address.GetAddressBytes());      // Client Address (4 bytes)
                                    bw.Write(meeting.Clients[i].IpEndPoint.Port);                           // Client Port
                                }

                                await Instance.BroadcastAsync(broadcast_ms.ToArray(), meeting.Clients.Select(x => x.IpEndPoint).ToArray(), clientEP);
                            }
                        }
                        else
                        {
                            using (var response_ms = new MemoryStream(8))
                            using (var bw = new BinaryWriter(response_ms))
                            {
                                bw.Write("CONNECT_ERROR");

                                await Instance.SendAsync(response_ms.ToArray(), clientEP);
                            }
                        }
                    }
                    else if (method == "DISCONNECT")
                    {
                        var meetingId = br.ReadInt64();

                        var meeting = Meetings.Find(x => x.Id == meetingId);

                        var sender = meeting.Clients.Find(x => Equals(x.IpEndPoint, clientEP));
                        meeting.Clients.Remove(sender);

                        if (meeting.Clients.Count == 0)
                        {
                            FileManager.DeleteMeetingCatalog(meetingId);
                            Meetings.Remove(meeting);
                        }
                        else
                        {
                            using (var response_ms = new MemoryStream())
                            using (var bw = new BinaryWriter(response_ms))
                            {
                                bw.Write("UPDATE_LIST");            // Header
                                bw.Write(meeting.Clients.Count);    // Clients Count

                                for (var i = 0; i < meeting.Clients.Count; i++)
                                {
                                    bw.Write(meeting.Clients[i].Name);                                      // Client Name
                                    bw.Write(meeting.Clients[i].IpEndPoint.Address.GetAddressBytes());      // Client Address (4 bytes)
                                    bw.Write(meeting.Clients[i].IpEndPoint.Port);                           // Client Port
                                }

                                await Instance.BroadcastAsync(response_ms.ToArray(), meeting.Clients.Select(x => x.IpEndPoint).ToArray(), clientEP);
                            }
                        }
                    }
                    else if (method == "PING")
                    {
                        using (var response_ms = new MemoryStream(8))
                        using (var bw = new BinaryWriter(response_ms))
                        {
                            bw.Write("PONG");

                            await Instance.SendAsync(response_ms.ToArray(), clientEP);
                        }
                    }
                    else if (method == "PONG")
                    {
                        foreach (var meeting in Meetings)
                        {
                            foreach (var client in meeting.Clients)
                            {
                                if (Equals(clientEP.Address, client.IpEndPoint.Address) && clientEP.Port == client.IpEndPoint.Port)
                                {
                                    client.UpdateLastPong();
                                    goto exit;
                                }
                            }
                        }
                    exit:
                        { }
                    }
                    else if (method == "CAMERA_START")
                    {
                        var meetingId = br.ReadInt64();

                        var meeting = Meetings.Find(x => x.Id == meetingId);

                        var sender = meeting.Clients.Find(x => Equals(x.IpEndPoint, clientEP));
                        sender.UpdateCamera(true);

                        using (var response_ms = new MemoryStream(16))
                        using (var bw = new BinaryWriter(response_ms))
                        {
                            bw.Write("CAMERA_START");                           // HEADER
                            bw.Write(clientEP.Address.GetAddressBytes());       // Client Address (4 bytes)
                            bw.Write(clientEP.Port);                            // Client Port

                            await Instance.BroadcastAsync(response_ms.ToArray(), meeting.Clients.Select(x => x.IpEndPoint).ToArray(), clientEP);
                        }
                    }
                    else if (method == "FRAME_FIRST")
                    {
                        var frameLength = br.ReadInt32();
                        var meetingId = br.ReadInt64();
                        var chunkIndex = br.ReadInt32();

                        var chunkSize = br.ReadInt32();
                        var chunkData = br.ReadBytes(chunkSize);

                        var meeting = Meetings.Find(x => x.Id == meetingId);

                        using (var response_ms = new MemoryStream(new byte[request.Buffer.Length + 64]))
                        using (var bw = new BinaryWriter(response_ms))
                        {
                            bw.Write("FRAME_FIRST");                            // HEADER
                            bw.Write(clientEP.Address.GetAddressBytes());       // Client Address (4 bytes)
                            bw.Write(clientEP.Port);                            // Client Port
                            bw.Write(frameLength);                              // Frame Length
                            bw.Write(chunkIndex);                               // Chunk Index
                            bw.Write(chunkSize);                                // Chunk Size
                            bw.Write(chunkData);                                // Chunk Data

                            await Instance.BroadcastAsync(response_ms.ToArray(), meeting.Clients.Select(x => x.IpEndPoint).ToArray(), clientEP);
                        }
                    }
                    else if (method == "FRAME")
                    {
                        var meetingId = br.ReadInt64();
                        var chunkIndex = br.ReadInt32();

                        var chunkSize = br.ReadInt32();
                        var chunkData = br.ReadBytes(chunkSize);

                        var meeting = Meetings.Find(x => x.Id == meetingId);

                        using (var response_ms = new MemoryStream(new byte[request.Buffer.Length + 64]))
                        using (var bw = new BinaryWriter(response_ms))
                        {
                            bw.Write("FRAME");                                  // HEADER
                            bw.Write(clientEP.Address.GetAddressBytes());       // Client Address (4 bytes)
                            bw.Write(clientEP.Port);                            // Client Port
                            bw.Write(chunkIndex);                               // Chunk Index
                            bw.Write(chunkSize);                                // Chunk Size
                            bw.Write(chunkData);                                // Chunk Data

                            await Instance.BroadcastAsync(response_ms.ToArray(), meeting.Clients.Select(x => x.IpEndPoint).ToArray(), clientEP);
                        }
                    }
                    else if (method == "FRAME_LAST")
                    {
                        var meetingId = br.ReadInt64();
                        var chunkIndex = br.ReadInt32();

                        var chunkSize = br.ReadInt32();
                        var chunkData = br.ReadBytes(chunkSize);

                        var meeting = Meetings.Find(x => x.Id == meetingId);

                        using (var response_ms = new MemoryStream(new byte[request.Buffer.Length + 64]))
                        using (var bw = new BinaryWriter(response_ms))
                        {
                            bw.Write("FRAME_LAST");                             // HEADER
                            bw.Write(clientEP.Address.GetAddressBytes());       // Client Address (4 bytes)
                            bw.Write(clientEP.Port);                            // Client Port
                            bw.Write(chunkIndex);                               // Chunk Index
                            bw.Write(chunkSize);                                // Chunk Size
                            bw.Write(chunkData);                                // Chunk Data

                            await Instance.BroadcastAsync(response_ms.ToArray(), meeting.Clients.Select(x => x.IpEndPoint).ToArray(), clientEP);
                        }
                    }
                    else if (method == "CAMERA_END")
                    {
                        var meetingId = br.ReadInt64();

                        var meeting = Meetings.Find(x => x.Id == meetingId);

                        var sender = meeting.Clients.Find(x => Equals(x.IpEndPoint, clientEP));
                        sender.UpdateCamera(false);

                        using (var response_ms = new MemoryStream())
                        using (var bw = new BinaryWriter(response_ms))
                        {
                            bw.Write("CAMERA_END");                             // HEADER
                            bw.Write(clientEP.Address.GetAddressBytes());       // Client Address (4 bytes)
                            bw.Write(clientEP.Port);                            // Client Port

                            await Instance.BroadcastAsync(response_ms.ToArray(), meeting.Clients.Select(x => x.IpEndPoint).ToArray(), clientEP);
                        }
                    }
                    else if (method == "AUDIO_START")
                    {
                        var meetingId = br.ReadInt64();

                        var meeting = Meetings.Find(x => x.Id == meetingId);

                        var sender = meeting.Clients.Find(x => Equals(x.IpEndPoint, clientEP));
                        sender.UpdateAudio(true);

                        using (var response_ms = new MemoryStream(16))
                        using (var bw = new BinaryWriter(response_ms))
                        {
                            bw.Write("AUDIO_START");                            // HEADER
                            bw.Write(clientEP.Address.GetAddressBytes());       // Client Address (4 bytes)
                            bw.Write(clientEP.Port);                            // Client Port

                            await Instance.BroadcastAsync(response_ms.ToArray(), meeting.Clients.Select(x => x.IpEndPoint).ToArray(), clientEP);
                        }
                    }
                    else if (method == "AUDIO")
                    {
                        var meetingId = br.ReadInt64();

                        var meeting = Meetings.Find(x => x.Id == meetingId);

                        var bufferSize = br.ReadInt32();
                        var buffer = br.ReadBytes(bufferSize);

                        using (var response_ms = new MemoryStream(bufferSize + 8))
                        using (var bw = new BinaryWriter(response_ms))
                        {
                            bw.Write("AUDIO");                                  // HEADER
                            bw.Write(bufferSize);                               // Buffer Size
                            bw.Write(buffer);                                   // Buffer

                            await Instance.BroadcastAsync(response_ms.ToArray(), meeting.Clients.Select(x => x.IpEndPoint).ToArray(), clientEP);
                        }
                    }
                    else if (method == "AUDIO_END")
                    {
                        var meetingId = br.ReadInt64();

                        var meeting = Meetings.Find(x => x.Id == meetingId);

                        var sender = meeting.Clients.Find(x => Equals(x.IpEndPoint, clientEP));
                        sender.UpdateAudio(false);

                        using (var response_ms = new MemoryStream(16))
                        using (var bw = new BinaryWriter(response_ms))
                        {
                            bw.Write("AUDIO_END");                              // HEADER
                            bw.Write(clientEP.Address.GetAddressBytes());       // Client Address (4 bytes)
                            bw.Write(clientEP.Port);                            // Client Port

                            await Instance.BroadcastAsync(response_ms.ToArray(), meeting.Clients.Select(x => x.IpEndPoint).ToArray(), clientEP);
                        }
                    }
                    else if (method == "SHARE_START")
                    {
                        var meetingId = br.ReadInt64();

                        var meeting = Meetings.Find(x => x.Id == meetingId);

                        if (meeting.IsScreenShared)
                        {
                            using (var response_ms = new MemoryStream(8))
                            using (var bw = new BinaryWriter(response_ms))
                            {
                                bw.Write("SHARE_RESULT");
                                bw.Write(false);

                                await Instance.SendAsync(response_ms.ToArray(), clientEP);
                            }
                        }
                        else
                        {
                            lock (_locker)
                            {
                                meeting.StartShare();

                                var sender = meeting.Clients.Find(x => Equals(x.IpEndPoint, clientEP));
                                sender.UpdateShare(true);

                                using (var response_ms = new MemoryStream(8))
                                using (var bw = new BinaryWriter(response_ms))
                                {
                                    bw.Write("SHARE_RESULT");
                                    bw.Write(true);

                                    var sendRes = Instance.SendAsync(response_ms.ToArray(), clientEP);
                                    sendRes.Wait();
                                }

                                using (var response_ms = new MemoryStream(8))
                                using (var bw = new BinaryWriter(response_ms))
                                {
                                    bw.Write("SHARE_START");

                                    var bRes = Instance.BroadcastAsync(response_ms.ToArray(), meeting.Clients.Select(x => x.IpEndPoint).ToArray(), clientEP);
                                    bRes.Wait();
                                }
                            }
                        }
                    }
                    else if (method == "SHARE_FRAME_FIRST")
                    {
                        var frameLength = br.ReadInt32();
                        var meetingId = br.ReadInt64();
                        var chunkIndex = br.ReadInt32();

                        var chunkSize = br.ReadInt32();
                        var chunkData = br.ReadBytes(chunkSize);

                        var meeting = Meetings.Find(x => x.Id == meetingId);

                        using (var response_ms = new MemoryStream(new byte[request.Buffer.Length + 64]))
                        using (var bw = new BinaryWriter(response_ms))
                        {
                            bw.Write("SHARE_FRAME_FIRST");                            // HEADER
                            bw.Write(clientEP.Address.GetAddressBytes());       // Client Address (4 bytes)
                            bw.Write(clientEP.Port);                            // Client Port
                            bw.Write(frameLength);                              // Frame Length
                            bw.Write(chunkIndex);                               // Chunk Index
                            bw.Write(chunkSize);                                // Chunk Size
                            bw.Write(chunkData);                                // Chunk Data

                            await Instance.BroadcastAsync(response_ms.ToArray(), meeting.Clients.Select(x => x.IpEndPoint).ToArray(), clientEP);
                        }
                    }
                    else if (method == "SHARE_FRAME")
                    {
                        var meetingId = br.ReadInt64();
                        var chunkIndex = br.ReadInt32();

                        var chunkSize = br.ReadInt32();
                        var chunkData = br.ReadBytes(chunkSize);

                        var meeting = Meetings.Find(x => x.Id == meetingId);

                        using (var response_ms = new MemoryStream(new byte[request.Buffer.Length + 64]))
                        using (var bw = new BinaryWriter(response_ms))
                        {
                            bw.Write("SHARE_FRAME");                                  // HEADER
                            bw.Write(clientEP.Address.GetAddressBytes());       // Client Address (4 bytes)
                            bw.Write(clientEP.Port);                            // Client Port
                            bw.Write(chunkIndex);                               // Chunk Index
                            bw.Write(chunkSize);                                // Chunk Size
                            bw.Write(chunkData);                                // Chunk Data

                            await Instance.BroadcastAsync(response_ms.ToArray(), meeting.Clients.Select(x => x.IpEndPoint).ToArray(), clientEP);
                        }
                    }
                    else if (method == "SHARE_FRAME_LAST")
                    {
                        var meetingId = br.ReadInt64();
                        var chunkIndex = br.ReadInt32();

                        var chunkSize = br.ReadInt32();
                        var chunkData = br.ReadBytes(chunkSize);

                        var meeting = Meetings.Find(x => x.Id == meetingId);

                        using (var response_ms = new MemoryStream(new byte[request.Buffer.Length + 64]))
                        using (var bw = new BinaryWriter(response_ms))
                        {
                            bw.Write("SHARE_FRAME_LAST");                             // HEADER
                            bw.Write(clientEP.Address.GetAddressBytes());       // Client Address (4 bytes)
                            bw.Write(clientEP.Port);                            // Client Port
                            bw.Write(chunkIndex);                               // Chunk Index
                            bw.Write(chunkSize);                                // Chunk Size
                            bw.Write(chunkData);                                // Chunk Data

                            await Instance.BroadcastAsync(response_ms.ToArray(), meeting.Clients.Select(x => x.IpEndPoint).ToArray(), clientEP);
                        }
                    }
                    else if (method == "SHARE_END")
                    {
                        var meetingId = br.ReadInt64();

                        var meeting = Meetings.Find(x => x.Id == meetingId);

                        meeting.StopShare();

                        var sender = meeting.Clients.Find(x => Equals(x.IpEndPoint, clientEP));
                        sender.UpdateShare(false);

                        using (var response_ms = new MemoryStream(8))
                        using (var bw = new BinaryWriter(response_ms))
                        {
                            bw.Write("SHARE_END");

                            await Instance.BroadcastAsync(response_ms.ToArray(), meeting.Clients.Select(x => x.IpEndPoint).ToArray(), clientEP);
                        }
                    }
                    else if (method == "CHAT")
                    {
                        var meetingId = br.ReadInt64();
                        var meeting = Meetings.Find(x => x.Id == meetingId);
                        var senderName = br.ReadString();

                        var address = br.ReadBytes(sizeof(int));
                        var port = br.ReadInt32();
                        var message = br.ReadString();

                        var receiver = new IPEndPoint(new IPAddress(address), port);

                        using (var response_ms = new MemoryStream(16))
                        using (var bw = new BinaryWriter(response_ms))
                        {
                            bw.Write("CHAT");
                            bw.Write(senderName);
                            bw.Write(message);

                            await Instance.SendAsync(response_ms.ToArray(), receiver);
                        }
                    }
                    else if (method == "CHAT_ALL")
                    {
                        var senderName = br.ReadString();
                        var meetingId = br.ReadInt64();
                        var meeting = Meetings.Find(x => x.Id == meetingId);

                        var message = br.ReadString();

                        using (var response_ms = new MemoryStream())
                        using (var bw = new BinaryWriter(response_ms))
                        {
                            bw.Write("CHAT_ALL");
                            bw.Write(senderName);
                            bw.Write(message);

                            await Instance.BroadcastAsync(response_ms.ToArray(), meeting.Clients.Select(x => x.IpEndPoint).ToArray(), clientEP);
                        }
                    }
                    else if (method == "FILE")
                    {
                        var meetingId = br.ReadInt64();

                        var fileId = br.ReadString();
                        var cursorPosition = br.ReadInt32();

                        var dataLength = br.ReadInt32();
                        var data = br.ReadBytes(dataLength);

                        FileManager.WriteDataToFile(data, cursorPosition, meetingId, fileId);
                    }
                    else if (method == "FILE_LAST")
                    {
                        var fileName = br.ReadString();
                        var senderName = br.ReadString();

                        var address = br.ReadBytes(sizeof(int));
                        var port = br.ReadInt32();                        

                        var receiver = new IPEndPoint(new IPAddress(address), port);

                        var meetingId = br.ReadInt64();

                        var fileId = br.ReadString();
                        var cursorPosition = br.ReadInt32();

                        var dataLength = br.ReadInt32();
                        var data = br.ReadBytes(dataLength);

                        FileManager.WriteDataToFile(data, cursorPosition, meetingId, fileId);

                        using (var response_ms = new MemoryStream(16))
                        using (var bw = new BinaryWriter(response_ms))
                        {
                            bw.Write("FILE_UPLOADED");
                            bw.Write(senderName);
                            bw.Write(fileName);
                            bw.Write(FileManager.GetFileSize(meetingId, fileId));
                            bw.Write(fileId);

                            await Instance.SendAsync(response_ms.ToArray(), receiver);
                        }
                    }
                    else if (method == "FILE_LAST_ALL")
                    {
                        var fileName = br.ReadString();
                        var senderName = br.ReadString();

                        var meetingId = br.ReadInt64();
                        var meeting = Meetings.Find(x => x.Id == meetingId);

                        var fileId = br.ReadString();
                        var cursorPosition = br.ReadInt32();

                        var dataLength = br.ReadInt32();
                        var data = br.ReadBytes(dataLength);

                        FileManager.WriteDataToFile(data, cursorPosition, meetingId, fileId);

                        using (var response_ms = new MemoryStream())
                        using (var bw = new BinaryWriter(response_ms))
                        {
                            bw.Write("FILE_UPLOADED_ALL");
                            bw.Write(senderName);
                            bw.Write(fileName);
                            bw.Write(FileManager.GetFileSize(meetingId, fileId));
                            bw.Write(fileId);

                            await Instance.BroadcastAsync(response_ms.ToArray(), meeting.Clients.Select(x => x.IpEndPoint).ToArray(), clientEP);
                        }
                    }
                    else if (method == "FILE_DELETE")
                    {
                        var meetingId = br.ReadInt64();
                        var fileId = br.ReadString();

                        FileManager.DeleteFile(meetingId, fileId);
                    }
                    else if (method == "DOWNLOAD_FILE")
                    {
                        var meetingId = br.ReadInt64();
                        var fileId = br.ReadString();

                        var cursor = br.ReadInt64();

                        (var data, var bytesRead) = FileManager.GetFileData(meetingId, fileId, cursor);

                        using (var response_ms = new MemoryStream(16))
                        using (var bw = new BinaryWriter(response_ms))
                        {
                            bw.Write("DOWNLOAD_FILE");
                            bw.Write(fileId);
                            bw.Write(bytesRead);
                            bw.Write(data);

                            await Instance.SendAsync(response_ms.ToArray(), clientEP);
                        }
                    }
                }
            }
            catch { }
        }
        private static async void PingClients(CancellationToken token)
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

                while (!token.IsCancellationRequested)
                {
                    for (int i = 0; i < Meetings.Count; i++)
                    {
                        for (int j = 0; j < Meetings[i].Clients.Count; j++)
                        {
                            if (Meetings[i].Clients[j].CheckLastPong())
                            {
                                await Instance.SendAsync(data, Meetings[i].Clients[j].IpEndPoint);                                
                            }
                            else
                            {
                                var client = Meetings[i].Clients[j];
                                Meetings[i].Clients.Remove(client);
                                if (client.IsShareScreen)
                                {
                                    Meetings[i].StopShare();
                                    using (var response_ms = new MemoryStream(8))
                                    using (var bw = new BinaryWriter(response_ms))
                                    {
                                        bw.Write("SHARE_END");

                                        await Instance.BroadcastAsync(response_ms.ToArray(), Meetings[i].Clients.Select(x => x.IpEndPoint).ToArray(), null);
                                    }
                                }

                                if (Meetings[i].Clients.Count == 0)
                                {
                                    FileManager.DeleteMeetingCatalog(Meetings[i].Id);
                                    Meetings.RemoveAt(i);
                                    i--;
                                    break;
                                }
                                else
                                {
                                    using (var broadcast_ms = new MemoryStream(64))
                                    using (var bw = new BinaryWriter(broadcast_ms))
                                    {
                                        bw.Write("UPDATE_LIST");            // Header
                                        bw.Write(Meetings[i].Clients.Count);    // Clients Count

                                        for (var k = 0; k < Meetings[i].Clients.Count; k++)
                                        {
                                            bw.Write(Meetings[i].Clients[k].Name);                                      // Client Name
                                            bw.Write(Meetings[i].Clients[k].IpEndPoint.Address.GetAddressBytes());      // Client Address (4 bytes)
                                            bw.Write(Meetings[i].Clients[k].IpEndPoint.Port);                           // Client Port
                                        }

                                        await Instance.BroadcastAsync(broadcast_ms.ToArray(), Meetings[i].Clients.Select(x => x.IpEndPoint).ToArray(), null, true);                                        
                                    }
                                    j--;
                                }
                            }
                        }
                    }

                    await Task.Delay(5000, token);
                }
            }
            catch { }
        }
    }
}
