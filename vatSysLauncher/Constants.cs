using System.IO;
using vatSysManager;

namespace vatSysLauncher
{
    public class Constants
    {
        public static Settings Settings = null;
        public static List<string> CurrentCommands = [];

        public static string WorkingDirectory => $"{Settings.ProfileDirectory}\\Temp";
        public static string VatsysExe => $"{Settings.BaseDirectory}\\bin\\vatSys.exe";
        public static string PluginsBaseDirectory => $"{Settings.BaseDirectory}\\bin\\Plugins";
        public static string SettingsFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "vatSys Launcher");
        public static string RestartFile => Path.Combine(SettingsFolder, "Restart.txt");
        public static string SettingsFile => Path.Combine(SettingsFolder, "Settings.json");
        public static string UpdateFile => Path.Combine(SettingsFolder, "Update.txt");
        public static string PluginsFile => Path.Combine(SettingsFolder, "Plugins.json");

        public static string ProfilesUrl => "https://vatsys.sawbe.com/downloads/data/emptyprofiles/profiles.json";
        public static string PluginsUrl => "https://raw.githubusercontent.com/badvectors/vatSysLauncher/refs/heads/master/vatSysLauncher/Plugins.json";
        public static string VersionUrl => "https://raw.githubusercontent.com/badvectors/vatSysLauncher/refs/heads/master/vatSysLauncher/LauncherVersion.json";
        public static string PluginsBaseDirectoryName => "Base Directory";
    }
}
