using System.Windows;

namespace Lab_7_Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            MyTextBox.Text = "Status: NULL\n";
        }

        private void OnClicked(object sender, RoutedEventArgs e)
        {
            if (!Server.IsWorking)
            {
                if (Server.Start())
                {
                    Switcher.Header = "Stop";
                    MyTextBox.Text = $"Status: WORKING\n\n" +
                                     $"LocalEndPoint: {Server.Instance.Instance.Client.LocalEndPoint}";
                }
            }
            else
            {
                if (Server.Stop())
                {
                    Switcher.Header = "Start";
                    MyTextBox.Text = "Status: NULL\n";
                }
            }
        }
    }
}