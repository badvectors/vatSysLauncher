using System.Windows;
using System.Windows.Controls;
using vatSysLauncher.Controllers;

namespace vatSysLauncher.Views
{
    public partial class PluginsView : UserControl
    {
        public PluginsView()
        {
            InitializeComponent();
            DataContext = Launcher.MainViewModel;
        }

        private async void UpdaterButton_Click(object sender, RoutedEventArgs e)
        {
            var command = ((Button)sender).Tag.ToString();

            await Updater.Run(command);
        }

        private async void PluginInstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (PluginsLocationsComboBox.SelectedValue == null || PluginsOptionsComboBox.SelectedValue == null) return;

            var location = PluginsLocationsComboBox.SelectedValue.ToString();

            var pluginName = PluginsOptionsComboBox.SelectedValue.ToString();

            if (location == null || pluginName == null) return;

            var pluginResponse = Launcher.PluginsAvailable.FirstOrDefault(x => x.Name == pluginName);

            if (pluginResponse == null) return;

            if (location == Launcher.PluginsBaseDirectoryName)
            {
                if (pluginResponse.PreventBaseInstall)
                {
                    MessageBox.Show($"{pluginResponse.Name} cannot be installed into '{Launcher.PluginsBaseDirectoryName}'. Install it into a specific profile instead.", "vatSys Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                location = Launcher.PluginsBaseDirectory;
            }
            else
            {
                location = $"{Launcher.Settings.ProfileDirectory}\\{location}\\Plugins";
            }

            var installCommand = $"Install|Plugin|{pluginResponse.Name}|{location}\\{pluginResponse.DirectoryName}";

            await Updater.Run(installCommand);
        }
    }
}
