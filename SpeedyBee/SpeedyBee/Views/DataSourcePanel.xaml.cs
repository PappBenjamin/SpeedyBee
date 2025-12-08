using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SpeedyBee.Views
{
    public partial class DataSourcePanel : UserControl
    {
        public enum DataSourceType { Redis, Csv }

        public static readonly RoutedEvent DataSourceChangedEvent =
            EventManager.RegisterRoutedEvent("DataSourceChanged", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(DataSourcePanel));

        public event RoutedEventHandler DataSourceChanged
        {
            add { AddHandler(DataSourceChangedEvent, value); }
            remove { RemoveHandler(DataSourceChangedEvent, value); }
        }

        public DataSourceType SelectedDataSource { get; private set; } = DataSourceType.Redis;
        public string SelectedCsvPath { get; private set; }

        public DataSourcePanel()
        {
            InitializeComponent();
            rbRedis.IsChecked = true;
        }

        private void DataSource_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == rbRedis)
            {
                SelectedDataSource = DataSourceType.Redis;
                lblSelectedFile.Content = string.Empty;
            }
            else if (sender == rbCsv)
            {
                SelectedDataSource = DataSourceType.Csv;
            }
            RaiseEvent(new RoutedEventArgs(DataSourceChangedEvent));
        }

        private void btnSelectCsv_Click(object sender, RoutedEventArgs e)
        {
            var loadDialog = new SpeedyBee.Dialogs.LoadRecordingDialog();
            if (loadDialog.ShowDialog() == true)
            {
                if (loadDialog.LoadFromCsv)
                {
                    if (!string.IsNullOrEmpty(loadDialog.SelectedCsvPath))
                    {
                        SelectedCsvPath = loadDialog.SelectedCsvPath;
                        lblSelectedFile.Content = Path.GetFileName(loadDialog.SelectedCsvPath);
                        RaiseEvent(new RoutedEventArgs(DataSourceChangedEvent));
                    }
                }
            }
        }
    }
}
