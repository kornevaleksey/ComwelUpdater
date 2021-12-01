using Config;
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
using System.Windows.Media;
using Updater;

namespace Launcher.ViewModels
{
    public class UpdateViewModel : BindableBase, INavigationAware
    {
        private readonly GameUpdater updater;
        private UpdaterConfig? config;

        public UpdateViewModel(GameUpdater updater)
        {
            this.updater = updater;

            PlayGameCommand = new DelegateCommand(PlayGame);
            UpdateGameCommand = new DelegateCommand(UpdateGame);

            infoBlock = "";
            infoBlockAdd = "";
            infoBlockColor = Brushes.Black;

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
            set => SetProperty(ref playEnabled, value);
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
                ProcessStartInfo l2info = new();
                l2info.EnvironmentVariables["__COMPAT_LAYER"] = "RunAsInvoker";
                l2info.FileName = config.ClientExeFile;

                Process l2Run = new()
                {
                    EnableRaisingEvents = true,
                    StartInfo = l2info
                };
                l2Run.Exited += L2RunExited;

                l2Run.Start();
            }
            catch (Exception ex)
            {
                InfoBlock = "Запуск невозможен!";
                InfoBlockColor = Brushes.Red;
                InfoBlockAdd = ex.Message;
            }
        }

        private void L2RunExited(object? sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private async void UpdateGame()
        {
            RunningGameUpdate = false;

            InfoBlock = "Проверка клиента игры Lineage II";
            //await updater.FastLocalClientCheck();
        }

        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            PlayEnabled = File.Exists(config.ClientExeFile);

            CancellationTokenSource cts = new CancellationTokenSource();

            await updater.FastCheckAsync(cts.Token);

            InfoBlock = updater.Difference?.Count > 0 ? "Необходимо обновление" : "Файлы игры проверены";
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
