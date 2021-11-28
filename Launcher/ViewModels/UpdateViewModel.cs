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
using System.Threading.Tasks;
using System.Windows.Media;
using Updater;

namespace Launcher.ViewModels
{
    public class UpdateViewModel : BindableBase, INavigationAware
    {
        private readonly GameUpdater updater;
        private readonly Configurator configReader;
        private UpdaterConfig? config;

        public UpdateViewModel(GameUpdater updater, Configurator configReader)
        {
            this.updater = updater;
            this.configReader = configReader;

            UpdateGameButtonText = "Обновить";

            PlayGameCommand = new DelegateCommand(PlayGame);
            UpdateGameCommand = new DelegateCommand(UpdateGame);
        }

        private string updateGameButtonText;
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

        private bool updateEnabled;
        public bool UpdateEnabled
        {
            get => updateEnabled;
            set => SetProperty(ref updateEnabled, value);
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
            try
            {
                UpdateEnabled = false;
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

        private void L2RunExited(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private async void UpdateGame()
        {
            UpdateEnabled = false;

            InfoBlock = "Проверка клиента игры Lineage II";
            await updater.FastLocalClientCheck();
        }

        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            config = await configReader.ReadAsync();

            PlayEnabled = File.Exists(config.ClientExeFile);

            await updater.FastCheckAsync();
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
