using Config;
using Ookii.Dialogs.Wpf;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media;
using Updater;

namespace Launcher.ViewModels
{
    public class SettingsViewModel : BindableBase, INavigationAware
    {

        private readonly SimpleHttpLoader loader;
        private readonly Timer timer;
        private readonly Configurator configReader;

        private UpdaterConfig? config;

        public SettingsViewModel(SimpleHttpLoader loader, Configurator configReader)
        {
            this.loader = loader;
            this.configReader = configReader;

            SelectDirectoryCommand = new DelegateCommand(SelectDirectory);

            timer = new(3000);
            timer.Elapsed += PeriodicCheck;
        }

        public DelegateCommand SelectDirectoryCommand { get; private set; }

        private Brush remoteDestinationCheck = Brushes.Gray;
        public Brush RemoteDestinationCheck
        {
            get => remoteDestinationCheck;
            set => SetProperty(ref remoteDestinationCheck, value);
        }

        private Brush localDestinationCheck = Brushes.Gray;
        public Brush LocalDestinationCheck
        {
            get => localDestinationCheck;
            set => SetProperty(ref localDestinationCheck, value);
        }

        private bool placeInGameDirectory;
        public bool PlaceInGameDirectory
        {
            get => placeInGameDirectory;
            set => SetProperty(ref placeInGameDirectory, value);
        }

        private string? localGameDirectory;
        public string? LocalGameDirectory
        {
            get => localGameDirectory;
            set => SetProperty(ref localGameDirectory, value);
        }

        private string remoteDirectoryToolTip;
        public string RemoteDirectoryToolTip
        {
            get => remoteDirectoryToolTip;
            set => SetProperty(ref remoteDirectoryToolTip, value);
        }

        private string remoteSourceAddress = @"https://l2-update.gudilap.ru/";
        public string RemoteSourceAddress
        {
            get => remoteSourceAddress;
            set => SetProperty(ref remoteSourceAddress, value);
        }

        private async void SelectDirectory()
        {
            VistaFolderBrowserDialog folderdialog = new()
            {
                SelectedPath = config.LocalDirectory == null ? "" : Path.GetFullPath(config.LocalDirectory.LocalPath) + "\\",
                ShowNewFolderButton = true
            };

            if (folderdialog.ShowDialog() == true)
            {
                config.LocalDirectory = new Uri(folderdialog.SelectedPath);
                LocalGameDirectory = folderdialog.SelectedPath;
                await configReader.WriteAsync(config);
            }
        }

        private async void PeriodicCheck(object? sender, ElapsedEventArgs e)
        {
            if (Directory.Exists(LocalGameDirectory))
            {
                LocalDestinationCheck = Brushes.Green;
            } else
            {
                LocalDestinationCheck = Brushes.Red;
            }

            if (Uri.IsWellFormedUriString(RemoteSourceAddress, UriKind.Absolute))
            {
                RemoteDestinationCheck = Brushes.Orange;

                loader.RemoteAddr = new Uri(RemoteSourceAddress);

                if (await loader.CheckConnectAsync())
                {
                    RemoteDestinationCheck = Brushes.Green;
                }
            }
            else
            {
                RemoteDestinationCheck = Brushes.Red;
                RemoteDirectoryToolTip = "Неверный формат адреса!";
            }
        }

        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            timer.Start();

            config = configReader.Read();

            if (config != null)
            {
                LocalGameDirectory = config.LocalDirectory.LocalPath;
                RemoteSourceAddress = config.RemoteStorage.AbsoluteUri;
            } else
            {
                config = new UpdaterConfig();
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            timer.Stop();
        }
    }
}
