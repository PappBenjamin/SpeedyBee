using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SpeedyBee.Pages;

namespace SpeedyBee
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new HomePage());
        }

        private void NavigateHome_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new HomePage());
        }

        private void NavigateRobotChoice_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new RobotChoicePage());
        }

        private void NavigateVisualization_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new VisualizationPage());
        }

        private void NavigateAbout_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AboutPage());
        }
    }
}