using System.Windows;
using System.Windows.Controls;

namespace SpeedyBee.Views
{
    public partial class PlaybackControls : UserControl
    {
        public static readonly RoutedEvent StartEvent =
            EventManager.RegisterRoutedEvent("Start", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(PlaybackControls));

        public event RoutedEventHandler Start
        {
            add { AddHandler(StartEvent, value); }
            remove { RemoveHandler(StartEvent, value); }
        }

        public static readonly RoutedEvent PauseEvent =
            EventManager.RegisterRoutedEvent("Pause", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(PlaybackControls));

        public event RoutedEventHandler Pause
        {
            add { AddHandler(PauseEvent, value); }
            remove { RemoveHandler(PauseEvent, value); }
        }

        public static readonly RoutedEvent ResetEvent =
            EventManager.RegisterRoutedEvent("Reset", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(PlaybackControls));

        public event RoutedEventHandler Reset
        {
            add { AddHandler(ResetEvent, value); }
            remove { RemoveHandler(ResetEvent, value); }
        }

        public PlaybackControls()
        {
            InitializeComponent();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(StartEvent));
            btnStart.IsEnabled = false;
            btnPause.IsEnabled = true;
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(PauseEvent));
            btnStart.IsEnabled = true;
            btnPause.IsEnabled = false;
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(ResetEvent));
            btnStart.IsEnabled = true;
            btnPause.IsEnabled = false;
        }
    }
}
