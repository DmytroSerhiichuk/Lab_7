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
    /// Interaction logic for Scheduler.xaml
    /// </summary>
    public partial class Scheduler : UserControl
    {
        public Scheduler()
        {
            InitializeComponent();

            var schedules = Client.GetSchedule();

            foreach (var schedule in schedules)
            {
                AddScheduleContainer(schedule);
            }
        }

        private void OnAddClicked(object sender, RoutedEventArgs e)
        {
            Client.AddSchedule(MyDate.DisplayDate);

            AddScheduleContainer(MyDate.DisplayDate);
        }

        private void OnCalendarClosed(object sender, RoutedEventArgs e)
        {
            MyDate.DisplayDate = (DateTime)MyDate.SelectedDate;
        }

        public void AddScheduleContainer(DateTime date)
        {
            var sp = new StackPanel()
            {
                Orientation = Orientation.Horizontal
            };
            var l = new Label()
            {
                Content = $"Date: {date.Day}.{date.Month}.{date.Year}"
            };
            var btn = new Button()
            {
                Content = "Delete",
            };
            btn.Click += OnDelete;

            sp.Children.Add(l);
            sp.Children.Add(btn);

            MainPanel.Children.Add(sp);
        }

        public void AddScheduleContainer(string date)
        {
            var sp = new StackPanel()
            {
                Orientation = Orientation.Horizontal
            };
            var l = new Label()
            {
                Content = date
            };
            var btn = new Button()
            {
                Content = "Delete"
            };
            btn.Click += OnDelete;

            sp.Children.Add(l);
            sp.Children.Add(btn);

            MainPanel.Children.Add(sp);
        }

        private void OnDelete(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var sp = btn.Parent as StackPanel;

            var l = sp.Children[0] as Label;

            Client.DeleteSchedule(l.Content as string);

            MainPanel.Children.Remove(sp);
        }
    }
}
