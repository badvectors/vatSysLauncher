using Microsoft.Win32;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Xml.Serialization;
using vatSysLauncher;
using vatSysLauncher.Models;

namespace vatSysManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Version Version = new(1, 17);

        private static readonly string VatsysProcessName = "vatSys";
        private static readonly DispatcherTimer VatSysTimer = new();
        private static Canvas CurrentCanvas = null;
        private static readonly HttpClient HttpClient = new();

        private static List<ProfileOption> ProfileOptions = [];
        private static List<PluginResponse> PluginsAvailable = [];
        private static List<PluginInstalled> PluginsInstalled = [];
        private static List<string> Changes = [];
        private static List<string> CurrentCommands = [];

        public MainWindow()
        {
            InitializeComponent();

            _ = Init();
        }

        private async Task Init()
        {
            VersionText.Text = $"Version {Version}";

            InitSettings();

            HomeButton_Click(null, null);

            HomeButton.IsEnabled = false;
            PluginsButton.IsEnabled = false;
            ProfilesButton.IsEnabled = false;
            SetupButton.IsEnabled = false;
            WaitTextBlock.Visibility = Visibility.Visible;
            LaunchButton.Visibility = Visibility.Hidden;

            await CheckVersion();

            await InitProfiles();

            await InitPlugins();

            await CheckForRestart();

            HomeButton.IsEnabled = true;
            PluginsButton.IsEnabled = true;
            ProfilesButton.IsEnabled = true;
            SetupButton.IsEnabled = true;
            WaitTextBlock.Visibility = Visibility.Hidden;
            LaunchButton.Visibility = Visibility.Visible;

            VatSysCheck();

            VatSysTimer.Tick += VatSysTimer_Tick;
            VatSysTimer.Interval = new TimeSpan(0, 0, 1);

            VatSysTimer.Start();

            Utility.DeleteDirectory(Constants.WorkingDirectory);
        }

        private async Task CheckVersion()
        {
            var versionResponse = await HttpClient.GetAsync(Constants.VersionUrl);

            if (!versionResponse.IsSuccessStatusCode) return;

            var content = await versionResponse.Content.ReadAsStringAsync();

            try
            {
                var version = JsonConvert.DeserializeObject<LauncherVersion>(content);

                if (version.Version == Version.ToString()) return;

                string messageBoxText = $"You must update vatSys Launcher to version {version.Version} to continue.";
                string caption = "vatSys Launcher";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Exclamation;
                MessageBoxResult result;
                result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                switch (result)
                {
                    case MessageBoxResult.OK:
                        await UpdateSelf(version.DownloadUrl);
                        break;
                }
                return;
            }
            catch { }
        }

        private async Task<bool> UpdateSelf(string url)
        {
            UpdaterCanvasMode();

            // create working directory

            var workingResult = Utility.CreateDirectory(Constants.WorkingDirectory);

            UpdaterOutput(workingResult);

            if (!workingResult.Success) return false;

            // download file

            var downloadResult = await Utility.DownloadFile(url, "Launcher.exe");

            UpdaterOutput(downloadResult);

            if (!downloadResult.Success) return false;

            // run file
            ProcessStartInfo processStartInfo = new(Path.Combine(Constants.WorkingDirectory, "Launcher.exe"));

            // Start the application as new process
            Process.Start(processStartInfo);

            // Shut down the current (old) process
            Application.Current.Shutdown();

            return true;
        }

        private async Task<bool> UpdateAll()
        {
            UpdaterCanvasMode();

            CurrentCommands = [.. Changes];

            foreach (var code in CurrentCommands)
            {
                var success = await UpdaterAction(code);

                if (!success)
                {
                    return false;
                }
            }

            return true;
        }

        private void GetChanges()
        {
            Changes.Clear();

            foreach (var plugin in PluginsInstalled)
            {
                if (!plugin.UpdateAvailable) continue;

                Changes.Add(plugin.UpdateCommand);
            }

            foreach (var profile in ProfileOptions)
            {
                if (!profile.UpdateAvailable) continue;

                Changes.Add(profile.UpdateCommand);
            }

            if (Changes.Count == 0)
            {
                UpdateText.Visibility = Visibility.Hidden;
            }
            else
            {
                UpdateText.Visibility = Visibility.Visible;
                var updateText = "update";
                if (Changes.Count > 1) updateText = "updates";
                UpdateText.Text = $"{Changes.Count} {updateText} to be installed.";
            }

        }

        private async Task CheckForRestart()
        {
            if (!File.Exists(Constants.RestartFile)) return;

            CurrentCommands = [.. File.ReadAllLines(Constants.RestartFile)];

            UpdaterCanvasMode();

            foreach (var code in CurrentCommands)
            {
                var success = await UpdaterAction(code);
            }

            CurrentCommands.Clear();

            if (File.Exists(Constants.RestartFile))
            {
                File.Delete(Constants.RestartFile);
            }
        }

        private async Task InitPlugins()
        {
            PluginsAvailable.Clear();

            PluginsInstalled.Clear();

            PluginsLoading.Visibility = Visibility.Visible;

            var available = await PluginsGetAvailable();

            PluginsAvailable = available;

            var plugins = new List<string>();

            foreach (var plugin in available)
            {
                plugins.Add(plugin.Name);
            }

            PluginsOptionsComboBox.ItemsSource = plugins;

            PluginsLoading.Visibility = Visibility.Hidden;

            var pluginOptions = new List<PluginInstalled>();

            var installed = PluginsGetInstalled();

            PluginsInstalled = installed;

            PluginsList.ItemsSource = installed;

            GetChanges();
        }

        private static async Task<DateTime> PluginsLastRefresh()
        {
            if (!File.Exists(Constants.UpdateFile)) return DateTime.MinValue;

            var lastUpdateText = await File.ReadAllTextAsync(Constants.UpdateFile);

            var lastUpdateOk = DateTime.TryParse(lastUpdateText, out DateTime lastUpdate);

            if (!lastUpdateOk) return DateTime.MinValue;

            return lastUpdate;
        }

        private static async Task<List<PluginResponse>> PluginsGetSaved()
        {
            var output = new List<PluginResponse>();

            if (!File.Exists(Constants.PluginsFile)) return output;

            var pluginsText = await File.ReadAllTextAsync(Constants.PluginsFile);

            try
            {
                output = JsonConvert.DeserializeObject<List<PluginResponse>>(pluginsText);
            }
            catch
            {
                return output;
            }

            return output;
        }

        private static async Task PluginsSave(List<PluginResponse> plugins)
        {
            if (File.Exists(Constants.PluginsFile))
            {
                File.Delete(Constants.PluginsFile);
            }

            if (File.Exists(Constants.UpdateFile))
            {
                File.Delete(Constants.UpdateFile);
            }

            var content = JsonConvert.SerializeObject(plugins);

            await File.WriteAllTextAsync(Constants.PluginsFile, content);

            var lastUpdate = DateTime.UtcNow.ToString();

            await File.WriteAllTextAsync(Constants.UpdateFile, lastUpdate);
        }

        private async Task<List<PluginResponse>> PluginsGetAvailable()
        {
            var plugins = new List<PluginResponse>();

            var lastRefresh = await PluginsLastRefresh();

            if (lastRefresh > DateTime.UtcNow.Subtract(TimeSpan.FromHours(1)))
            {
                return await PluginsGetSaved();
            }

            var response = await HttpClient.GetAsync(Constants.PluginsUrl);

            if (!response.IsSuccessStatusCode) return plugins;

            var content = await response.Content.ReadAsStringAsync();

            var pluginResponses = JsonConvert.DeserializeObject<List<PluginResponse>>(content);

            foreach (var pluginResponse in pluginResponses)
            {
                await GetPluginVersion(pluginResponse);
            }

            plugins.AddRange(pluginResponses);

            await PluginsSave(plugins);
    
            return plugins;
        }

        private static List<PluginInstalled> PluginsGetInstalled()
        {
            var plugins = new List<PluginInstalled>();

            if (Constants.Settings == null || 
                string.IsNullOrWhiteSpace(Constants.Settings.BaseDirectory) || 
                string.IsNullOrWhiteSpace(Constants.Settings.ProfileDirectory)) return plugins;

            foreach (var directory in Directory.GetDirectories(Constants.Settings.ProfileDirectory))
            {
                if (directory == Constants.WorkingDirectory) continue;

                var profile = directory.Split('\\').Last();

                var subdirectories = Directory.GetDirectories(directory);

                var pluginDirectory = subdirectories.FirstOrDefault(x => x.EndsWith("Plugins"));

                if (pluginDirectory == null) continue;

                foreach (var dir in Directory.GetDirectories(pluginDirectory))
                {
                    var files = Directory.GetFiles(dir);

                    foreach (var file in files)
                    {
                        var split = file.Split('\\');

                        var pluginAvailable = PluginsAvailable.FirstOrDefault(x => x.DllName == split.Last());

                        if (pluginAvailable == null) continue;

                        var localVersion = new Version();

                        try
                        {
                            var versionInfo = FileVersionInfo.GetVersionInfo(file);
                            localVersion = new Version(versionInfo.FileVersion);
                        }
                        catch { }

                        plugins.Add(new PluginInstalled(pluginAvailable.Name, profile, dir, pluginAvailable.Version, localVersion));

                        break;
                    }
                }
            }

            if (!Directory.Exists(Constants.PluginsBaseDirectory)) return plugins;

            foreach (var dir in Directory.GetDirectories(Constants.PluginsBaseDirectory))
            {
                var files = Directory.GetFiles(dir);

                foreach (var file in files)
                {
                    var split = file.Split('\\');

                    var pluginAvailable = PluginsAvailable.FirstOrDefault(x => x.DllName == split.Last());

                    if (pluginAvailable == null) continue;

                    var localVersion = new Version();

                    try
                    {
                        var versionInfo = FileVersionInfo.GetVersionInfo(file);
                        localVersion = new Version(versionInfo.FileVersion);
                    }
                    catch { }

                    plugins.Add(new PluginInstalled(pluginAvailable.Name, Constants.PluginsBaseDirectoryName, dir, pluginAvailable.Version, localVersion));

                    break;
                }
            }

            return plugins;
        }

        private async Task InitProfiles()
        {
            ProfileOptions.Clear();
            
            ProfilesLoading.Visibility = Visibility.Visible;

            var profiles = new List<ProfileOption>();

            var installed = ProfilesGetInstalled();

            profiles.AddRange(installed);

            var available = await ProfilesGetAvailable();

            foreach (var profile in available)
            {
                var existing = profiles.FirstOrDefault(x => x.Title == profile.Title);
                if (existing != null)
                {
                    existing.Url = profile.Url;
                    existing.CurrentVersion = profile.CurrentVersion;
                    continue;
                }
                profiles.Add(profile);
            }

            ProfileOptions = profiles;

            ProfilesLoading.Visibility = Visibility.Hidden;

            ProfilesList.ItemsSource = ProfileOptions;

            var locations = new List<string>
            {
                Constants.PluginsBaseDirectoryName
            };
            foreach (var profile in profiles.Where(x => x.Installed))
            {
                locations.Add(profile.Title);
            }
            PluginsLocationsComboBox.ItemsSource = locations;
        }

        private void InitSettings()
        {
            if (!Directory.Exists(Constants.SettingsFolder))
            {
                Directory.CreateDirectory(Constants.SettingsFolder);
            }

            if (!File.Exists(Constants.SettingsFile))
            {
                var settings = new Settings();

                var defaultProfileDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "vatSys Files", "Profiles");

                var defaultBaseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "vatSys");

                if (Directory.Exists(defaultProfileDirectory))
                {
                    settings.ProfileDirectory = defaultProfileDirectory;
                }

                if (Directory.Exists(defaultBaseDirectory))
                {
                    settings.BaseDirectory = defaultBaseDirectory;
                }

                Constants.Settings = settings;

                var settingsFile = JsonConvert.SerializeObject(Constants.Settings);

                File.WriteAllText(Constants.SettingsFile, settingsFile);

                return;
            }

            try
            {
                var settingsFile = File.ReadAllText(Constants.SettingsFile);

                Constants.Settings = JsonConvert.DeserializeObject<Settings>(settingsFile);
            }
            catch 
            {
                File.Delete(Constants.SettingsFile);

                InitSettings();
            }
        }

        private void VatSysTimer_Tick(object sender, EventArgs e)
        {
            VatSysCheck();
        }

        private void VatSysCheck()
        {
            // Get any running vatSys processes.
            var vatsysProcesses = Process.GetProcessesByName(VatsysProcessName);

            if (vatsysProcesses.Length > 0)
            {
                InitCheckCanvas.Visibility = Visibility.Visible;
                HomeButton.IsEnabled = false;
                HomeCanvas.Visibility = Visibility.Hidden;
                PluginsButton.IsEnabled = false;
                ProfilesButton.IsEnabled = false;
                SetupButton.IsEnabled = false;
                SetupCanvas.Visibility = Visibility.Hidden;
                ProfilesCanvas.Visibility = Visibility.Hidden;
                UpdaterCanvas.Visibility = Visibility.Hidden;
            }
            else
            {
                InitCheckCanvas.Visibility = Visibility.Hidden;
                HomeButton.IsEnabled = true;
                PluginsButton.IsEnabled = true;
                ProfilesButton.IsEnabled = true;
                SetupButton.IsEnabled = true;

                if (CurrentCanvas == null) HomeCanvas.Visibility = Visibility.Visible;
                else CurrentCanvas.Visibility = Visibility.Visible;
            }
        }

        private void VatSysClose()
        {
            // Get any running vatSys processes.
            var vatsysProcesses = Process.GetProcessesByName(VatsysProcessName);

            // Kill all running vatSys processes.
            if (vatsysProcesses.Length > 0)
            {
                foreach (var vatsysProcess in vatsysProcesses)
                    vatsysProcess.Kill();
            }
        }

        private void VatSysCloseButton_Click(object sender, RoutedEventArgs e)
        {
            VatSysClose();
        }

        private async void VatSysLaunchButton_Click(object sender, RoutedEventArgs e)
        {
            if (Constants.Settings == null || string.IsNullOrWhiteSpace(Constants.Settings.BaseDirectory)) return;
            
            if (!File.Exists(Constants.VatsysExe))
            {
                string messageBoxText = "Unable to locate vatSys. Update your 'base directory' in the Setup menu.";
                string caption = "vatSys Launder";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Error;
                MessageBoxResult result;
                result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                switch (result)
                {
                    case MessageBoxResult.OK:
                        SetupButton_Click(null, null);
                        break;
                }
                return;
            }

            var success = await UpdateAll();

            if (!success) return;

            Process.Start(Constants.VatsysExe);
            
            Environment.Exit(1);
        }

        private void SetupButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentCanvas = SetupCanvas;
            SetupCanvas.Visibility = Visibility.Visible;
            InitCheckCanvas.Visibility = Visibility.Hidden;
            HomeCanvas.Visibility = Visibility.Hidden;
            ProfilesCanvas.Visibility = Visibility.Hidden;
            UpdaterCanvas.Visibility = Visibility.Hidden;
            PluginsCanvas.Visibility = Visibility.Hidden;

            if (Constants.Settings == null) return;
            BaseDirectoryTextBox.Text = Constants.Settings.BaseDirectory;
            ProfileDirectoryTextBox.Text = Constants.Settings.ProfileDirectory;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentCanvas = HomeCanvas;
            SetupCanvas.Visibility = Visibility.Hidden;
            InitCheckCanvas.Visibility = Visibility.Hidden;
            HomeCanvas.Visibility = Visibility.Visible;
            ProfilesCanvas.Visibility = Visibility.Hidden;
            UpdaterCanvas.Visibility = Visibility.Hidden;
            PluginsCanvas.Visibility = Visibility.Hidden;
        }

        private void ProfilesButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentCanvas = ProfilesCanvas;
            SetupCanvas.Visibility = Visibility.Hidden;
            InitCheckCanvas.Visibility = Visibility.Hidden;
            HomeCanvas.Visibility = Visibility.Hidden;
            ProfilesCanvas.Visibility = Visibility.Visible;
            UpdaterCanvas.Visibility = Visibility.Hidden;
            PluginsCanvas.Visibility = Visibility.Hidden;
        }

        private void PluginsButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentCanvas = PluginsCanvas;
            SetupCanvas.Visibility = Visibility.Hidden;
            InitCheckCanvas.Visibility = Visibility.Hidden;
            HomeCanvas.Visibility = Visibility.Hidden;
            ProfilesCanvas.Visibility = Visibility.Hidden;
            UpdaterCanvas.Visibility = Visibility.Hidden;
            PluginsCanvas.Visibility = Visibility.Visible;
        }

        private static async Task<List<ProfileOption>> ProfilesGetAvailable()
        {
            var profiles = new List<ProfileOption>();

            var response = await HttpClient.GetAsync(Constants.ProfilesUrl);

            if (!response.IsSuccessStatusCode) return profiles;

            var responseString = await response.Content.ReadAsStringAsync();

            var available = JsonConvert.DeserializeObject<List<ProfilesResponse>>(responseString);

            foreach (var profile in available)
            {
                var profileOption = new ProfileOption(profile.name, profile.path);

                var profileFile = $"{profile.path}/Profile.xml";

                var profileResponse = await HttpClient.GetAsync(profileFile);

                var contents = await profileResponse.Content.ReadAsStringAsync();

                profileOption.CurrentVersion = ProfileGetVersion(contents);

                profiles.Add(profileOption);
            }

            return profiles;
        }

        private static List<ProfileOption> ProfilesGetInstalled()
        {
            var profiles = new List<ProfileOption>();

            if (Constants.Settings == null || string.IsNullOrWhiteSpace(Constants.Settings.ProfileDirectory)) return profiles;

            foreach (var directory in Directory.GetDirectories(Constants.Settings.ProfileDirectory))
            {
                if (directory == Constants.WorkingDirectory) continue;

                var profileOption = new ProfileOption(directory.Split('\\').Last(), null, true);

                var profileFile = Path.Combine(directory, "Profile.xml");

                if (File.Exists(profileFile))
                {
                    var contents = File.ReadAllText(profileFile);

                    profileOption.LocalVersion = ProfileGetVersion(contents);
                }

                profiles.Add(profileOption);
            }

            return profiles;
        }

        private static string ProfileGetVersion(string contents)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(Profile));

                using var reader = new StringReader(contents);

                var profileXml = (Profile)serializer.Deserialize(reader);

                if (!string.IsNullOrWhiteSpace(profileXml.Version.Revision))
                {
                    return $"{profileXml.Version.AIRAC}.{profileXml.Version.Revision}";
                }

                return $"{profileXml.Version.AIRAC}";
            }
            catch
            {
                return "ERROR";
            }
        }

        private void UpdaterCanvasMode()
        {
            CurrentCanvas = UpdaterCanvas;
            SetupCanvas.Visibility = Visibility.Hidden;
            InitCheckCanvas.Visibility = Visibility.Hidden;
            HomeCanvas.Visibility = Visibility.Hidden;
            ProfilesCanvas.Visibility = Visibility.Hidden;
            UpdaterCanvas.Visibility = Visibility.Visible;
            PluginsCanvas.Visibility = Visibility.Hidden;

            UpdaterLog.Text = string.Empty;
        }

        private async void UpdaterButton_Click(object sender, RoutedEventArgs e)
        {
            UpdaterCanvasMode();

            await UpdaterAction(((Button)sender).Tag.ToString());
        }

        private async void PluginInstallButton_Click(object sender, RoutedEventArgs e)
        {
            var location = PluginsLocationsComboBox.SelectedValue.ToString();

            var pluginName = PluginsOptionsComboBox.SelectedValue.ToString();

            if (location == null || pluginName == null) return;

            if (location == Constants.PluginsBaseDirectoryName)
            {
                location = Constants.PluginsBaseDirectory;
            }
            else
            {
                location = $"{Constants.Settings.ProfileDirectory}\\{location}\\Plugins";
            }

            var pluginResponse = PluginsAvailable.FirstOrDefault(x => x.Name == pluginName);

            if (pluginResponse == null) return;

            var installCommand = $"Install|Plugin|{pluginResponse.Name}|{location}\\{pluginResponse.DirectoryName}";

            CurrentCommands.Add(installCommand);

            var installTo = Path.Combine(location, pluginResponse.DirectoryName);

            var success = await RunPluginInstall(pluginResponse, installTo);

            if (!success) return;

            CurrentCommands.Remove(installCommand);

            await InitPlugins();

            PluginsButton_Click(null, null);
        }

        private async Task<bool> UpdaterAction(string code)
        {
            if (!CurrentCommands.Contains(code))
            {
                CurrentCommands.Add(code);
            }

            var split = code.Split('|');

            if (split[0] == "Delete")
            {
                if (split[1] == "Profile")
                {
                    // delete directory

                    var directory = Path.Combine(Constants.Settings.ProfileDirectory, split[2]);

                    var success = RunDelete(directory);

                    if (!success) return false;

                    // if success return to profile screen

                    await InitProfiles();

                    await InitPlugins();

                    ProfilesButton_Click(null, null);
                }
                else if (split[1] == "Plugin")
                {
                    // delete directory

                    var success = RunDelete(split[3]);

                    if (!success) return false;

                    // if success return to profile screen

                    await InitPlugins();

                    PluginsButton_Click(null, null);
                }
            }
            else if (split[0] == "Install")
            {
                if (split[1] == "Profile")
                {
                    var profileOption = ProfileOptions.FirstOrDefault(x => x.Title == split[2]);

                    if (profileOption == null) return false;

                    var directory = Path.Combine(Constants.Settings.ProfileDirectory, split[2]);

                    if (Path.Exists(directory)) return false;

                    var success = await RunProfileInstall(profileOption);

                    if (!success) return false;

                    // if success return to profile screen

                    await InitProfiles();

                    await InitPlugins();

                    ProfilesButton_Click(null, null);
                }
                else if (split[1] == "Plugin")
                {
                    //Install|Plugin|PluginName|directory
                    //Install|Plugin|badvectors/SimulatorPlugin|C:\Program Files (x86)\vatSys\bin\Plugins

                    var pluginResponse = PluginsAvailable.FirstOrDefault(x => x.Name == split[2]);

                    if (pluginResponse == null) return false;

                    var success = await RunPluginInstall(pluginResponse, split[3]);

                    if (!success) return false;

                    // if success return to plugins screen

                    await InitPlugins();

                    PluginsButton_Click(null, null);
                }

            }
            else if (split[0] == "Update")
            {
                if (split[1] == "Profile")
                {
                    var profileOption = ProfileOptions.FirstOrDefault(x => x.Title == split[2]);

                    if (profileOption == null) return false;

                    var directory = Path.Combine(Constants.Settings.ProfileDirectory, split[2]);

                    if (!Path.Exists(directory)) return false;

                    var success = RunDelete(directory);

                    if (!success) return false;

                    success = await RunProfileInstall(profileOption);

                    if (!success) return false;

                    // if success return to profile screen

                    await InitProfiles();

                    await InitPlugins();

                    ProfilesButton_Click(null, null);
                }
                else if (split[1] == "Plugin")
                {
                    //Update|Plugin|PluginName|directory

                    // delete directory

                    var success = RunDelete(split[3]);

                    if (!success) return false;

                    var pluginResponse = PluginsAvailable.FirstOrDefault(x => x.Name == split[2]);

                    if (pluginResponse == null) return false;

                    success = await RunPluginInstall(pluginResponse, split[3]);

                    if (!success) return false;

                    await InitPlugins();

                    PluginsButton_Click(null, null);
                }
            }

            CurrentCommands.Remove(code);

            if (File.Exists(Constants.RestartFile))
            {
                File.Delete(Constants.RestartFile);
            }

            return true;
        }

        private async Task<PluginResponse> GetPluginVersion(PluginResponse pluginResponse)
        {
            try
            {
                HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("vatSysManager", "0.0.0"));

                HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

                var latestPage = await HttpClient.GetAsync(pluginResponse.LatestUrl);

                if (!latestPage.IsSuccessStatusCode) return pluginResponse;

                var latestPageContent = await latestPage.Content.ReadAsStringAsync();

                var gitHubResponse = JsonConvert.DeserializeObject<GitHubResponse>(latestPageContent);

                if (string.IsNullOrWhiteSpace(gitHubResponse.tag_name)) return null;

                var tagName = gitHubResponse.tag_name == "latest" ? gitHubResponse.name : gitHubResponse.tag_name;

                var version = new Version(0, 0, 0);

                try
                {
                    tagName = tagName.Replace("Version", "");
                    tagName = tagName.Replace("v", "");
                    tagName = tagName.Replace("-beta", "");
                    tagName = tagName.Trim();
                    version = new Version(tagName);
                }
                catch { }

                pluginResponse.Version = version;

                if (!gitHubResponse.assets.Any()) return pluginResponse;

                pluginResponse.DownloadUrl = gitHubResponse.assets[0].browser_download_url;

                return pluginResponse;
            }
            catch
            {
                return pluginResponse;
            }
        }

        private async Task<bool> RunPluginInstall(PluginResponse pluginResponse, string installTo, string name = "Temp.zip")
        {
            UpdaterCanvasMode();

            // create working directory

            var workingResult = Utility.CreateDirectory(Constants.WorkingDirectory);

            UpdaterOutput(workingResult);

            if (!workingResult.Success) return false;

            // find download file

            await GetPluginVersion(pluginResponse);

            // download plugin

            var downloadResult = await Utility.DownloadFile(pluginResponse.DownloadUrl);

            UpdaterOutput(downloadResult);

            if (!downloadResult.Success) return false;

            // create directory

            var directoryResult = Utility.CreateDirectory(installTo);

            UpdaterOutput(directoryResult);

            if (!directoryResult.Success) return false;

            // extract profile

            var extractResult = Utility.ExtractZip(Path.Combine(Constants.WorkingDirectory, name), installTo);

            UpdaterOutput(extractResult);

            if (!extractResult.Success) return false;

            // delete working directory

            var deleteResult = Utility.DeleteDirectory(Constants.WorkingDirectory);

            UpdaterOutput(deleteResult);

            if (!deleteResult.Success) return false;

            return true;
        }

        private bool RunDelete(string directory)
        {
            // delete directory

            var result = Utility.DeleteDirectory(directory);

            UpdaterOutput(result);

            if (!result.Success) return false;

            return true;
        }

        private async Task<bool> RunProfileInstall(ProfileOption profileOption)
        {
            // create working directory

            var workingResult = Utility.CreateDirectory(Constants.WorkingDirectory);

            UpdaterOutput(workingResult);

            if (!workingResult.Success) return false;

            // download plugin

            var downloadResult = await Utility.DownloadFile(profileOption.DownloadUrl);

            UpdaterOutput(downloadResult);

            if (!downloadResult.Success) return false;

            // create directory

            var directoryResult = Utility.CreateDirectory(Path.Combine(Constants.Settings.ProfileDirectory, profileOption.Title));

            UpdaterOutput(directoryResult);

            if (!directoryResult.Success) return false;

            // extract profile

            var extractResult = Utility.ExtractZip(Path.Combine(Constants.WorkingDirectory, "Temp.zip"), Path.Combine(Constants.Settings.ProfileDirectory, profileOption.Title));

            UpdaterOutput(extractResult);

            if (!extractResult.Success) return false;

            // delete working directory

            var deleteResult = Utility.DeleteDirectory(Constants.WorkingDirectory);

            UpdaterOutput(deleteResult);

            if (!deleteResult.Success) return false;

            return true;
        }

        private void UpdaterOutput(UpdaterResult result)
        {
            foreach (var item in result.Log)
            {
                UpdaterLog.Text += item + Environment.NewLine;
            }
        }




        public class UpdaterResult
        {
            public bool Success { get; set; } = false;
            public List<string> Log { get; set; } = [];
        }

        private void BaseDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new OpenFolderDialog();

            if (folderDialog.ShowDialog() == true)
            {
                Constants.Settings.BaseDirectory = folderDialog.FolderName;

                SettingsSave();

                BaseDirectoryTextBox.Text = Constants.Settings.BaseDirectory;
            }
        }

        private void ProfileDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new OpenFolderDialog();

            if (folderDialog.ShowDialog() == true)
            {
                Constants.Settings.ProfileDirectory = folderDialog.FolderName;

                SettingsSave();

                _ = InitProfiles();

                ProfileDirectoryTextBox.Text = Constants.Settings.ProfileDirectory;
            }
        }

        private void SettingsSave()
        {
            if (!Directory.Exists(Constants.SettingsFolder))
            {
                Directory.CreateDirectory(Constants.SettingsFolder);
            }

            if (File.Exists(Constants.SettingsFile))
            {
                File.Delete(Constants.SettingsFile);
            }

            var settingsFile = JsonConvert.SerializeObject(Constants.Settings);

            File.WriteAllText(Constants.SettingsFile, settingsFile);
        }
    }
}