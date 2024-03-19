using System.Windows;
using System.Windows.Controls;
using Lab_7_Client.Utils;

namespace Lab_7_Client.Pages
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();

            Client.Connected += OnConnected;
        }

        private void OnConnected()
        {
            Client.Connected -= OnConnected;
            App.Current.Dispatcher.Invoke(() =>
            {
                ProgramManager.Instance.Navigate(PageType.MeetingPage);
            });
        }

        private async void OnCreateMeeting(object sender, RoutedEventArgs e)
        {
            try
            {
                var name = UserNameInput.Text.Trim();
                if (name.Length == 0) name = "NULL";

                Client.CreateMeeting(name);
            }
            catch
            {
                MessageBox.Show("Зустріч не було створено", "ПОМИЛКА", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnConnect(object sender, RoutedEventArgs e)
        {
            try
            {
                long meetingId = Int64.Parse(MeetingIdInput.Text);
                var name = UserNameInput.Text.Trim();
                if (name.Length == 0) name = "NULL";

                Client.Connect(name, meetingId);
            }
            catch
            {
                MessageBox.Show("ID задано некоректно", "ПОМИЛКА", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
