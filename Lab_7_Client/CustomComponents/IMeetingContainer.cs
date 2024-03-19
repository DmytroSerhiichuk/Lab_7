using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Lab_7_Client.CustomComponents
{
    interface IMeetingContainer
    {
        public UIElementCollection Collection { get; }
        public List<MeetingParticipantContainer> ParticipantsContainers { get; }

        public void UpdateContainers();

        //public void UpdateContainers()
        //{
        //    for (var i = 0; i < ParticipantsContainers.Count; i++)
        //    {
        //        if (!Client.Participants.Any(x => Equals(x.IpEndPoint, ParticipantsContainers[i].ClientIpEP)))
        //        {
        //            ParticipantsContainers.RemoveAt(i);
        //            Collection.RemoveAt(i);
        //            i--;
        //        }
        //    }

        //    for (var i = 0; i < Client.Participants.Count; i++)
        //    {
        //        if (!ParticipantsContainers.Any(x => Equals(x.ClientIpEP, Client.Participants[i].IpEndPoint)))
        //        {
        //            var newClientContainer = new MeetingParticipantContainer(Client.Participants[i].Name, Client.Participants[i].IpEndPoint);
        //            ParticipantsContainers.Add(newClientContainer);
        //            Collection.Add(newClientContainer);
        //        }
        //    }
        //}
    }
}
