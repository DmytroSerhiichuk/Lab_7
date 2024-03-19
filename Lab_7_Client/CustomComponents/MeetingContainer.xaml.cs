using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Lab_7_Client.CustomComponents
{
    /// <summary>
    /// Interaction logic for MeetingContainer.xaml
    /// </summary>
    public partial class MeetingContainer : UserControl
    {
        public List<MeetingClientContainer> ClientsContainers { get; private set; }

        public MeetingContainer()
        {
            InitializeComponent();
        }
    }
}
