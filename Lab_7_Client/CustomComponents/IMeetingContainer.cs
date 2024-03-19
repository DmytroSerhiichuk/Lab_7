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

        public void InitContainers();

        public void Clear()
        {
            Collection.Clear();
        }
    }
}
