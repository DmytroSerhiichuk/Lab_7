namespace Lab_7_Server
{
    internal class MeetingData
    {
        public readonly long Id;
        public List<MeetingUser> Clients { get; private set; }
        public bool IsScreenShared { get; private set; } = false;

        public MeetingData()
        {
            Id = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;            
            Clients = new List<MeetingUser>();
        }

        public void AddClient(MeetingUser client)
        {
            Clients.Add(client);
        }

        public void StartShare()
        {
            IsScreenShared = true;
        }
        public void StopShare()
        {
            IsScreenShared = false;
        }
    }
}
