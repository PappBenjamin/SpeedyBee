using System.Windows;
using System.Windows.Controls;

namespace SpeedyBee.Pages
{
    public partial class HomePage : Page
    {
        public HomePage()
        {
            InitializeComponent();
        }

        private void SelectRobot_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new RobotChoicePage());
        }

        private void OpenVisualization_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new VisualizationPage());
        }
    }
}