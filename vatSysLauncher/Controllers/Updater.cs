using System.Diagnostics;
using System.IO;
using System.Windows;
using vatSysLauncher.Models;

namespace vatSysLauncher.Controllers
{
    public class Updater
    {
        private static List<string> CurrentCommands = [];

        public static List<string> GetCurrentCommands() => CurrentCommands;

        /// <summary>
        /// Function will update all profiles and plugins with an available update.
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> UpdateAll()
        {
            Start();

            CurrentCommands = [.. Launcher.Changes];

            return await Go();
        }

        public static async Task<bool> Run(string command)
        {
            Start();

            CurrentCommands = [command];

            return await Go();
        }

        public static async Task<bool> Run(string[] commands)
        {
            Start();

            CurrentCommands = [.. commands];

            return await Go();
        }

        private static void Start()
        {
            Launcher.SetCanvas("Updater");
            ClearLog();
        }

        private static async Task<bool> Go()
        {
            foreach (var command in CurrentCommands.ToList())
            {
                var success = await Go(command);

                if (!success)
                {
                    return false;
                }
            }

            return true;
        }

        private static async Task<bool> Go(string command)
        {
            var split = command.Split('|');

            if (split[0] == "Delete")
            {
                if (split[1] == "Profile")
                {
                    // delete directory

                    var directory = Path.Combine(Launcher.Settings.ProfileDirectory, split[2]);

                    var success = RunDelete(directory);

                    if (!success) return false;

                    // if success return to profile screen

                    await Profiles.Init();

                    await Plugins.Init();

                    Launcher.SetCanvas("Profiles");
                }
                else if (split[1] == "Plugin")
                {
                    // delete directory

                    var success = RunDelete(split[3]);

                    if (!success) return false;

                    // if success return to profile screen

                    await Plugins.Init();

                    Launcher.SetCanvas("Plugins");
                }
            }
            else if (split[0] == "Install")
            {
                if (split[1] == "Profile")
                {
                    var profileOption = Launcher.ProfileOptions.FirstOrDefault(x => x.Title == split[2]);

                    if (profileOption == null) return false;

                    var directory = Path.Combine(Launcher.Settings.ProfileDirectory, split[2]);

                    if (Path.Exists(directory)) return false;

                    var success = await RunProfileInstall(profileOption);

                    if (!success) return false;

                    // if success return to profile screen

                    await Profiles.Init();

                    await Plugins.Init();

                    Launcher.SetCanvas("Profiles");
                }
                else if (split[1] == "Plugin")
                {
                    //Install|Plugin|PluginName|directory
                    //Install|Plugin|badvectors/SimulatorPlugin|C:\Program Files (x86)\vatSys\bin\Plugins

                    var pluginResponse = Launcher.PluginsAvailable.FirstOrDefault(x => x.Name == split[2]);

                    if (pluginResponse == null) return false;

                    var success = await RunPluginInstall(pluginResponse, split[3]);

                    if (!success) return false;

                    // if success return to plugins screen

                    await Plugins.Init();

                    Launcher.SetCanvas("Plugins");
                }

            }
            else if (split[0] == "Update")
            {
                if (split[1] == "Profile")
                {
                    var profileOption = Launcher.ProfileOptions.FirstOrDefault(x => x.Title == split[2]);

                    if (profileOption == null) return false;

                    var directory = Path.Combine(Launcher.Settings.ProfileDirectory, split[2]);

                    if (!Path.Exists(directory)) return false;

                    var success = RunDelete(directory);

                    if (!success) return false;

                    success = await RunProfileInstall(profileOption);

                    if (!success) return false;

                    // if success return to profile screen

                    await Profiles.Init();

                    await Plugins.Init();

                    Launcher.SetCanvas("Profiles");
                }
                else if (split[1] == "Plugin")
                {
                    //Update|Plugin|PluginName|directory

                    // delete directory

                    var success = RunDelete(split[3]);

                    if (!success) return false;

                    var pluginResponse = Launcher.PluginsAvailable.FirstOrDefault(x => x.Name == split[2]);

                    if (pluginResponse == null) return false;

                    success = await RunPluginInstall(pluginResponse, split[3]);

                    if (!success) return false;

                    await Plugins.Init();

                    Launcher.SetCanvas("Plugins");
                }
            }

            CurrentCommands.Remove(command);

            return true;
        }

        private static bool RunDelete(string directory)
        {
            // delete directory

            var result = Utility.DeleteDirectory(directory);

            //SetLog(result);

            if (!result.Success) return false;

            return true;
        }


        private static async Task<bool> RunPluginInstall(PluginResponse pluginResponse, string installTo, string name = "Temp.zip")
        {
            // create working directory

            var workingResult = Utility.CreateDirectory(Launcher.WorkingDirectory);

            if (!workingResult.Success) return false;

            // find download file

            await Plugins.GetVersion(pluginResponse);

            // download plugin

            var downloadResult = await Utility.DownloadFile(pluginResponse.DownloadUrl);

            if (!downloadResult.Success) return false;

            // create directory

            var directoryResult = Utility.CreateDirectory(installTo);

            if (!directoryResult.Success) return false;

            // extract profile

            var extractResult = Utility.ExtractZip(Path.Combine(Launcher.WorkingDirectory, name), installTo);

            if (!extractResult.Success) return false;

            // delete working directory

            var deleteResult = Utility.DeleteDirectory(Launcher.WorkingDirectory);

            if (!deleteResult.Success) return false;

            return true;
        }

        private static async Task<bool> RunProfileInstall(ProfileOption profileOption)
        {
            // create working directory

            var workingResult = Utility.CreateDirectory(Launcher.WorkingDirectory);

            if (!workingResult.Success) return false;

            // download plugin

            var downloadResult = await Utility.DownloadFile(profileOption.DownloadUrl);

            if (!downloadResult.Success) return false;

            // create directory

            var directoryResult = Utility.CreateDirectory(Path.Combine(Launcher.Settings.ProfileDirectory, profileOption.Title));

            if (!directoryResult.Success) return false;

            // extract profile

            var extractResult = Utility.ExtractZip(Path.Combine(Launcher.WorkingDirectory, "Temp.zip"), Path.Combine(Launcher.Settings.ProfileDirectory, profileOption.Title));

            if (!extractResult.Success) return false;

            // delete working directory

            var deleteResult = Utility.DeleteDirectory(Launcher.WorkingDirectory);

            if (!deleteResult.Success) return false;

            return true;
        }

        public static async Task<bool> UpdateSelf(string url)
        {
            Start();

            // create working directory

            var workingResult = Utility.CreateDirectory(Launcher.WorkingDirectory);

            if (!workingResult.Success) return false;

            // download file

            var downloadResult = await Utility.DownloadFile(url, "Launcher.exe");

            if (!downloadResult.Success) return false;

            // run file
            ProcessStartInfo processStartInfo = new(Path.Combine(Launcher.WorkingDirectory, "Launcher.exe"));

            // Start the application as new process
            Process.Start(processStartInfo);

            // Shut down the current (old) process
            Application.Current.Shutdown();

            return true;
        }

        public static void ClearLog()
        {
            Launcher.MainViewModel.UpdaterLog = string.Empty;
        }

        public static void SetLog(string log)
        {
            Launcher.MainViewModel.UpdaterLog += log + Environment.NewLine;
        }
    }
}