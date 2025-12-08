using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using OpenTK.Mathematics;
using StackExchange.Redis;
using SpeedyBee.Views;
using SpeedyBee.ViewModels;

namespace SpeedyBee.Pages
{
    public partial class VisualizationPage : Page
    {
        private ConnectionMultiplexer _redisConnection;
        private IDatabase _redis;
        private CancellationTokenSource? _pollingCancellation;
        private List<MotionFrame> _frames = new();
        private bool _isPolling = false;
        private bool _isRecording = false;
        private List<ImuData> _recordedData = new();
        private const string RedisQueue = "imu_queue";
        private DataSourcePanel.DataSourceType _dataSource = DataSourcePanel.DataSourceType.Redis;
        private string _selectedCsvPath = string.Empty;
        private DispatcherTimer _playbackTimer;
        private int _currentFrameIndex = 0;
        private bool _isServerRunning = false;

        public VisualizationPage()
        {
            InitializeComponent();
            _redisConnection = ConnectionMultiplexer.Connect("localhost:6379");
            _redis = _redisConnection.GetDatabase();
            _playbackTimer = new DispatcherTimer();
            _playbackTimer.Interval = TimeSpan.FromMilliseconds(50);
            _playbackTimer.Tick += PlaybackTimer_Tick;
        }

        private void LoadMotionData(string csvPath)
        {
            try
            {
                _frames.Clear();
                _currentFrameIndex = 0;

                if (!File.Exists(csvPath))
                {
                    MessageBox.Show($"Motion data file not found at: {csvPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                foreach (var line in File.ReadLines(csvPath))
                {
                    var parts = line.Split(',');
                    if (parts.Length < 6) continue;

                    if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float ax) &&
                        float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float ay) &&
                        float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float az) &&
                        float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float gx) &&
                        float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float gy) &&
                        float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float gz))
                    {
                        Vector3 accel = new Vector3(ax, ay, az);
                        Vector3 rot = new Vector3(gx, gy, gz);

                        _frames.Add(new MotionFrame { Acceleration = accel, Rotation = rot });
                    }
                }

                MessageBox.Show($"Loaded {_frames.Count} motion frames from {Path.GetFileName(csvPath)}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading motion data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task StartPolling(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await FetchAndUpdateImuData(token);
                await Task.Delay(TimeSpan.FromMilliseconds(50), token);
            }
        }

        private async Task FetchAndUpdateImuData(CancellationToken token)
        {
            try
            {
                ImuData latestImuData = null;
                var json = await _redis.ListRightPopAsync(RedisQueue);
                while (!string.IsNullOrEmpty(json))
                {
                    var imuData = JsonSerializer.Deserialize<ImuData>(json);
                    if (imuData != null)
                    {
                        latestImuData = imuData;
                    }
                    json = await _redis.ListRightPopAsync(RedisQueue);
                }
                if (latestImuData != null)
                {
                    Dispatcher.Invoke(() => visualizationViewport.UpdateImuTransform(latestImuData));
                }
                if (_isRecording && latestImuData != null)
                {
                    _recordedData.Add(latestImuData);
                }
            }
            catch (Exception ex)
            {
                // Handle error silently
            }
        }
        
        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            if (_currentFrameIndex < _frames.Count)
            {
                var frame = _frames[_currentFrameIndex];
                visualizationViewport.UpdateFrameTransform(frame.Acceleration, frame.Rotation);
                _currentFrameIndex++;
            }
            else
            {
                _playbackTimer.Stop();
                MessageBox.Show("Playback finished.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void VisualizationPage_KeyDown(object sender, KeyEventArgs e)
        {
            visualizationViewport.HandleKeyDown(e);
        }

        private void StartFastApiServer()
        {
            if (_isServerRunning) return;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c cd C:\\Users\\progenor\\code\\SpeedyBee\\fastAPI && python run.py",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                _isServerRunning = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting FastAPI server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopFastApiServer()
        {
            if (!_isServerRunning) return;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "taskkill.exe",
                    Arguments = "/f /im python.exe",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                _isServerRunning = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping FastAPI server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DataSourcePanel_DataSourceChanged(object sender, RoutedEventArgs e)
        {
            _dataSource = dataSourcePanel.SelectedDataSource;
            if (_dataSource == DataSourcePanel.DataSourceType.Csv)
            {
                _selectedCsvPath = dataSourcePanel.SelectedCsvPath;
                if (!string.IsNullOrEmpty(_selectedCsvPath))
                {
                    LoadMotionData(_selectedCsvPath);
                }
                recordingControls.SetRecordingButtons(false);
                _isRecording = false;
            }
            else
            {
                _frames.Clear();
            }
        }
        
        private void PlaybackControls_Start(object sender, RoutedEventArgs e)
        {
            if (_dataSource == DataSourcePanel.DataSourceType.Redis)
            {
                if (_isPolling) return;

                StartFastApiServer();
                _isPolling = true;
                _pollingCancellation = new CancellationTokenSource();
                _ = StartPolling(_pollingCancellation.Token);
                recordingControls.SetRecordingButtons(true);
            }
            else if (_dataSource == DataSourcePanel.DataSourceType.Csv)
            {
                if (_frames.Count == 0)
                {
                    MessageBox.Show("No motion frames loaded. Please select a CSV file first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _currentFrameIndex = 0;
                _playbackTimer.Start();
            }
        }

        private void PlaybackControls_Pause(object sender, RoutedEventArgs e)
        {
            if (_dataSource == DataSourcePanel.DataSourceType.Redis)
            {
                _isPolling = false;
                _pollingCancellation?.Cancel();
                StopFastApiServer();
            }
            else if (_dataSource == DataSourcePanel.DataSourceType.Csv)
            {
                _playbackTimer.Stop();
            }
            _isRecording = false;
            recordingControls.SetRecordingButtons(false);
        }
        
        private void PlaybackControls_Reset(object sender, RoutedEventArgs e)
        {
            if (_dataSource == DataSourcePanel.DataSourceType.Redis)
            {
                _pollingCancellation?.Cancel();
                _isPolling = false;
                StopFastApiServer();
            }
            else if (_dataSource == DataSourcePanel.DataSourceType.Csv)
            {
                _playbackTimer.Stop();
                _currentFrameIndex = 0;
            }

            visualizationViewport.Reset();

            _isRecording = false;
            recordingControls.SetRecordingButtons(false);
        }

        private void RecordingControls_StartRecording(object sender, RoutedEventArgs e)
        {
            if (!_isPolling)
            {
                MessageBox.Show("Please start real-time visualization first.", "Recording Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _isRecording = true;
            _recordedData.Clear();
        }

        private async void RecordingControls_StopRecording(object sender, RoutedEventArgs e)
        {
            _isRecording = false;

            var saveDialog = new SpeedyBee.Dialogs.SaveRecordingDialog();
            if (saveDialog.ShowDialog() == true)
            {
                var recordingName = saveDialog.RecordingName;

                if (saveDialog.SaveToCsv)
                {
                    var fileDialog = new SaveFileDialog
                    {
                        DefaultExt = ".csv",
                        Filter = "CSV files (*.csv)|*.csv",
                        Title = "Save Recorded Motion Data",
                        InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                        FileName = recordingName
                    };

                    if (fileDialog.ShowDialog() == true)
                    {
                        try
                        {
                            using (var writer = new StreamWriter(fileDialog.FileName))
                            {
                                foreach (var data in _recordedData)
                                {
                                    int ax = (int)Math.Round(data.accel_x);
                                    int ay = (int)Math.Round(data.accel_y);
                                    int az = (int)Math.Round(data.accel_z);
                                    int gx = (int)Math.Round(data.gyro_x);
                                    int gy = (int)Math.Round(data.gyro_y);
                                    int gz = (int)Math.Round(data.gyro_z);
                                    writer.WriteLine($"{ax},{ay},{az},{gx},{gy},{gz}");
                                }
                            }
                            MessageBox.Show($"Recorded {_recordedData.Count} frames to {Path.GetFileName(fileDialog.FileName)}",
                                "Recording Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error saving recording: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    try
                    {
                        var apiService = new SpeedyBee.Services.ApiService();
                        var frames = _recordedData.Select((data, index) => new SpeedyBee.Services.FrameData
                        {
                            AccelX = (int)Math.Round(data.accel_x),
                            AccelY = (int)Math.Round(data.accel_y),
                            AccelZ = (int)Math.Round(data.accel_z),
                            GyroX = (int)Math.Round(data.gyro_x),
                            GyroY = (int)Math.Round(data.gyro_y),
                            GyroZ = (int)Math.Round(data.gyro_z),
                            FrameNumber = index
                        }).ToList();

                        var result = await apiService.SaveRunAsync(recordingName, frames);

                        if (result != null)
                        {
                            MessageBox.Show($"Recorded {result.FrameCount} frames saved to database as '{result.Name}'",
                                "Recording Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving to database: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private class MotionFrame
        {
            public Vector3 Acceleration { get; set; }
            public Vector3 Rotation { get; set; }
        }
    }
}
