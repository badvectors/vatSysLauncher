using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Principal;
using System.Windows;
using static vatSysManager.MainWindow;

namespace vatSysLauncher
{
    public class Utility
    {
        private static readonly HttpClient HttpClient = new();

        public static UpdaterResult EmptyDirectory(string directory)
        {
            var result = new UpdaterResult();

            if (Directory.Exists(directory))
            {
                result.Log.Add($"Emptying directory: {directory}.");

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

                    result.Log.Add($"Could not empty directory as administrator access was not provided");

                    return result;
                }
                catch (Exception ex)
                {
                    result.Log.Add($"Could not directory directory: {ex.Message}");

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

                result.Log.Add($"Could not create directory as administrator access was not provided");

                return result;
            }
            catch (Exception ex)
            {
                result.Log.Add($"Could not create directory: {ex.Message}");

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
                result.Log.Add($"Deleting directory: {directory}");

                try
                {
                    DirectoryInfo dir = new(directory);

                    SetAttributesNormal(dir);

                    dir.Delete(true);
                }
                catch (UnauthorizedAccessException)
                {
                    RestartAsAdministrator();

                    result.Log.Add($"Could not delete directory as administrator access was not provided");

                    return result;
                }
                catch (Exception ex)
                {
                    result.Log.Add($"Could not delete directory: {ex.Message}");

                    return result;
                }
            }

            result.Success = true;

            return result;
        }

        public static UpdaterResult ExtractZip(string zipFile, string toDirectory)
        {
            var result = new UpdaterResult();

            result.Log.Add("Extracting plugin.");

            try
            {
                ZipFile.ExtractToDirectory(zipFile, toDirectory);
            }
            catch (Exception ex)
            {
                result.Log.Add($"Could not extract file: {ex.Message}");

                if (ex.InnerException != null)
                {
                    result.Log.Add($"-> {ex.InnerException.Message}");
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

            result.Log.Add("Extract completed.");

            result.Success = true;

            return result;
        }

        public static async Task<UpdaterResult> DownloadFile(string url, string name = "Temp.zip")
        {
            var result = new UpdaterResult();

            if (string.IsNullOrWhiteSpace(url))
            {
                result.Log.Add("No download link was found.");

                return result;
            }

            result.Log.Add($"Downloading from: {url}.");

            using (var downloadResponse = await HttpClient.GetAsync(url))
            {
                if (!downloadResponse.IsSuccessStatusCode)
                {
                    result.Log.Add($"Could not download file: {downloadResponse.StatusCode}.");

                    return result;
                }

                using (var stream = await downloadResponse.Content.ReadAsStreamAsync())
                using (var file = File.OpenWrite(Path.Combine(Constants.WorkingDirectory, name)))
                {
                    stream.CopyTo(file);
                }
            }

            result.Log.Add("Download completed.");

            result.Success = true;

            return result;
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

            if (File.Exists(Constants.RestartFile))
            {
                File.Delete(Constants.RestartFile);
            }

            File.WriteAllLines(Constants.RestartFile, Constants.CurrentCommands);

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
    }
}
