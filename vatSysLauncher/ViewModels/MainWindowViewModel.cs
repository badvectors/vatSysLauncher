using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using vatSysLauncher.Controllers;
using vatSysLauncher.Models;

namespace vatSysLauncher.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        // Main

        private bool _buttonsEnabled;
        private Visibility _waitText;
        private Visibility _launchButton;
        private Visibility _updatesAvailable;
        private string _updatesText;

        public string Version => $"Version {Utility.GetFileVersion()}";
        public bool ButtonsEnabled
        {
            get { return _buttonsEnabled; }
            set
            {
                if (_buttonsEnabled != value)
                {
                    _buttonsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }
        public Visibility WaitText
        {
            get { return _waitText; }
            set
            {
                if (_waitText != value)
                {
                    _waitText = value;
                    OnPropertyChanged();
                }
            }
        }
        public Visibility LaunchButton
        {
            get { return _launchButton; }
            set
            {
                if (_launchButton != value)
                {
                    _launchButton = value;
                    OnPropertyChanged();
                }
            }
        }
        public Visibility UpdatesAvailable
        {
            get { return _updatesAvailable; }
            set
            {
                if (_updatesAvailable != value)
                {
                    _updatesAvailable = value;
                    OnPropertyChanged();
                }
            }
        }
        public string UpdatesText
        {
            get { return _updatesText; }
            set
            {
                if (_updatesText != value)
                {
                    _updatesText = value;
                    OnPropertyChanged();
                }
            }
        }

        // Current view

        private object _currentView;

        public object CurrentView
        {
            get { return _currentView; }
            set
            {
                if (_currentView != value)
                {
                    _currentView = value;
                    OnPropertyChanged();
                }
            }
        }

        // Profiles

        private Visibility _profilesLoading;
        private List<ProfileOption> _profilesList = new();

        public Visibility ProfilesLoading
        {
            get { return _profilesLoading; }
            set
            {
                if (_profilesLoading != value)
                {
                    _profilesLoading = value;
                    OnPropertyChanged();
                }
            }
        }
        public List<ProfileOption> ProfilesList
        {
            get { return _profilesList; }
            set
            {
                if (_profilesList != value)
                {
                    _profilesList = value;
                    OnPropertyChanged();
                }
            }
        }

        // Plugins

        private Visibility _pluginsLoading;
        private List<PluginInstalled> _pluginsList;
        private List<string> _pluginsLocations = new();
        private List<string> _pluginsAvailable = new();

        public Visibility PluginsLoading
        {
            get { return _pluginsLoading; }
            set
            {
                if (_pluginsLoading != value)
                {
                    _pluginsLoading = value;
                    OnPropertyChanged();
                }
            }
        }
        public List<PluginInstalled> PluginsList
        {
            get { return _pluginsList; }
            set
            {
                if (_pluginsList != value)
                {
                    _pluginsList = value;
                    OnPropertyChanged();
                }
            }
        }
        public List<string> PluginsLocations
        {
            get { return _pluginsLocations; }
            set
            {
                if (_pluginsLocations != value)
                {
                    _pluginsLocations = value;
                    OnPropertyChanged();
                }
            }
        }
        public List<string> PluginsAvailable
        {
            get { return _pluginsAvailable; }
            set
            {
                if (_pluginsAvailable != value)
                {
                    _pluginsAvailable = value;
                    OnPropertyChanged();
                }
            }
        }

        // Updater

        private string _updaterLog;

        public string UpdaterLog
        {
            get { return _updaterLog; }
            set
            {
                if (_updaterLog != value)
                {
                    _updaterLog = value;
                    OnPropertyChanged();
                }
            }
        }

        // Setup

        public string BaseDirectory => Launcher.Settings.BaseDirectory;
        public string ProfileDirectory => Launcher.Settings.ProfileDirectory;
        public bool IncludeDevelopment => Launcher.Settings.IncludeDevelopment;

        // On Property Change

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
