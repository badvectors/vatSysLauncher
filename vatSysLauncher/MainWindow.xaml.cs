using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using vatSysLauncher.Controllers;

namespace vatSysManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaCaptionColor = 35;
        private const int ColorWhite = 0x00FFFFFF;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = Launcher.MainViewModel;
            _ = Init();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var hwnd = new WindowInteropHelper(this).Handle;
            var useDarkMode = 0;
            DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref useDarkMode, sizeof(int));

            var captionColor = ColorWhite;
            DwmSetWindowAttribute(hwnd, DwmwaCaptionColor, ref captionColor, sizeof(int));
        }

        private async Task Init()
        {
            Settings.Init();

            Launcher.SetCanvas("Home");

            Launcher.SetLoading(true);

            await Utility.CheckVersion();

            await Profiles.Init();

            await Plugins.Init();

            await Launcher.CheckForRestart();

            Launcher.SetLoading(false);

            VatSys.Init();

            Launcher.GetChanges();

            Utility.DeleteDirectory(Launcher.WorkingDirectory);
        }

        private void SetupButton_Click(object sender, RoutedEventArgs e)
        {
            Launcher.SetCanvas("Setup");
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            Launcher.SetCanvas("Home");
        }

        private void ProfilesButton_Click(object sender, RoutedEventArgs e)
        {
            Launcher.SetCanvas("Profiles");
        }

        private void PluginsButton_Click(object sender, RoutedEventArgs e)
        {
            Launcher.SetCanvas("Plugins");
        }
    }
}