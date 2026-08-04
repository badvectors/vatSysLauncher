using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using vatSysLauncher.Controllers;

namespace vatSysLauncher.Views
{
    public partial class SetupView : UserControl
    {
        public SetupView()
        {
            InitializeComponent();
            DataContext = Launcher.MainViewModel;
        }

        private void BaseDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new OpenFolderDialog();

            if (folderDialog.ShowDialog() == true)
            {
                Launcher.Settings.BaseDirectory = folderDialog.FolderName;

                Settings.Save();

                BaseDirectoryTextBox.Text = Launcher.Settings.BaseDirectory;
            }
        }

        private void ProfileDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new OpenFolderDialog();

            if (folderDialog.ShowDialog() == true)
            {
                Launcher.Settings.ProfileDirectory = folderDialog.FolderName;

                Settings.Save();

                _ = Profiles.Init();

                ProfileDirectoryTextBox.Text = Launcher.Settings.ProfileDirectory;
            }
        }

        private async void DevelopmentCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Launcher.Settings.IncludeDevelopment = false;
            Settings.Save();
            await Plugins.Init();
        }

        private async void DevelopmentCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Launcher.Settings.IncludeDevelopment = true;
            Settings.Save();
            await Plugins.Init();
        }
    }
}
