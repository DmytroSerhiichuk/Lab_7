using System.Net;
using System.Net.Sockets;

namespace Lab_7_Server
{
    internal class UdpServer
    {
        private CancellationTokenSource _cts;

        private readonly List<UdpServerClient> _udpClients;
        private readonly SemaphoreSlim _semaphore;

        public UdpClient Instance { get; private set; }
        public IPEndPoint IPEndPoint { get; private set; }

        public UdpServer(IPAddress address, int port)
        {
            IPEndPoint = new IPEndPoint(address, port);
            _udpClients = new List<UdpServerClient>();
            _semaphore = new SemaphoreSlim(1);
            _cts = new CancellationTokenSource();
        }

        public void Start()
        {
            Instance = new UdpClient(IPEndPoint);

            var token = _cts.Token;
            Task.Run(() => CheckClients(token), token);
        }
        public void Close()
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource();
            Instance.Close();
            Instance.Dispose();
        }

        public async Task<UdpReceiveResult> ReceiveAsync(CancellationToken token)
        {
            return await Instance.ReceiveAsync(token);
        }

        public async void HandleRequest(UdpReceiveResult res, Action<UdpReceiveResult> function)
        {
            var udpClinent = _udpClients.Find(x => Equals(x.IPEndPoint, res.RemoteEndPoint));

            if (udpClinent != null)
            {
                await _semaphore.WaitAsync();

                await Task.Run(async () =>
                {
                    function(res);

                    _semaphore.Release();
                });

                udpClinent.UpdateLastRequestTime();
            }
            else
            {
                var newClient = new UdpServerClient(res.RemoteEndPoint);
                _udpClients.Add(newClient);

                await Task.Run(() =>
                {
                    function(res);
                });

                newClient.UpdateLastRequestTime();
            }
        }

        public async Task SendAsync(byte[] data, IPEndPoint remote)
        {
            await Instance.SendAsync(data, data.Length, remote);
        }

        public async Task BroadcastAsync(byte[] data, IPEndPoint[] remotes, IPEndPoint sender, bool isSendToSender = false)
        {
            foreach (var remote in remotes)
            {
                if (isSendToSender || (!isSendToSender && !Equals(remote, sender)))
                {
                    await Instance.SendAsync(data, data.Length, remote);
                }
            }
        }

        private async void CheckClients(CancellationToken token)
        {
            while(!token.IsCancellationRequested)
            {
                await Task.Delay(60000);

                for (var i = 0; i < _udpClients.Count; i++)
                {
                    var time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    if (time - _udpClients[i].LastRequest > 60000)
                    {
                        _udpClients.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
    }

    class UdpServerClient
    {
        public IPEndPoint IPEndPoint { get; private set; }
        public long LastRequest { get; private set; }

        public UdpServerClient(IPEndPoint iPEndPoint)
        {
            IPEndPoint = iPEndPoint;
            LastRequest = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public void UpdateLastRequestTime()
        {
            LastRequest = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }
}
