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
            var commands = CurrentCommands.ToList();

            var success = true;

            try
            {
                foreach (var command in commands)
                {
                    if (!await Go(command))
                    {
                        success = false;
                        break;
                    }
                }
            }
            catch
            {
                success = false;
            }

            // always recheck for updates afterwards, whether or not every command succeeded
            await Refresh(commands);

            return success;
        }

        private static async Task Refresh(List<string> commands)
        {
            CurrentCommands.Clear();

            try
            {
                await Profiles.Init();

                await Plugins.Init();
            }
            catch { }

            Launcher.GetChanges();

            var type = commands.Select(x => x.Split('|')).LastOrDefault(x => x.Length > 1)?[1];

            Launcher.SetCanvas(type == "Plugin" ? "Plugins" : "Profiles");
        }

        private static async Task<bool> Go(string command)
        {
            var split = command.Split('|');

            var success = false;

            if (split[0] == "Delete")
            {
                if (split[1] == "Profile")
                {
                    // delete directory

                    var directory = Path.Combine(Launcher.Settings.ProfileDirectory, split[2]);

                    success = RunDelete(directory);

                    if (!success) return false;
                }
                else if (split[1] == "Plugin")
                {
                    // delete directory

                    success = RunDelete(split[3]);

                    if (!success) return false;
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

                    success = await RunProfileInstall(profileOption);

                    if (!success) return false;
                }
                else if (split[1] == "Plugin")
                {
                    //Install|Plugin|PluginName|directory
                    //Install|Plugin|badvectors/SimulatorPlugin|C:\Program Files (x86)\vatSys\bin\Plugins

                    var pluginResponse = Launcher.PluginsAvailable.FirstOrDefault(x => x.Name == split[2]);

                    if (pluginResponse == null) return false;

                    success = await RunPluginInstall(pluginResponse, split[3]);
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

                    // download first so a failed download doesn't leave the profile deleted

                    success = await RunDownload(profileOption.DownloadUrl);

                    if (!success) return false;

                    success = RunDelete(directory);

                    if (!success) return false;

                    success = RunExtract(directory);

                    if (!success) return false;
                }
                else if (split[1] == "Plugin")
                {
                    //Update|Plugin|PluginName|directory

                    var pluginResponse = Launcher.PluginsAvailable.FirstOrDefault(x => x.Name == split[2]);

                    if (pluginResponse == null) return false;

                    // download first so a failed download doesn't leave the plugin deleted

                    success = await RunDownload(pluginResponse.DownloadUrl);

                    if (!success) return false;

                    success = RunDelete(split[3]);

                    if (!success) return false;

                    success = RunExtract(split[3]);

                    if (!success) return false;
                }
            }

            CurrentCommands.Remove(command);

            return success;
        }

        private static bool RunDelete(string directory)
        {
            // delete directory

            var result = Utility.DeleteDirectory(directory);

            //SetLog(result);

            if (!result.Success) return false;

            return true;
        }


        private const string DownloadName = "Temp.zip";

        private static string DownloadPath => Path.Combine(Launcher.WorkingDirectory, DownloadName);

        private static async Task<bool> RunPluginInstall(PluginResponse pluginResponse, string installTo)
        {
            if (!await RunDownload(pluginResponse.DownloadUrl)) return false;

            return RunExtract(installTo);
        }

        private static async Task<bool> RunProfileInstall(ProfileOption profileOption)
        {
            if (!await RunDownload(profileOption.DownloadUrl)) return false;

            return RunExtract(Path.Combine(Launcher.Settings.ProfileDirectory, profileOption.Title));
        }

        private static async Task<bool> RunDownload(string url)
        {
            // create (or empty) working directory

            var workingResult = Utility.CreateDirectory(Launcher.WorkingDirectory);

            if (!workingResult.Success) return false;

            // download file

            var downloadResult = await Utility.DownloadFile(url, DownloadName);

            return downloadResult.Success;
        }

        private static bool RunExtract(string installTo)
        {
            // create directory

            var directoryResult = Utility.CreateDirectory(installTo);

            if (!directoryResult.Success) return false;

            // extract

            var extractResult = Utility.ExtractZip(DownloadPath, installTo);

            if (!extractResult.Success) return false;

            // delete downloaded file

            var deleteResult = Utility.DeleteFile(DownloadPath);

            return deleteResult.Success;
        }

        public static async Task<bool> UpdateSelf(string url)
        {
            Start();

            // download the installer to its own folder, outside the working directory, so the
            // still-running installer never locks files that later installs need to clear

            var updateDirectory = Path.Combine(Launcher.SettingsFolder, "Update");

            var updateResult = Utility.CreateDirectory(updateDirectory);

            if (!updateResult.Success) return false;

            // download file

            var downloadResult = await Utility.DownloadFile(url, "Launcher.exe", updateDirectory);

            if (!downloadResult.Success) return false;

            // run file
            ProcessStartInfo processStartInfo = new(Path.Combine(updateDirectory, "Launcher.exe"));

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