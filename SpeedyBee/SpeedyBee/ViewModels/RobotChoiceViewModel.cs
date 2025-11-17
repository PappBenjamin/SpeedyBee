using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Management;
using System.Windows.Input;
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
            set
            {
                if (SetProperty(ref _selectedRobot, value))
                {
                    OnPropertyChanged(nameof(IsPidConfigVisible));
                }
            }
        }

        private double _kp = 1.0;
        public double Kp
        {
            get => _kp;
            set => SetProperty(ref _kp, value);
        }

        private double _kd = 0.0;
        public double Kd
        {
            get => _kd;
            set => SetProperty(ref _kd, value);
        }

        private double _baseSpeed = 50.0;
        public double baseSpeed
        {
            get => _baseSpeed;
            set => SetProperty(ref _baseSpeed, value);
        }

        private double _maxTurnSpeed = 100.0;
        public double maxTurnSpeed
        {
            get => _maxTurnSpeed;
            set => SetProperty(ref _maxTurnSpeed, value);
        }

        public bool IsPidConfigVisible => !string.IsNullOrEmpty(SelectedRobot);

        private ManagementEventWatcher? _portWatcher;
        private readonly Dispatcher _dispatcher;

        public ICommand UpdatePidParametersCommand { get; }

        public RobotChoiceViewModel()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            UpdatePidParametersCommand = new RelayCommand(UpdatePidParameters, CanUpdatePidParameters);
            RefreshPorts();
            StartWatchingPorts();
        }

        private bool CanUpdatePidParameters()
        {
            return !string.IsNullOrEmpty(SelectedPort) && !string.IsNullOrEmpty(SelectedRobot);
        }

        private void UpdatePidParameters()
        {
            if (string.IsNullOrEmpty(SelectedPort)) return;

            try
            {
                using var serialPort = new SerialPort(SelectedPort, 115200);
                serialPort.Open();
                
                // Send parameters to the robot
                // Format: "PID,Kp,Kd,baseSpeed,maxTurnSpeed\n"
                string command = $"{Kp:F6},{Kd:F6},{baseSpeed:F6},{maxTurnSpeed:F6}\n";
                serialPort.Write(command);
                
                serialPort.Close();
            }
            catch (Exception)
            {
                // Handle communication errors
            }
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

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }
}