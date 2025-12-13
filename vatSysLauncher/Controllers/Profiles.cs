using Newtonsoft.Json;
using System.IO;
using System.Windows;
using System.Xml.Serialization;
using vatSysLauncher.Models;

namespace vatSysLauncher.Controllers
{
    public class Profiles
    {
        public static async Task Init()
        {
            Launcher.ProfileOptions.Clear();

            Launcher.MainViewModel.ProfilesLoading = Visibility.Visible;

            var profiles = new List<ProfileOption>();

            var installed = GetInstalled();

            profiles.AddRange(installed);

            var available = await GetAvailable();

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

            Launcher.ProfileOptions = profiles;

            Launcher.MainViewModel.ProfilesLoading = Visibility.Hidden;

            Launcher.MainViewModel.ProfilesList = Launcher.ProfileOptions;

            var locations = new List<string>
            {
                Launcher.PluginsBaseDirectoryName
            };
            foreach (var profile in profiles.Where(x => x.Installed))
            {
                locations.Add(profile.Title);
            }
            Launcher.MainViewModel.PluginsLocations = locations;
        }

        private static async Task<List<ProfileOption>> GetAvailable()
        {
            var profiles = new List<ProfileOption>();

            var response = await Launcher.HttpClient.GetAsync(Launcher.ProfilesUrl);

            if (!response.IsSuccessStatusCode) return profiles;

            var responseString = await response.Content.ReadAsStringAsync();

            var available = JsonConvert.DeserializeObject<List<ProfilesResponse>>(responseString);

            foreach (var profile in available)
            {
                var profileOption = new ProfileOption(profile.name, profile.path);

                var profileFile = $"{profile.path}/Profile.xml";

                var profileResponse = await Launcher.HttpClient.GetAsync(profileFile);

                var contents = await profileResponse.Content.ReadAsStringAsync();

                profileOption.CurrentVersion = GetVersion(contents);

                profiles.Add(profileOption);
            }

            return profiles;
        }

        private static List<ProfileOption> GetInstalled()
        {
            var profiles = new List<ProfileOption>();

            if (Launcher.Settings == null || string.IsNullOrWhiteSpace(Launcher.Settings.ProfileDirectory)) return profiles;

            foreach (var directory in Directory.GetDirectories(Launcher.Settings.ProfileDirectory))
            {
                if (directory == Launcher.WorkingDirectory) continue;

                var profileOption = new ProfileOption(directory.Split('\\').Last(), null, true);

                var profileFile = Path.Combine(directory, "Profile.xml");

                if (File.Exists(profileFile))
                {
                    var contents = File.ReadAllText(profileFile);

                    profileOption.LocalVersion = GetVersion(contents);
                }

                profiles.Add(profileOption);
            }

            return profiles;
        }

        private static string GetVersion(string contents)
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
    }
}
