using System.Windows;
using System.Windows.Controls;
using vatSysLauncher.Controllers;

namespace vatSysLauncher.Views
{
    public partial class ProfilesView : UserControl
    {
        public ProfilesView()
        {
            InitializeComponent();
            DataContext = Launcher.MainViewModel;
        }

        private async void UpdaterButton_Click(object sender, RoutedEventArgs e)
        {
            var command = ((Button)sender).Tag.ToString();

            await Updater.Run(command);
        }
    }
}
