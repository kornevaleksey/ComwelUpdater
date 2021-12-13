using Config;
using Launcher.Models;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Updater;
using Updater.Models;

namespace Launcher.ViewModels
{
    public class UpdateViewModel : BindableBase, INavigationAware
    {
        private readonly GameUpdater updater;
        private readonly UpdaterConfig config;
        private readonly GameLauncher launcher;

        private CancellationTokenSource cts;

        public UpdateViewModel(GameUpdater updater, GameLauncher launcher, UpdaterConfig config)
        {
            this.updater = updater;
            this.config = config;
            this.launcher = launcher;

            PlayGameCommand = new DelegateCommand(PlayGame);
            UpdateGameCommand = new DelegateCommand(UpdateGame);

            cts = new CancellationTokenSource();

            infoBlock = "";
            infoBlockAdd = "";
            infoBlockColor = Brushes.Black;

            PlayEnabled = false;

            this.updater.ClientCheckProgress += OnLocalCheckProgress;
        }

        private void OnCanLaunchChanged(object? sender, bool e)
        {
            PlayEnabled = e;
        }

        private void OnLocalCheckProgress(object? sender, UpdaterProgressEventArgs e)
        {
            if (e.Overall)
            {
                InfoBlock = e.InfoStr;
                OverallProgress = e.Progress;
                UnzipProgressVisible = Visibility.Hidden;
            } else
            {
                UnzipProgressVisible = Visibility.Visible;
                InfoBlock = e.InfoStr;
                InfoBlockAdd = "";
                FileProgress = e.Progress;
            }
        }

        private Visibility visibility;
        public Visibility UnzipProgressVisible
        {
            get => visibility;
            set => SetProperty(ref visibility, value);
        }

        private string updateGameButtonText = "Обновить";
        public string UpdateGameButtonText
        {
            get => updateGameButtonText;
            set => SetProperty(ref updateGameButtonText, value);
        }

        private string infoBlock;
        public string InfoBlock
        {
            get => infoBlock;
            set => SetProperty(ref infoBlock, value);
        }

        private Brush infoBlockColor;
        public Brush InfoBlockColor
        {
            get => infoBlockColor;
            set => SetProperty(ref infoBlockColor, value);
        }

        private string infoBlockAdd;
        public string InfoBlockAdd
        {
            get => infoBlockAdd;
            set => SetProperty(ref infoBlockAdd, value);
        }

        private bool runningGameUpdate;
        public bool RunningGameUpdate
        {
            get => runningGameUpdate;
            set => SetProperty(ref runningGameUpdate, value);
        }

        private bool playEnabled;
        public bool PlayEnabled
        {
            get => playEnabled;
            set
            {
                bool playGame = !updater.IsBusy && value;
                SetProperty(ref playEnabled, playGame);
            }

        }

        private bool updateEnabled;
        public bool UpdateEnabled
        {
            get => updateEnabled;
            set => SetProperty(ref updateEnabled, value);
        }

        private double overallProgress;
        public double OverallProgress
        {
            get => overallProgress;
            set => SetProperty(ref overallProgress, value);
        }

        private double fileProgress;
        public double FileProgress
        {
            get => fileProgress;
            set => SetProperty(ref fileProgress, value);
        }

        private double minProgress = 0;
        public double MinProgress
        {
            get => minProgress;
            set => SetProperty(ref minProgress, value);
        }

        private double maxProgress = 100;
        public double MaxProgress
        {
            get => maxProgress;
            set => SetProperty(ref maxProgress, value);
        }

        public DelegateCommand PlayGameCommand { get; private set; }
        public DelegateCommand UpdateGameCommand { get; private set; }

        private void PlayGame()
        {
            if (config == null)
            {
                return;
            }

            try
            {
                RunningGameUpdate = false;
                launcher.Launch();

                InfoBlockColor = Brushes.Black;
                InfoBlockAdd = String.Empty;
                InfoBlock = "Игра запущена";
            }
            catch (Exception ex)
            {
                InfoBlock = "Запуск невозможен!";
                InfoBlockColor = Brushes.Red;
                InfoBlockAdd = ex.Message;
            }
        }

        private async void UpdateGame()
        {
            RunningGameUpdate = false;

            InfoBlock = "Проверка клиента игры Lineage II";
            await updater.UpdateClient(cts.Token);
        }

        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            cts = new CancellationTokenSource();

            UpdateEnabled = false;
            bool updateIsNeeded = await updater.FastCheckAsync(cts.Token);

            if (updater.Difference != null && updateIsNeeded)
            {
                string updateSize = Updater.Models.SizeConverter.Convert(updater.Difference.Sum(f => f.FileSizeCompressed));
                InfoBlock = "Необходимо обновление";
                InfoBlockAdd = $"Размер обновления : {updateSize}";
                InfoBlockColor = Brushes.Black;
                UpdateEnabled = true;
            } else
            {
                InfoBlock = "Обновление не требуется";
                InfoBlockAdd = "";
                InfoBlockColor = Brushes.Green;
                UpdateEnabled = false;
            }

            PlayEnabled = launcher.CanLaunch;
            launcher.CanLaunchChanged += OnCanLaunchChanged;
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            
        }


    }
}
