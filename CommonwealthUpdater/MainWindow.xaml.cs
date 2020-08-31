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
using Launcher;
using Updater;
using System.Diagnostics;

namespace CommonwealthUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        static L2UpdaterConfig UpdaterConfig;
        FileChecker checker;
        Loader loader;
        L2ClientRemote clientRemote;
        L2ClientLocal clientLocal;


        public MainWindow()
        {
            UpdaterConfig = new L2UpdaterConfig();

            loader = new Loader(UpdaterConfig.ConfigParameters["DownloadAddress"], Convert.ToInt32(UpdaterConfig.ConfigParameters["DownloadPort"]));

            checker = new FileChecker();

            Uri remoteaddr = new UriBuilder("http", UpdaterConfig.ConfigParameters["DownloadAddress"], Convert.ToInt32(UpdaterConfig.ConfigParameters["DownloadPort"])).Uri;

            clientRemote = new L2ClientRemote(remoteaddr, loader);
            clientLocal = new L2ClientLocal(UpdaterConfig.ConfigParameters["ClientFolder"], AppDomain.CurrentDomain.BaseDirectory + "clientinfo.inf");

            InitializeComponent();
        }

        private async void updaterwindow_Initialized(object sender, EventArgs e)
        {
            bool res = await CheckClientOnStart();
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
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            RemoteAddr.Text = UpdaterConfig.ConfigParameters[(string)RemoteAddr.Tag];
            RemotePort.Text = UpdaterConfig.ConfigParameters[(string)RemotePort.Tag];
            ClientDestination.Text = UpdaterConfig.ConfigParameters[(string)ClientDestination.Tag];

            MainSelector.SelectedIndex = 1;
        }

        #endregion

        #region Settings tab events 
        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog commonOpenFileDialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                InitialDirectory = UpdaterConfig.ConfigParameters[(string)ClientDestination.Tag]
            };

            if (commonOpenFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                UpdaterConfig.ConfigParameters[(string)ClientDestination.Tag] = commonOpenFileDialog.FileName;
                ClientDestination.Text = commonOpenFileDialog.FileName;
            }
        }

        private void SettingField_Changed(object sender, TextChangedEventArgs e)
        {
            UpdaterConfig.ConfigParameters[(string)((TextBox)sender).Tag] = ((TextBox)sender).Text;

            UpdaterConfig.Write();
        }
        #endregion

        #region Updater tab events

        List<ClientFileInfo> diff;

        public async Task<bool> CheckClientOnStart()
        {
            await clientLocal.PrepareInfo();
            await clientRemote.PrepareInfo();

            diff = clientRemote.CompareToLocal(clientLocal);

            if (diff.Count>0)
            {
                UpdateL2.IsEnabled = true;
                PlayL2.Background = (SolidColorBrush)(new BrushConverter().ConvertFromString("Red"));
                PlayL2.IsEnabled = clientLocal.ClientisRunnable;
            }

            return true;
        }

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
            proc.StartInfo.FileName = clientLocal.Folder + "//system/l2.exe";
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.Verb = "runas";
            proc.Start();
        }

        private async void UpdateL2_Click(object sender, RoutedEventArgs e)
        {
            if (diff.Count > 0)
            {
                List<ClientFileInfo> error_load = await loader.DownloadClientFiles(clientRemote.Folder.ToString(), clientLocal.Folder.ToString(), diff);
                if (error_load.Count > 0)
                {
                    UpdaterActionProgress(this, new UpdaterProgressEventArgs()
                    {
                        InfoStr = String.Format("Ошибка обновления! Не удалось скачать {0} файлов", error_load.Count),
                        InfoStrColor = System.Drawing.Color.Red,
                        ProgressMax = 0,
                        ProgressValue = 0
                    });
                } else
                {
                    UpdaterActionProgress(this, new UpdaterProgressEventArgs()
                    {
                        InfoStr = "Обновление завершилось успешно",
                        InfoStrColor = System.Drawing.Color.Green,
                        ProgressMax = 100,
                        ProgressValue = 100
                    });
                }
            }
        }
        #endregion


    }
}
