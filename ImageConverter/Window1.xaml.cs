using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ImageConverter
{
    /// <summary>
    /// Interaktionslogik für Window1.xaml
    /// </summary>
    public partial class MessageWindow : Window
    {
        private Timer timer;

        public MessageWindow()
        {
            InitializeComponent();
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(UpdateMessage);
        }

        private void UpdateMessage()
        {
            if (l_Titel.Content.ToString() == "Converting . . .")
            {
                l_Titel.Content = "Converting .";
            }
            else if (l_Titel.Content.ToString() == "Converting . .")
            {
                l_Titel.Content = "Converting . . .";
            }
            else if (l_Titel.Content.ToString() == "Converting .")
            {
                l_Titel.Content = "Converting . .";
            }
        }

        public void Loop()
        {
            timer = new Timer(250);
            timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            timer.Enabled = true;
        }

        public void UpdateStatus(int count, int amount)
        {
            Dispatcher.Invoke(() =>
            {
                l_ConvertingStatus.Content = count + " of " + amount + " files converted.";
            });
        }

        public void UpdateTime(TimeSpan time, string text, bool milliseconds)
        {
            Dispatcher.Invoke(() =>
            {
                int minutes = (int)time.TotalMinutes;
                string tmp = time.Milliseconds.ToString();
                string millisecondsText = "," + (tmp.Length >= 1 ? tmp[0].ToString() : "") + (tmp.Length >= 2 ? tmp[1].ToString() : "") + (tmp.Length >= 3 ? tmp[2].ToString() : "");

                l_Timer.Content = text + " " + (minutes <= 9 ? "0" + minutes.ToString() : minutes.ToString()) + ":" + (time.Seconds <= 9 ? "0" + time.Seconds.ToString() : time.Seconds.ToString()) + ( milliseconds ? millisecondsText : "");
            });
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
