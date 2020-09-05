using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.WindowsAPICodePack.Dialogs;
using Config;
using Updater;
using System.Diagnostics;
using System.Net.Http;

namespace CommonwealthUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static NLog.Logger logger;// = NLog.LogManager.GetCurrentClassLogger();

        static L2UpdaterConfig UpdaterConfig;
        readonly Loader loader;
        L2Updater updater;

        Task UpdaterTask;

        public MainWindow()
        {
            NLog.LogManager.LoadConfiguration(AppDomain.CurrentDomain.BaseDirectory+"\\nlog.config");
            logger = NLog.LogManager.GetCurrentClassLogger();
            
            UpdaterConfig = new L2UpdaterConfig();

            loader = new Loader();

            updater = new L2Updater(loader, UpdaterConfig);

            InitializeComponent();
        }

        private async void updaterwindow_Initialized(object sender, EventArgs e)
        {
            logger.Info("Start init updater");
            await UpdaterConfig.Read();
            loader.RemoteAddr = UpdaterConfig.ConfigFields.DownloadAddress;
            UpdaterSelect.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        #region Selection DockPanel events

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void UpdaterClick(object sender, RoutedEventArgs e)
        {
            MainSelector.SelectedIndex = 0;

            if (updater.IsBusy == false)
            {

                updater.ClientCheckUpdate += ClientCheckUpdate;
                updater.ClientCheckFinished += ClientCheckFinish;
                loader.LoaderProgress += ClientLoadUpdate;

                InfoBlock.Text = "Проверка клиента игры Lineage II";

                try
                {
                    updater.FastLocalClientCheck();
                }
                finally
                {
                }
            }

            if (File.Exists(UpdaterConfig.ClientExeFile))
            {
                PlayL2.IsEnabled = true;
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            RemoteAddr.Text = UpdaterConfig.ConfigFields.DownloadAddress?.Host+":"+ UpdaterConfig.ConfigFields.DownloadAddress?.Port.ToString();
            ClientDestination.Text = UpdaterConfig.ConfigFields.ClientFolder?.ToString();

            MainSelector.SelectedIndex = 1;
        }

        private void RecheckBtn_Click(object sender, RoutedEventArgs e)
        {
            MainSelector.SelectedIndex = 2;
        }

        #endregion

        #region Settings tab events 
        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog commonOpenFileDialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                InitialDirectory = UpdaterConfig.ConfigFields.ClientFolder?.ToString()
            };

            if (commonOpenFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                UpdaterConfig.ConfigFields.ClientFolder = new Uri(commonOpenFileDialog.FileName);
                ClientDestination.Text = commonOpenFileDialog.FileName;
            }
        }

        private void RemoteAddrChanged(object sender, TextChangedEventArgs e)
        {
        }

        private async void BtnSettingsWrite_Click(object sender, RoutedEventArgs e)
        {
            if ((RemoteAddr.Text != "") && (ClientDestination.Text != ""))
            {
                UriBuilder remote = new UriBuilder()
                {
                    Host = RemoteAddr.Text.Split(":")[0],
                    Port = Convert.ToInt32(RemoteAddr.Text.Split(":")[1])
                };
                UpdaterConfig.ConfigFields.DownloadAddress = remote.Uri;
                UpdaterConfig.ConfigFields.ClientFolder = new Uri(ClientDestination.Text);
                await UpdaterConfig.Write();
            } else
            {
                MessageBox.Show("Не указано одно из значений!");
            }
        }
        #endregion

        #region Updater tab events

        private void ClientCheckFinish (object sender, ClientCheckFinishEventArgs args)
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                updatepercentage.Maximum = 0;
                updatepercentage.Value = 0;
                InfoBlock.Text = args.FinishMessage;
                InfoBlock.Foreground = args.UpdateRequired? Brushes.Black : Brushes.Green;

                if (updater.Difference?.Count > 0)
                {
                    UpdateL2.IsEnabled = true;
                    UpdateL2.ToolTip = new TextBlock()
                    {
                        Text = String.Format("Обновление {0} файлов. Всего будет скачано: {1} МБ, необходимое место на диске {2}",
                        updater.Difference.Count,
                        updater.Difference.Sum(s => s.FileSizeCompressed) / 1024 / 1024,
                        updater.Difference.Sum(s => s.FileSize) / 1024 / 1024)
                    };
                }

                updater.ClientCheckUpdate -= ClientCheckUpdate;
                updater.ClientCheckFinished -= ClientCheckFinish;
                loader.LoaderProgress -= ClientLoadUpdate;
            });
        }

        private void ClientCheckUpdate(object sender, ClientCheckUpdateEventArgs args)
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                updatepercentage.Maximum = args.ProgressMax;
                updatepercentage.Value = args.ProgressValue;
                InfoBlock.Text = args.InfoStr;
                InfoBlock.Foreground = Brushes.Black;
            });
        }

        private void ClientLoadUpdate(object sender, LoaderProgressEventArgs args)
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                updatepercentage.Maximum = 100;
                updatepercentage.Value = args.Percentage*100.0;
                InfoBlock.Text = String.Format("Скачиваю файл {0}", args.FileName);
                InfoBlock.Foreground = Brushes.Black;
            });
        }

        private void ClientUpdateFinished (object sender, EventArgs args)
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                UpdateL2.IsEnabled = false;
                PlayL2.IsEnabled = updater.ClientCanRun;

                updatepercentage.Maximum = 100;
                updatepercentage.Value = 100;
                InfoBlock.Text = String.Format("Игра обновлена");
                InfoBlock.Foreground = Brushes.Green;
            });
        }

        private void PlayL2_Click(object sender, RoutedEventArgs e)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = UpdaterConfig.ClientExeFile;
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.Verb = "runas";
            proc.Start();
        }

        private void UpdateL2_Click(object sender, RoutedEventArgs e)
        {
            if (BtnUpdateL2Text.Text.Equals("Обновить"))
            {
                UpdateL2.Tag = false;
                BtnUpdateL2Text.Text = "Остановить";

                loader.LoaderProgress += ClientLoadUpdate;
                updater.ClientUpdateFinished += ClientUpdateFinished;

                try
                {
                    PlayL2.IsEnabled = false;
                    UpdaterTask = updater.UpdateClient();
                }
                finally
                {
                }

            } else
            {
                BtnUpdateL2Text.Text = "Обновить";
                Task.
                UpdaterTask.stop
            }
        }

        #endregion

        #region Full check tab
        private void FullCheckActionProgress(object sender, ClientCheckUpdateEventArgs args)
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                FullCheckPercentage.Maximum = args.ProgressMax;
                FullCheckPercentage.Value = args.ProgressValue;
                FullCheckLog.AppendText(args.InfoStr + Environment.NewLine);
            });
        }

        private void FullCheckLoaderProgress(object sender, LoaderProgressEventArgs args)
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                FullCheckPercentage.Maximum = 100;
                FullCheckPercentage.Value = args.Percentage*100.0;
                FullCheckLog.AppendText(String.Format("Скачиваю файл {0}{1}",args.FileName, Environment.NewLine));
            });
        }

        private async void BtnFullCheck_Click(object sender, RoutedEventArgs e)
        {
            BtnFullCheck.IsEnabled = false;

            updater = new L2Updater(loader, UpdaterConfig);
            updater.ClientCheckUpdate += FullCheckActionProgress;
            loader.LoaderProgress += FullCheckLoaderProgress;

            try
            {
                await updater.FullLocalClientCheck();
            }
            catch (RemoteModelException exr)
            {
                MessageBox.Show(String.Format("Не могу соединиться с {0} для получения файла {1}", exr.RemoteAddr, exr.RemoteFile));
                SettingsBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                return;
            }
            catch (HttpRequestException exhttp)
            {
                MessageBox.Show(String.Format("Не могу соединиться с {0}", exhttp.TargetSite));
                SettingsBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                SettingsBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                return;
            }

            if (updater.Difference.Count > 0)
            {
                UpdateL2.IsEnabled = true;
                UpdateL2.ToolTip = new TextBlock()
                {
                    Text = String.Format("Обновление {0} файлов. Всего будет скачано: {1}", updater.Difference.Count, updater.Difference.Sum(s => s.FileSize))
                };
            }

            if (File.Exists(UpdaterConfig.ConfigFields.ClientFolder + UpdaterConfig.ClientExeFile))
            {
                PlayL2.IsEnabled = true;
            }

            BtnFullCheck.IsEnabled = true;
        }

        #endregion
    }
}
