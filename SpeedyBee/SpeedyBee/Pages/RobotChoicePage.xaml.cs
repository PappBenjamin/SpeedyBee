using System.Windows;
using System.Windows.Controls;
using SpeedyBee.ViewModels;

namespace SpeedyBee.Pages
{
    public partial class RobotChoicePage : Page
    {
        private readonly RobotChoiceViewModel _viewModel;

        public RobotChoicePage()
        {
            InitializeComponent();
            _viewModel = (RobotChoiceViewModel)DataContext;
        }

        private void SelectRobot_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string version = button.Tag.ToString();
                SelectedRobotText.Text = $"Current Robot: SpeedyBee {version}";
                _viewModel.SelectedRobot = version;
            }
        }
        
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Cleanup();
        }
    }
}