using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SpeedyBee.Services;

namespace SpeedyBee.Dialogs
{
    public partial class LoadRecordingDialog : Window
    {
        private readonly ApiService _apiService;
        public string? SelectedCsvPath { get; private set; }
        public RunDetails? SelectedRun { get; private set; }
        public bool LoadFromCsv { get; private set; } = true;

        public LoadRecordingDialog()
        {
            InitializeComponent();
            _apiService = new ApiService();
        }

        private void LoadSource_Changed(object sender, RoutedEventArgs e)
        {
            if (rbLoadCsv.IsChecked == true)
            {
                LoadFromCsv = true;
                pnlCsvSection.Visibility = Visibility.Visible;
                pnlPostgresSection.Visibility = Visibility.Collapsed;
                grpRecordingList.Visibility = Visibility.Collapsed;
                btnLoad.IsEnabled = !string.IsNullOrEmpty(SelectedCsvPath);
            }
            else if (rbLoadPostgres.IsChecked == true)
            {
                LoadFromCsv = false;
                pnlCsvSection.Visibility = Visibility.Collapsed;
                pnlPostgresSection.Visibility = Visibility.Visible;
                grpRecordingList.Visibility = Visibility.Visible;
                btnLoad.IsEnabled = false;
                
                // Load recordings automatically
                _ = LoadRecordingsAsync();
            }
        }

        private void BtnBrowseCsv_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                Filter = "CSV files (*.csv)|*.csv",
                Title = "Select Motion Data CSV"
            };

            if (dialog.ShowDialog() == true)
            {
                SelectedCsvPath = dialog.FileName;
                txtSelectedFile.Text = $"Selected: {Path.GetFileName(dialog.FileName)}";
                btnLoad.IsEnabled = true;
            }
        }

        private async Task LoadRecordingsAsync(string? searchTerm = null)
        {
            try
            {
                txtLoadingStatus.Text = "Loading recordings...";
                txtLoadingStatus.Visibility = Visibility.Visible;
                lvRecordings.IsEnabled = false;

                var recordings = await _apiService.GetRunsAsync(searchTerm);
                lvRecordings.ItemsSource = recordings;

                txtLoadingStatus.Text = $"Found {recordings.Count} recording(s)";
                
                if (recordings.Count == 0)
                {
                    txtLoadingStatus.Text = "No recordings found";
                }
            }
            catch (Exception ex)
            {
                txtLoadingStatus.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Failed to load recordings: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                lvRecordings.IsEnabled = true;
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Optional: Add debouncing here for better UX
        }

        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            var searchTerm = txtSearch.Text.Trim();
            await LoadRecordingsAsync(string.IsNullOrEmpty(searchTerm) ? null : searchTerm);
        }

        private void LvRecordings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnLoad.IsEnabled = lvRecordings.SelectedItem != null;
        }

        private async void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (LoadFromCsv)
            {
                if (string.IsNullOrEmpty(SelectedCsvPath))
                {
                    MessageBox.Show("Please select a CSV file.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DialogResult = true;
                Close();
            }
            else
            {
                var selected = lvRecordings.SelectedItem as RunSummary;
                if (selected == null)
                {
                    MessageBox.Show("Please select a recording.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    txtLoadingStatus.Text = "Loading recording data...";
                    txtLoadingStatus.Visibility = Visibility.Visible;
                    btnLoad.IsEnabled = false;

                    SelectedRun = await _apiService.GetRunByIdAsync(selected.Id);

                    if (SelectedRun == null || SelectedRun.Frames.Count == 0)
                    {
                        MessageBox.Show("The selected recording has no frames.", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    txtLoadingStatus.Text = $"Error: {ex.Message}";
                    MessageBox.Show($"Failed to load recording: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    btnLoad.IsEnabled = true;
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
