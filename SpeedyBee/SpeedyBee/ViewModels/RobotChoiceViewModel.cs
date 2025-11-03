using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Management;
using System.Windows.Threading;

namespace SpeedyBee.ViewModels
{
    public class RobotChoiceViewModel : ViewModelBase
    {
        public ObservableCollection<string> AvailablePorts { get; } = new();
        
        private string? _selectedPort;
        public string? SelectedPort
        {
            get => _selectedPort;
            set => SetProperty(ref _selectedPort, value);
        }

        private string? _selectedRobot;
        public string? SelectedRobot
        {
            get => _selectedRobot;
            set => SetProperty(ref _selectedRobot, value);
        }

        private ManagementEventWatcher? _portWatcher;
        private readonly Dispatcher _dispatcher;

        public RobotChoiceViewModel()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            RefreshPorts();
            StartWatchingPorts();
        }

        private void RefreshPorts()
        {
            var currentPorts = SerialPort.GetPortNames();
            _dispatcher.Invoke(() =>
            {
                AvailablePorts.Clear();
                foreach (var port in currentPorts)
                {
                    AvailablePorts.Add(port);
                }

                // If the previously selected port is no longer available, clear it
                if (SelectedPort != null && !AvailablePorts.Contains(SelectedPort))
                {
                    SelectedPort = null;
                }
            });
        }

        private void StartWatchingPorts()
        {
            try
            {
                var query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 OR EventType = 3");
                _portWatcher = new ManagementEventWatcher(query);
                _portWatcher.EventArrived += (s, e) => 
                {
                    // Small delay to allow the system to finish device enumeration
                    _dispatcher.BeginInvoke(() => RefreshPorts(), DispatcherPriority.Background);
                };
                _portWatcher.Start();
            }
            catch (ManagementException)
            {
                // Handle the case where WMI is not available
                // For now, we'll just rely on manual refresh
            }
        }

        public void Cleanup()
        {
            if (_portWatcher != null)
            {
                _portWatcher.Stop();
                _portWatcher.Dispose();
                _portWatcher = null;
            }
        }
    }
}