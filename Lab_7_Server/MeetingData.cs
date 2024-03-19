namespace Lab_7_Server
{
    internal class MeetingData
    {
        public readonly long Id;
        public List<MeetingUser> Clients { get; private set; }

        public MeetingData()
        {
            Id = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;            
            Clients = new List<MeetingUser>();
        }

        public void AddClient(MeetingUser client)
        {
            Clients.Add(client);
        }
    }
}
