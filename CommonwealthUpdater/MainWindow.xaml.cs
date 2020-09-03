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
        Loader loader;
        L2Updater updater;

        public MainWindow()
        {
            NLog.LogManager.LoadConfiguration(AppDomain.CurrentDomain.BaseDirectory+"\\nlog.config");
            logger = NLog.LogManager.GetCurrentClassLogger();
            
            UpdaterConfig = new L2UpdaterConfig();

            loader = new Loader();

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

        private async void UpdaterClick(object sender, RoutedEventArgs e)
        {
            MainSelector.SelectedIndex = 0;
            updater = new L2Updater(loader, UpdaterConfig);
            updater.ProgressUpdate += UpdaterActionProgress;
            loader.ProgressUpdate += UpdaterActionProgress;

            try
            {
                await updater.FastLocalClientCheck();
            }
            catch (RemoteModelException exr)
            {
                MessageBox.Show(String.Format("Не могу соединиться с {0} для получения файла {1}",exr.RemoteAddr, exr.RemoteFile));
                //SettingsBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                return;
            }
            catch (HttpRequestException exhttp)
            {
                MessageBox.Show(String.Format("Не могу соединиться с {0}", exhttp.TargetSite));
                //SettingsBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                //SettingsBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                return;
            }

            if (updater.Difference.Count>0)
            {
                UpdaterActionProgress(this, new UpdaterProgressEventArgs() { ProgressValue=0, ProgressMax=100, InfoStr = "Обновление готово", InfoStrColor=System.Drawing.Color.Green });
                UpdateL2.IsEnabled = true;
                UpdateL2.ToolTip = new TextBlock()
                {
                    Text = String.Format("Обновление {0} файлов. Всего будет скачано: {1} МБ", updater.Difference.Count, updater.Difference.Sum(s => s.FileSize)/1024/1024)
                };
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

        private void UpdaterActionProgress(object sender, UpdaterProgressEventArgs args)
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                updatepercentage.Maximum = args.ProgressMax;
                updatepercentage.Value = args.ProgressValue;
                InfoBlock.Text = args.InfoStr;
                InfoBlock.Foreground = new SolidColorBrush(Color.FromArgb(args.InfoStrColor.A, args.InfoStrColor.R, args.InfoStrColor.G, args.InfoStrColor.B));
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

        private async void UpdateL2_Click(object sender, RoutedEventArgs e)
        {
            if (BtnUpdateL2Text.Text.Equals("Обновить"))
            {
                UpdateL2.Tag = false;
                BtnUpdateL2Text.Text = "Остановить";
                try
                {
                    PlayL2.IsEnabled = false;
                    await updater.UpdateClient();
                    UpdateL2.IsEnabled = false;
                }
                catch (LoaderFilesLoadException exload)
                {
                    MessageBox.Show(String.Format("Не могу скачать {0} файлов", exload.Files.Count));
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }

                if (File.Exists(UpdaterConfig.ClientExeFile))
                {
                    PlayL2.IsEnabled = true;
                }
            } else
            {
                BtnUpdateL2Text.Text = "Обновить";
            }
        }

        #endregion

        #region Full check tab
        private void FullCheckActionProgress(object sender, UpdaterProgressEventArgs args)
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                FullCheckPercentage.Maximum = args.ProgressMax;
                FullCheckPercentage.Value = args.ProgressValue;
                FullCheckLog.AppendText(args.InfoStr + Environment.NewLine);
            });
        }

        private async void BtnFullCheck_Click(object sender, RoutedEventArgs e)
        {
            BtnFullCheck.IsEnabled = false;

            updater = new L2Updater(loader, UpdaterConfig);
            updater.ProgressUpdate += FullCheckActionProgress;
            loader.ProgressUpdate += FullCheckActionProgress;

            try
            {
                await updater.FullLocalClientCheck(true);
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
