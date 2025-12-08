using System.Windows;

namespace SpeedyBee.Dialogs
{
    public partial class SaveRecordingDialog : Window
    {
        public string RecordingName { get; private set; } = string.Empty;
        public bool SaveToCsv { get; private set; } = true;

        public SaveRecordingDialog()
        {
            InitializeComponent();
            txtRecordingName.Focus();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            RecordingName = txtRecordingName.Text.Trim();

            if (string.IsNullOrWhiteSpace(RecordingName))
            {
                MessageBox.Show("Please enter a recording name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtRecordingName.Focus();
                return;
            }

            SaveToCsv = rbCsv.IsChecked == true;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
