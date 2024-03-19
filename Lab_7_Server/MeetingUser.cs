using System.Net;

namespace Lab_7_Server
{
    internal class MeetingUser
    {
        public IPEndPoint IpEndPoint { get; private set; }
        public string Name { get; private set; }
        public long LastPong { get; private set; }
        public bool IsUsingCamera { get; private set; } = false;
        public bool IsUsingAudio { get; private set; } = false;
        public bool IsShareScreen { get; private set; } = false;

        public MeetingUser(IPEndPoint endPoint, string name)
        {
            IpEndPoint = endPoint;
            Name = name;
            LastPong = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public bool CheckLastPong()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - LastPong < 10000;
        }
        public void UpdateLastPong()
        {
            LastPong = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public void UpdateCamera(bool value)
        {
            IsUsingCamera = value;
        }
        public void UpdateAudio(bool value)
        {
            IsUsingAudio = value;
        }
        public void UpdateShare(bool value)
        {
            IsShareScreen = value;
        }
    }
}
