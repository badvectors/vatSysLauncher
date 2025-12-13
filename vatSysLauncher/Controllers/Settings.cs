using Newtonsoft.Json;
using System.IO;
using vatSysLauncher.Models;

namespace vatSysLauncher.Controllers
{
    public class Settings
    {
        public static void Init()
        {
            if (!Directory.Exists(Launcher.SettingsFolder))
            {
                Directory.CreateDirectory(Launcher.SettingsFolder);
            }

            if (!File.Exists(Launcher.SettingsFile))
            {
                var settings = new Setting();

                if (Directory.Exists(Launcher.DefaultProfileDirectory))
                {
                    settings.ProfileDirectory = Launcher.DefaultProfileDirectory;
                }

                if (Directory.Exists(Launcher.DefaultBaseDirectory))
                {
                    settings.BaseDirectory = Launcher.DefaultBaseDirectory;
                }

                Launcher.Settings = settings;

                var settingsFile = JsonConvert.SerializeObject(Launcher.Settings);

                File.WriteAllText(Launcher.SettingsFile, settingsFile);

                return;
            }

            try
            {
                var settingsFile = File.ReadAllText(Launcher.SettingsFile);

                Launcher.Settings = JsonConvert.DeserializeObject<Setting>(settingsFile);
            }
            catch
            {
                File.Delete(Launcher.SettingsFile);

                Init();
            }
        }

        public static void Save()
        {
            if (!Directory.Exists(Launcher.SettingsFolder))
            {
                Directory.CreateDirectory(Launcher.SettingsFolder);
            }

            if (File.Exists(Launcher.SettingsFile))
            {
                File.Delete(Launcher.SettingsFile);
            }

            var settingsFile = JsonConvert.SerializeObject(Launcher.Settings);

            File.WriteAllText(Launcher.SettingsFile, settingsFile);
        }
    }
}
