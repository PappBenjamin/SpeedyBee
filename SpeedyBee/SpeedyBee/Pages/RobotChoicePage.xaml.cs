using System.Windows;
using System.Windows.Controls;

namespace SpeedyBee.Pages
{
    public partial class RobotChoicePage : Page
    {
        public RobotChoicePage()
        {
            InitializeComponent();
        }

        private void SelectRobot_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string version = button.Tag.ToString();
                SelectedRobotText.Text = $"Current Robot: SpeedyBee {version}";
            }
        }
    }
}