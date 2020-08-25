using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Config;
using Launcher;
using Updater;


namespace CommonwealthUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        L2UpdaterConfig UpdaterConfig;
        FileChecker checker;
        Loader loader;
        
        public MainWindow()
        {
            UpdaterConfig = new L2UpdaterConfig();

            loader = new Loader(UpdaterConfig.ConfigParameters["DownloadAddress"], Convert.ToInt32(UpdaterConfig.ConfigParameters["DownloadPort"]));

            checker = new FileChecker()
            {
                ClientPath = UpdaterConfig.ConfigParameters["ClientFolder"],
                RemoteHashesFile = "",
                ClientHashesFile = AppDomain.CurrentDomain.BaseDirectory + "hashes.txt",
            };
            checker.ProgressUpdate += ActionProgress;

            InitializeComponent();
        }

        public async Task<bool> CheckClientOnStart()
        {
            ActionIndicator.Content = "Пытаюсь установить связь с хранилищем клиента";

            if (await loader.CheckConnect())
            {
                ActionIndicator.Content = "Есть подключение к хранилищу клиента";
                if (await loader.DownloadHashes())
                {
                    ActionIndicator.Content = "Получен файл хэшей";
                    checker.RemoteHashesFile = loader.LocalHashesFile;
                    if (await checker.CheckClientHashes()==false)
                    {
                        if (MessageBox.Show("Желаете обновиться?", "Обновление", MessageBoxButton.YesNo)==MessageBoxResult.Yes)
                        {
                            updatepercentage.Maximum = checker.FilesRemoteDifferent.Count;
                            int percentage = 0;
                            foreach (string file in checker.FilesRemoteDifferent)
                            {
                                updatepercentage.Value = percentage++;
                                string remote_file = loader.ClientPath + "\\" + file;
                                string local_file = checker.ClientPath + "\\" + file;
                                ActionIndicator.Content = "Скачиваю файл: " + file;
                                await loader.DownloadFile(remote_file, local_file);
                                //TODO: add speed meter NetworkInterface GetIPv4ReceivedBytes
                            }
                        }
                    }
                }
            }
            else
            {
                ActionIndicator.Content = "Отсутствует связь с хранилищем клиента. Проверьте подключение к интерненту.";
            }

            return false;
        }

        private void ActionProgress(object sender, FileChecker.FileCheckerProgressEventArgs args)
        {
            Dispatcher.BeginInvoke((Action)delegate () 
            { 
                updatepercentage.Maximum = args.ClientSize; 
                updatepercentage.Value = args.HashedSize; 
                ActionIndicator.Content =args.HashingFileName; 
            });
        }

        private void ClientRun_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            new ConfigWindow(UpdaterConfig).ShowDialog();
        }

        private void Recheck_Click(object sender, RoutedEventArgs e)
        {
            //checker = new FileChecker(UpdaterConfig.ConfigParameters["ClientFolder"], AppDomain.CurrentDomain.BaseDirectory + "//hashes.txt", ActionProgress, true, );
            //checker.ProgressUpdate += ActionProgress;
            checker.ClientPath = UpdaterConfig.ConfigParameters["ClientFolder"];

            Task.Run(checker.ClientFilesCalculateHashes);
        }

        private async void updaterwindow_Initialized(object sender, EventArgs e)
        {
            bool res = await CheckClientOnStart();
        }
    }
}
