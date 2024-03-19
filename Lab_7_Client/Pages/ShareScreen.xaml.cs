using Lab_7_Client.Utils;
using System.Windows;

namespace Lab_7_Client.Pages
{
    /// <summary>
    /// Interaction logic for ShareScreen.xaml
    /// </summary>
    public partial class ShareScreen : Window
    {
        private MeetingPage meetingPage;

        public ShareScreen(MeetingPage page)
        {
            InitializeComponent();

            meetingPage = page;
        }

        private void OnCloseButtonClicked(object sender, RoutedEventArgs e)
        {
            ProgramManager.Instance.StopShare();
            Close();
        }
    }
}
