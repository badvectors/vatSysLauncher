using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace vatSysLauncher.Controllers
{
    internal class VatSys
    {
        private static readonly DispatcherTimer VatSysTimer = new();

        public static void Init()
        {
            Check();

            VatSysTimer.Tick += Timer_Tick;
            VatSysTimer.Interval = new TimeSpan(0, 0, 1);

            VatSysTimer.Start();
        }

        private static void Timer_Tick(object sender, EventArgs e)
        {
            Check();
        }

        private static void Check()
        {
            // Get any running vatSys processes.
            var vatsysProcesses = Process.GetProcessesByName(Launcher.VatsysProcessName);

            if (vatsysProcesses.Length > 0)
            {
                Launcher.SetCanvas("Init");

                Launcher.SetLoading(false);

                return;
            }

            if (vatsysProcesses.Length == 0 && Launcher.CurrentCanvas != "Init") return;

            Launcher.SetCanvas("Home");

            Launcher.SetLoading(true);
        }

        public static async Task Launch()
        {
            if (Launcher.Settings == null || string.IsNullOrWhiteSpace(Launcher.Settings.BaseDirectory)) return;

            if (!File.Exists(Launcher.VatsysExe))
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
                        Launcher.SetCanvas("Setup");
                        break;
                }
                return;
            }

            var success = await Updater.UpdateAll();

            if (!success) return;

            Process.Start(Launcher.VatsysExe);

            Environment.Exit(1);
        }

        public static void Close()
        {
            // Get any running vatSys processes.
            var vatsysProcesses = Process.GetProcessesByName(Launcher.VatsysProcessName);

            // Kill all running vatSys processes.
            if (vatsysProcesses.Length > 0)
            {
                foreach (var vatsysProcess in vatsysProcesses)
                    vatsysProcess.Kill();
            }
        }
    }
}
