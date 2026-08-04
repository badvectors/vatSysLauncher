using System.Windows;
using System.Windows.Controls;
using vatSysLauncher.Controllers;

namespace vatSysLauncher.Views
{
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
            DataContext = Launcher.MainViewModel;
        }

        private async void VatSysLaunchButton_Click(object sender, RoutedEventArgs e)
        {
            await VatSys.Launch();
        }
    }
}
