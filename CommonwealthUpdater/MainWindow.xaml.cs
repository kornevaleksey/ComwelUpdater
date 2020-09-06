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
using Config;
using Updater;
using Launcher;
using System.Diagnostics;
using System.Net.Http;
using Ookii.Dialogs.Wpf;
using System.Net.NetworkInformation;

namespace CommonwealthUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static NLog.Logger logger;// = NLog.LogManager.GetCurrentClassLogger();

        static L2UpdaterConfig UpdaterConfig;

        CancellationTokenSource updaterCancellationTokenSource;
        CancellationTokenSource fullcheckCancellationTokenSource;

        public MainWindow()
        {
            NLog.LogManager.LoadConfiguration(AppDomain.CurrentDomain.BaseDirectory+"\\nlog.config");
            logger = NLog.LogManager.GetCurrentClassLogger();
            
            UpdaterConfig = new L2UpdaterConfig();

            InitializeComponent();

            UpdaterConfig.ConfigFinishedRead += ConfigFinishedRead;
            UpdaterConfig.ConfigReadError += ConfigReadError;
            UpdaterConfig.Read();
        }

        private void updaterwindow_Initialized(object sender, EventArgs e)
        {
            logger.Info("Start init updater");
            
        }

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void ConfigFinishedRead (object sender, EventArgs args)
        {
            ChkGameFolder.IsChecked = UpdaterConfig.ConfigFields.PlacedInClientFolder;
            ClientDestination.Text = UpdaterConfig.ConfigFields.ClientFolder.LocalPath;
            RemoteAddr.Text = UpdaterConfig.ConfigFields.DownloadAddress.ToString();

            UpdaterSelect.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        private void ConfigReadError(object sender, ConfigRWErrorEventArgs args)
        {
            switch (args.ReadException)
            {
                case ArgumentNullException nullex:
                    LauncherDialogs.MessageBox(String.Format("Нет конфигурации программы! Введите настройки."));
                    break;
                default:
                    //LauncherDialogs.MessageBox(String.Format("Ошибка чтения файла конфигурации! {0}", args.ReadException.Message));
                    break;
            }

            UpdaterSelect.IsEnabled = false;
            RecheckBtn.IsEnabled = false;

            SettingsBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
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

            UpdateL2.IsEnabled = false;

            Loader loader = new Loader();
            L2Updater updater = new L2Updater(loader, UpdaterConfig);
            UpdateL2.Tag = updater;

            updater.ClientCheckUpdate += ClientCheckUpdate;
            updater.ClientCheckFinished += ClientCheckFinish;
            updater.ClientCheckError += ClientCheckError;
            loader.LoaderProgress += ClientLoadUpdate;
            loader.UnZipProgress += LoadFileExtractProgress;
            loader.RemoteAddr = UpdaterConfig.ConfigFields.DownloadAddress;

            InfoBlock.Text = "Проверка клиента игры Lineage II";

            try
            {
                updater.FastLocalClientCheck();
            }
            finally
            {
            }

            PlayL2.IsEnabled = updater.ClientCanRun;
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            ChkGameFolder.IsChecked = UpdaterConfig.ConfigFields.PlacedInClientFolder;
            RemoteAddr.Text = UpdaterConfig.ConfigFields.DownloadAddress==null?"": UpdaterConfig.ConfigFields.DownloadAddress.ToString();
            ClientDestination.Text = UpdaterConfig.ConfigFields.ClientFolder==null?"": UpdaterConfig.ConfigFields.ClientFolder.LocalPath;

            MainSelector.SelectedIndex = 1;
        }

        private void RecheckBtn_Click(object sender, RoutedEventArgs e)
        {
            MainSelector.SelectedIndex = 2;
        }

        private void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            MainSelector.SelectedIndex = 3;
        }

        private void GudilapBtn_Click(object sender, RoutedEventArgs e)
        {
            Process runbrowser = new Process();
            runbrowser.StartInfo.UseShellExecute = true;
            runbrowser.StartInfo.FileName = "http://www.gudilap.ru";

            runbrowser.Start();

            e.Handled = true;
        }

        #endregion

        #region Settings tab events 

        private void ConfigFinishedWrite (object sender, EventArgs args)
        {
            UpdaterSelect.IsEnabled = true;
            RecheckBtn.IsEnabled = true;

            if (UpdaterConfig.ConfigFields.PlacedInClientFolder==false)
            {
                var cfg = new L2UpdaterConfig();
                cfg.ConfigFields.PlacedInClientFolder = true;
                if (File.Exists(cfg.ConfigFile))
                    File.Delete(cfg.ConfigFile);
            }

            TxtBSaveSettings.Text = "Настройки сохранены";
            TxtBSaveSettings.Foreground = Brushes.Green;
        }

        private void ConfigWriteError(object sender, ConfigRWErrorEventArgs args)
        {
            TxtBSaveSettings.Text = "Не получилось сохранить настройки!";
            TxtBSaveSettings.Foreground = Brushes.Red;
        }

        private void SettingsSave()
        {
            if ((Uri.IsWellFormedUriString(RemoteAddr.Text, UriKind.Absolute)) && Directory.Exists(ClientDestination.Text))
            {
                TxtBSaveSettings.Text = "Сохраняю настройки";

                UpdaterConfig.ConfigFinishedWrite = ConfigFinishedWrite;
                UpdaterConfig.ConfigWriteError = ConfigWriteError;

                UpdaterConfig.ConfigFields.DownloadAddress = new Uri(RemoteAddr.Text);
                UpdaterConfig.ConfigFields.ClientFolder = new Uri(ClientDestination.Text);
                UpdaterConfig.ConfigFields.PlacedInClientFolder = ChkGameFolder.IsChecked ?? false;
                UpdaterConfig.Write();
            } else
            {
                TxtBSaveSettings.Text = "Не могу сохранить настройки - неверные данные!";
                TxtBSaveSettings.Foreground = Brushes.Red;
            }

        }

        private void ChkGameFolder_Checked(object sender, RoutedEventArgs e)
        {
            BtnSelect.IsEnabled = false;
            UpdaterConfig.ConfigFields.PlacedInClientFolder = true;
            ClientDestination.Text = UpdaterConfig.ConfigFields.ClientFolder.LocalPath;
            SettingsSave();
        }

        private void ChkGameFolder_Unchecked(object sender, RoutedEventArgs e)
        {
            BtnSelect.IsEnabled = true;
            UpdaterConfig.ConfigFields.PlacedInClientFolder = false;
            ClientDestination.Text = UpdaterConfig.ConfigFields.ClientFolder.LocalPath;
            SettingsSave();
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog folderdialog = new VistaFolderBrowserDialog()
            {
                SelectedPath = UpdaterConfig.ConfigFields.ClientFolder==null?"": Path.GetFullPath ( UpdaterConfig.ConfigFields.ClientFolder.LocalPath)+"\\",
                ShowNewFolderButton = true
            };

            if (folderdialog.ShowDialog(this) == true)
            {
                UpdaterConfig.ConfigFields.ClientFolder = new Uri(folderdialog.SelectedPath);
                ClientDestination.Text = folderdialog.SelectedPath;
            }
        }

        private void RemoteAddrChecked (object sender, LoaderConnectionCheckEventArgs args)
        {
            if (args.CheckException==null)
            {
                RemoteOK.Fill = Brushes.Green;
            } else
            {
                RemoteAddr.ToolTip = new TextBlock() { Text = "Не могу соединиться с сервером" };
            }
        }
        
        private void RemoteAddrChanged(object sender, TextChangedEventArgs e)
        {
            SettingsSave();
            TextBox remoteaddr = (TextBox)sender;
            if (Uri.IsWellFormedUriString(remoteaddr.Text, UriKind.Absolute))
            {
                remoteaddr.ToolTip = null;
                RemoteOK.Fill = Brushes.Orange;
                Loader lr = new Loader()
                {
                    RemoteAddr = new Uri(remoteaddr.Text)
                };
                lr.ConnectionCheck += RemoteAddrChecked;

                lr.CheckConnect();
            } else
            {
                RemoteOK.Fill = Brushes.Red;
                remoteaddr.ToolTip = new TextBlock() { Text = "Неверный формат адреса!" };
            }
        }

        private void ClientDestination_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox dest = (TextBox)sender;
            if (Directory.Exists(dest.Text))
            {
                DestinationOK.Fill = Brushes.Green;
                dest.ToolTip = null;
                SettingsSave();
            } else
            {
                DestinationOK.Fill = Brushes.Red;
                dest.ToolTip = new TextBlock() { Text = "Указанного расположения не существует!" };
            }
        }

        #endregion

        #region Updater tab events

        private void ClientCheckFinish (object sender, ClientCheckFinishEventArgs args)
        {
            L2Updater updater = (L2Updater)sender;

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
            });
        }

        private void ClientCheckError (object sender, ClientCheckErrorEventArgs args)
        {
            switch (args.Exeption)
            {
                case HttpRequestException reqex:
                    Dispatcher.BeginInvoke((Action)delegate ()
                    {
                        updatepercentage.Maximum = 0;
                        updatepercentage.Value = 0;
                        InfoBlock.Text = "Не могу соединиться с сервером!";
                        InfoBlock.Foreground = Brushes.Red;
                    });
                    break;
                default:
                    LauncherDialogs.MessageBox(args.Exeption.Message);
                    break;
            }
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
            });
        }

        private void LoadFileExtractProgress(object sender, LoaderUnZipProgressEventArgs args)
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                InfoBlock.Text = String.Format("Скачиваю файл {0} - {1:F0}% готово", args.FileName, args.Percentage);
                InfoBlock.Foreground = Brushes.Black;
            });
        }

        private void ClientUpdateFinished (object sender, EventArgs args)
        {
            L2Updater updater = (L2Updater)sender;
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
            L2Updater updater = (L2Updater)((Button)sender).Tag;

            if (BtnUpdateL2Text.Text.Equals("Обновить"))
            {
                BtnUpdateL2Text.Text = "Остановить";

                updaterCancellationTokenSource = new CancellationTokenSource();

                Loader loader = new Loader();

                loader.LoaderProgress += ClientLoadUpdate;
                updater.ClientUpdateFinished += ClientUpdateFinished;

                try
                {
                    PlayL2.IsEnabled = false;
                    updater.UpdateClient(updaterCancellationTokenSource.Token);
                }
                finally
                {
                }

            } else
            {
                updaterCancellationTokenSource.Cancel();
                BtnUpdateL2Text.Text = "Обновить";
                InfoBlock.Text = "Обновление остановлено";
                InfoBlock.Foreground = Brushes.Black;
            }
        }

        #endregion

        #region Full check tab

        private void BtnFullCheck_Click(object sender, RoutedEventArgs e)
        {
            L2Updater updater;
            switch (BtnFullCheckText.Text)
            {
                case "Полная проверка":
                    BtnFullCheckText.Text = "Остановить проверку";
                    BtnFullReload.IsEnabled = false;

                    Loader loader = new Loader
                    {
                        RemoteAddr = UpdaterConfig.ConfigFields.DownloadAddress
                    };

                    updater = new L2Updater(loader, UpdaterConfig);
                    updater.ClientCheckUpdate += FullCheckActionProgress;
                    updater.ClientCheckFinished += FullClientCheckFinish;
                    loader.LoaderProgress += FullClientLoadUpdate;
                    loader.UnZipProgress += FullLoadFileExtractProgress;

                    BtnFullCheck.Tag = updater;

                    fullcheckCancellationTokenSource = new CancellationTokenSource();

                    try
                    {
                        updater.FullLocalClientCheck(fullcheckCancellationTokenSource.Token);
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
                    break;

                case "Обновить":
                    BtnFullCheckText.Text = "Остановить обновление";
                    BtnFullReload.IsEnabled = false;

                    fullcheckCancellationTokenSource = new CancellationTokenSource();

                    updater = (L2Updater)BtnFullCheck.Tag;

                    updater.ClientUpdateFinished += FullClientUpdateFinished;

                    try
                    {
                        updater.UpdateClient(fullcheckCancellationTokenSource.Token);
                    }
                    finally
                    {
                    }
                    break;
                case "Остановить проверку":
                    fullcheckCancellationTokenSource.Cancel();
                    BtnFullReload.IsEnabled = true;
                    BtnFullCheckText.Text = "Полная проверка";
                    FullCheckInfo1.Text = "Проверка остановлена";
                    FullCheckInfo1.Foreground = Brushes.Black;
                    break;
                case "Остановить обновление":
                    fullcheckCancellationTokenSource.Cancel();
                    BtnFullReload.IsEnabled = true;
                    BtnFullCheckText.Text = "Обновить";
                    FullCheckInfo1.Text = "Обновление остановлено";
                    FullCheckInfo1.Foreground = Brushes.Black;
                    break;
                default:
                    break;
            }
        }

        private void BtnFullReload_Click(object sender, RoutedEventArgs e)
        {
            L2Updater updater;
            switch (BtnFullReloadText.Text)
            {
                case "Полная перезапись":
                    BtnFullReloadText.Text = "Остановить перезапись";
                    BtnFullCheck.IsEnabled = false;

                    Loader loader = new Loader
                    {
                        RemoteAddr = UpdaterConfig.ConfigFields.DownloadAddress
                    };

                    updater = new L2Updater(loader, UpdaterConfig);
                    updater.ClientCheckUpdate += FullCheckActionProgress;
                    updater.ClientCheckFinished += FullClientCheckFinish;
                    loader.LoaderProgress += FullClientLoadUpdate;
                    loader.UnZipProgress += FullLoadFileExtractProgress;

                    BtnFullReload.Tag = updater;

                    fullcheckCancellationTokenSource = new CancellationTokenSource();

                    try
                    {
                        updater.RewriteClient(fullcheckCancellationTokenSource.Token);
                    }
                    finally
                    {

                    }

                    break;
                case "Остановить перезапись":
                    fullcheckCancellationTokenSource.Cancel();
                    BtnFullCheck.IsEnabled = true;
                    BtnFullReloadText.Text = "Полная перезапись";
                    FullCheckInfo1.Text = "Перезапись остановлена";
                    FullCheckInfo1.Foreground = Brushes.Black;
                    break;
            }
        }

        private void FullClientUpdateFinished(object sender, EventArgs args)
        {
            L2Updater updater = (L2Updater)sender;
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                FullCheckPercentage.Maximum = 100;
                FullCheckPercentage.Value = 100;
                FullCheckInfo1.Text = String.Format("Игра обновлена");
                FullCheckInfo1.Foreground = Brushes.Green;

                BtnFullCheckText.Text = "Полная проверка";
                BtnFullReload.IsEnabled = true;
            });
        }

        private void FullClientCheckFinish(object sender, ClientCheckFinishEventArgs args)
        {
            L2Updater updater = (L2Updater)sender;

            Dispatcher.BeginInvoke((Action)delegate ()
            {
                BtnFullReload.IsEnabled = true;

                FullCheckPercentage.Maximum = 100;
                FullCheckPercentage.Value = 1000;
                FullCheckInfo1.Text = args.FinishMessage;
                FullCheckInfo1.Foreground = args.UpdateRequired ? Brushes.Black : Brushes.Green;

                if (updater.Difference?.Count > 0)
                {
                    BtnFullCheckText.Text = "Обновить";
                    BtnFullCheck.ToolTip = new TextBlock()
                    {
                        Text = String.Format("Обновление {0} файлов. Всего будет скачано: {1} МБ, необходимое место на диске {2}",
                        updater.Difference.Count,
                        updater.Difference.Sum(s => s.FileSizeCompressed) / 1024 / 1024,
                        updater.Difference.Sum(s => s.FileSize) / 1024 / 1024)
                    };
                } else
                {
                    BtnFullCheckText.Text = "Полная проверка";
                    BtnFullCheck.ToolTip = new TextBlock();
                }
            });
        }

        private void FullCheckActionProgress(object sender, ClientCheckUpdateEventArgs args)
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                FullCheckPercentage.Maximum = args.ProgressMax;
                FullCheckPercentage.Value = args.ProgressValue;
                FullCheckInfo1.Text = args.InfoStr;
            });
        }

        private void FullClientLoadUpdate(object sender, LoaderProgressEventArgs args)
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                FullCheckPercentage.Maximum = 100;
                FullCheckPercentage.Value = args.Percentage * 100.0;
            });
        }

        private void FullLoadFileExtractProgress(object sender, LoaderUnZipProgressEventArgs args)
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                FullCheckInfo1.Text = String.Format("Скачиваю файл {0} - {1:F0}% готово", args.FileName, args.Percentage);
                FullCheckInfo1.Foreground = Brushes.Black;
            });
        }

        #endregion

        #region AboutTab
        private void HyperLicense_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

        }

    }
}
