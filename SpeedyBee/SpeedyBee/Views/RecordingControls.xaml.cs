using System.Windows;
using System.Windows.Controls;

namespace SpeedyBee.Views
{
    public partial class RecordingControls : UserControl
    {
        public static readonly RoutedEvent StartRecordingEvent =
            EventManager.RegisterRoutedEvent("StartRecording", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(RecordingControls));

        public event RoutedEventHandler StartRecording
        {
            add { AddHandler(StartRecordingEvent, value); }
            remove { RemoveHandler(StartRecordingEvent, value); }
        }

        public static readonly RoutedEvent StopRecordingEvent =
            EventManager.RegisterRoutedEvent("StopRecording", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(RecordingControls));

        public event RoutedEventHandler StopRecording
        {
            add { AddHandler(StopRecordingEvent, value); }
            remove { RemoveHandler(StopRecordingEvent, value); }
        }

        public RecordingControls()
        {
            InitializeComponent();
        }

        private void BtnRecordStart_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(StartRecordingEvent));
            btnRecordStart.IsEnabled = false;
            btnRecordStop.IsEnabled = true;
        }

        private void BtnRecordStop_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(StopRecordingEvent));
            btnRecordStart.IsEnabled = true;
            btnRecordStop.IsEnabled = false;
        }

        public void SetRecordingButtons(bool isPolling)
        {
            btnRecordStart.IsEnabled = isPolling;
            btnRecordStop.IsEnabled = false;
        }
    }
}
