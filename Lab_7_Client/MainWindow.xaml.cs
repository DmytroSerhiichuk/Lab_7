using Lab_7_Client.Pages;
using Lab_7_Client.Utils;
using System.Windows;
using System.Windows.Controls;

namespace Lab_7_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Page _currentPage;
        public MainWindow()
        {
            InitializeComponent();

            MyFrame.Navigate(new MainPage());

            ProgramManager.Instance.Navigated += OnNavigated;
            ProgramManager.Instance.ShareStarted += OnShareStarted;
            ProgramManager.Instance.ShareFinished += OnShareFinished;

            Closed += OnClose;
        }

        private void OnShareStarted(MeetingPage page)
        {
            Visibility = Visibility.Hidden;
            ShowInTaskbar = false;

            var shareScreen = new ShareScreen(page);
            shareScreen.Show();
        }

        private void OnShareFinished()
        {
            Visibility = Visibility.Visible;
            ShowInTaskbar = true;
        }

        private void OnClose(object? sender, EventArgs e)
        {
            if (_currentPage is MeetingPage)
            {
                var meetingPage = _currentPage as MeetingPage;
                meetingPage.Close();

            }
        }

        private void OnNavigated(PageType pageType)
        {
            if (pageType == PageType.MainPage)
            {
                _currentPage = new MainPage();
                MyFrame.Navigate(_currentPage);
            }
            else if (pageType == PageType.MeetingPage)
            {
                _currentPage = new MeetingPage();
                MyFrame.Navigate(_currentPage);
            }
        }
    }
}