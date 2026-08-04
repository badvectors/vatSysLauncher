using System.IO;
using System.Net.Http;
using System.Windows;
using vatSysLauncher.Models;
using vatSysLauncher.ViewModels;
using vatSysLauncher.Views;

namespace vatSysLauncher.Controllers
{
    public class Launcher
    {
        public static MainWindowViewModel MainViewModel { get; set; } = new MainWindowViewModel();

        private static readonly SetupView SetupView = new();
        private static readonly InitView InitView = new();
        private static readonly HomeView HomeView = new();
        private static readonly ProfilesView ProfilesView = new();
        private static readonly UpdaterView UpdaterView = new();
        private static readonly PluginsView PluginsView = new();
        public static readonly HttpClient HttpClient = new();
        public static Setting Settings = null;
        public static List<string> Changes = [];
        public static List<ProfileOption> ProfileOptions = [];
        public static List<PluginResponse> PluginsAvailable = [];
        public static List<PluginInstalled> PluginsInstalled = [];
        public static string CurrentCanvas = null;
        public static bool HasClearedCached = false;

        public static readonly string VatsysProcessName = "vatSys";
        public static string WorkingDirectory => $"{Settings.ProfileDirectory}\\Temp";
        public static string VatsysExe => $"{Settings.BaseDirectory}\\bin\\vatSys.exe";
        public static string PluginsBaseDirectory => $"{Settings.BaseDirectory}\\bin\\Plugins";
        public static string SettingsFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "vatSys Launcher");
        public static string RestartFile => Path.Combine(SettingsFolder, "Restart.txt");
        public static string SettingsFile => Path.Combine(SettingsFolder, "Settings.json");
        public static string UpdateFile => Path.Combine(SettingsFolder, "Update.txt");
        public static string PluginsFile => Path.Combine(SettingsFolder, "Plugins.json");
        public static string DefaultProfileDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "vatSys Files", "Profiles");
        public static string DefaultBaseDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "vatSys");

        public static string ProfilesUrl => "https://vatsys.sawbe.com/downloads/data/emptyprofiles/profiles.json";
        public static string PluginsUrl => "https://new.vatpac.org/api/Plugins";
        public static string VersionUrl => "https://raw.githubusercontent.com/badvectors/vatSysLauncher/refs/heads/master/vatSysLauncher/LauncherVersion.json";
        public static string PluginsBaseDirectoryName => "All";

        public static void SetLoading(bool loading)
        {
            if (loading == true)
            {
                MainViewModel.ButtonsEnabled = false;
                MainViewModel.WaitText = Visibility.Visible;
                MainViewModel.LaunchButton = Visibility.Hidden;
                return;
            }

            MainViewModel.ButtonsEnabled = true;
            MainViewModel.WaitText = Visibility.Hidden;
            MainViewModel.LaunchButton = Visibility.Visible;
        }

        public static void SetCanvas(string canvasName)
        {
            CurrentCanvas = canvasName;

            MainViewModel.CurrentView = canvasName switch
            {
                "Setup" => SetupView,
                "Init" => InitView,
                "Home" => HomeView,
                "Profiles" => ProfilesView,
                "Updater" => UpdaterView,
                "Plugins" => PluginsView,
                _ => MainViewModel.CurrentView
            };
        }

        public static void GetChanges()
        {
            Changes.Clear();

            foreach (var plugin in PluginsInstalled)
            {
                if (!plugin.UpdateAvailable) continue;

                Changes.Add(plugin.UpdateCommand);
            }

            foreach (var plugin in PluginsInstalled)
            {
                if (!plugin.Remove) continue;

                Changes.Add(plugin.DeleteCommand);
            }

            foreach (var profile in ProfileOptions)
            {
                if (!profile.UpdateAvailable) continue;

                Changes.Add(profile.UpdateCommand);
            }

            if (Changes.Count == 0)
            {
                MainViewModel.UpdatesAvailable = Visibility.Hidden;
            }
            else
            {
                MainViewModel.UpdatesAvailable = Visibility.Visible;
                var updateText = "update";
                if (Changes.Count > 1) updateText = "updates";
                MainViewModel.UpdatesText = $"{Changes.Count} {updateText} to be installed.";
            }
        }

        public static async Task CheckForRestart()
        {
            if (!File.Exists(RestartFile)) return;

            var commands = await File.ReadAllLinesAsync(RestartFile);

            await Updater.Run(commands);

            File.Delete(RestartFile);
        }
    }
}