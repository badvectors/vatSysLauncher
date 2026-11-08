using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Principal;
using System.Windows;
using vatSysLauncher.Models;

namespace vatSysLauncher.Controllers
{
    public class Utility
    {
        private static readonly HttpClient HttpClient = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            // api.github.com rejects requests without a User-Agent.
            client.DefaultRequestHeaders.UserAgent.ParseAdd("vatSysLauncher");
            return client;
        }

        public static UpdaterResult EmptyDirectory(string directory)
        {
            var result = new UpdaterResult();

            if (Directory.Exists(directory))
            {
                result.Add($"Emptying directory: {directory}.");

                try
                {
                    DirectoryInfo dir = new(directory);

                    foreach (var file in dir.GetFiles())
                    {
                        file.Delete();
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    RestartAsAdministrator();

                    result.Add($"Could not empty directory as administrator access was not provided");

                    return result;
                }
                catch (Exception ex)
                {
                    result.Add($"Could not directory directory: {ex.Message}");

                    return result;
                }
            }

            result.Success = true;

            return result;
        }

        public static UpdaterResult CreateDirectory(string directory)
        {
            var result = new UpdaterResult();

            if (Directory.Exists(directory))
            {
                return EmptyDirectory(directory);
            }

            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (UnauthorizedAccessException)
            {
                RestartAsAdministrator();

                result.Add($"Could not create directory as administrator access was not provided");

                return result;
            }
            catch (Exception ex)
            {
                result.Add($"Could not create directory: {ex.Message}");

                return result;
            }

            result.Success = true;

            return result;
        }

        public static UpdaterResult DeleteDirectory(string directory)
        {
            var result = new UpdaterResult();

            if (Directory.Exists(directory))
            {
                result.Add($"Deleting directory: {directory}");

                try
                {
                    DirectoryInfo dir = new(directory);

                    SetAttributesNormal(dir);

                    dir.Delete(true);
                }
                catch (UnauthorizedAccessException)
                {
                    RestartAsAdministrator();

                    result.Add($"Could not delete directory as administrator access was not provided");

                    return result;
                }
                catch (Exception ex)
                {
                    result.Add($"Could not delete directory: {ex.Message}");

                    return result;
                }
            }

            result.Success = true;

            return result;
        }

        public static UpdaterResult ExtractZip(string zipFile, string toDirectory)
        {
            var result = new UpdaterResult();

            result.Add("Extracting plugin.");

            try
            {
                ZipFile.ExtractToDirectory(zipFile, toDirectory);
            }
            catch (Exception ex)
            {
                result.Add($"Could not extract file: {ex.Message}");

                if (ex.InnerException != null)
                {
                    result.Add($"-> {ex.InnerException.Message}");
                }

                return result;
            }

            var subdirectories = Directory.GetDirectories(toDirectory);

            if (subdirectories.Length == 1 && Directory.GetFiles(toDirectory).Length == 0)
            {
                var files = Directory.GetFiles(subdirectories[0]);

                foreach (var file in files)
                {
                    var fileName = file.Split("\\").Last();
                    File.Move(file, Path.Combine(toDirectory, fileName));
                }
            }

            foreach (var file in Directory.GetFiles(toDirectory))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            result.Add("Extract completed.");

            result.Success = true;

            return result;
        }

        public static UpdaterResult DeleteFile(string file)
        {
            var result = new UpdaterResult();

            if (!File.Exists(file))
            {
                result.Add("File does not exist.");

                return result;
            }

            try
            {
                File.Delete(file);
            }
            catch (Exception ex)
            {
                result.Add($"Could not delete file: {ex.Message}");

                return result;
            }

            result.Add("File deleted.");

            result.Success = true;

            return result;
        }

        public static async Task<UpdaterResult> DownloadFile(string url, string name = "Temp.zip")
        {
            var result = new UpdaterResult();

            if (string.IsNullOrWhiteSpace(url))
            {
                result.Add("No download link was found.");

                return result;
            }

            result.Add($"Downloading from: {url}.");

            var path = Path.Combine(Launcher.WorkingDirectory, name);

            try
            {
                using var downloadResponse = await HttpClient.GetAsync(url);

                if (downloadResponse.IsSuccessStatusCode)
                {
                    await SaveResponse(downloadResponse, path);

                    result.Add("Download completed.");
                    result.Success = true;
                    return result;
                }

                result.Add($"Could not download file: {downloadResponse.StatusCode}.");
            }
            catch (Exception ex)
            {
                result.Add($"Could not download file: {ex.Message}");
            }

            // github.com release links sometimes fail (e.g. 504 from GitHub's edge) while the API still works.
            var apiUrl = await GetGitHubApiAssetUrl(url);

            if (apiUrl == null) return result;

            result.Add("Retrying via the GitHub API.");

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                request.Headers.Accept.ParseAdd("application/octet-stream");

                using var apiResponse = await HttpClient.SendAsync(request);

                if (!apiResponse.IsSuccessStatusCode)
                {
                    result.Add($"Could not download file: {apiResponse.StatusCode}.");
                    return result;
                }

                await SaveResponse(apiResponse, path);
            }
            catch (Exception ex)
            {
                result.Add($"Could not download file: {ex.Message}");
                return result;
            }

            result.Add("Download completed.");

            result.Success = true;

            return result;
        }

        private static async Task SaveResponse(HttpResponseMessage response, string path)
        {
            using var stream = await response.Content.ReadAsStreamAsync();
            using var file = File.Create(path);
            await stream.CopyToAsync(file);
        }

        private static readonly System.Text.RegularExpressions.Regex GitHubReleaseRegex =
            new(@"^https://github\.com/([^/]+)/([^/]+)/releases/download/([^/]+)/([^/?#]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        private static async Task<string> GetGitHubApiAssetUrl(string url)
        {
            var match = GitHubReleaseRegex.Match(url);

            if (!match.Success) return null;

            var (owner, repo, tag, asset) = (match.Groups[1].Value, match.Groups[2].Value, Uri.UnescapeDataString(match.Groups[3].Value), Uri.UnescapeDataString(match.Groups[4].Value));

            try
            {
                using var response = await HttpClient.GetAsync($"https://api.github.com/repos/{owner}/{repo}/releases/tags/{Uri.EscapeDataString(tag)}");

                if (!response.IsSuccessStatusCode) return null;

                var release = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(await response.Content.ReadAsStringAsync());

                var match2 = release?["assets"]?.FirstOrDefault(a => string.Equals((string)a["name"], asset, StringComparison.OrdinalIgnoreCase));

                return (string)match2?["url"];
            }
            catch
            {
                return null;
            }
        }

        public static bool IsRunningAsAdministrator()
        {
            // Get current Windows user
            WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();

            // Get current Windows user principal
            WindowsPrincipal windowsPrincipal = new(windowsIdentity);

            // Return TRUE if user is in role "Administrator"
            return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void RestartAsAdministrator()
        {
            if (IsRunningAsAdministrator()) return;

            if (File.Exists(Launcher.RestartFile))
            {
                File.Delete(Launcher.RestartFile);
            }

            File.WriteAllLines(Launcher.RestartFile, Updater.GetCurrentCommands());

            // Setting up start info of the new process of the same application
            ProcessStartInfo processStartInfo = new(Environment.ProcessPath)
            {
                UseShellExecute = true,
                Verb = "runas"
            };

            try
            {
                // Start the application as new process
                Process.Start(processStartInfo);

                // Shut down the current (old) process
                Application.Current.Shutdown();
            }
            catch
            {
                string messageBoxText = "You must grant administrator access to install plugins in the base directory.";
                string caption = "vatSys Launcher";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Error;
                MessageBoxResult result;
                result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                switch (result)
                {
                    case MessageBoxResult.OK:
                        break;
                }
                return;
            }
        }

        public static void SetAttributesNormal(DirectoryInfo dir)
        {
            foreach (var subDir in dir.GetDirectories())
            {
                SetAttributesNormal(subDir);

                subDir.Attributes = FileAttributes.Normal;
            }
            foreach (var file in dir.GetFiles())
            {
                file.Attributes = FileAttributes.Normal;
            }
            dir.Attributes = FileAttributes.Normal;
        }

        public static string GetFileVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        public static async Task CheckVersion()
        {
            var versionResponse = await Launcher.HttpClient.GetAsync(Launcher.VersionUrl);

            if (!versionResponse.IsSuccessStatusCode) return;

            var content = await versionResponse.Content.ReadAsStringAsync();

            try
            {
                var version = JsonConvert.DeserializeObject<LauncherVersion>(content);

                if (version.Version == GetFileVersion()) return;

                string messageBoxText = $"You must update vatSys Launcher to version {version.Version} to continue.";
                string caption = "vatSys Launcher";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Exclamation;
                MessageBoxResult result;
                result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                switch (result)
                {
                    case MessageBoxResult.OK:
                        await Updater.UpdateSelf(version.DownloadUrl);
                        break;
                }
                return;
            }
            catch { }
        }
    }
}
