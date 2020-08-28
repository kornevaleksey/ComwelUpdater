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
using System.Diagnostics;

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
        L2Client client;

        private bool isMoving;
        private double _startX;
        private double _startY;

        public MainWindow()
        {
            UpdaterConfig = new L2UpdaterConfig();

            loader = new Loader(UpdaterConfig.ConfigParameters["DownloadAddress"], Convert.ToInt32(UpdaterConfig.ConfigParameters["DownloadPort"]));

            checker = new FileChecker();

            client = new L2Client(loader)
            {
                ClientPath = UpdaterConfig.ConfigParameters["ClientFolder"],
                RemoteHashesFile = "hashes//hashes.txt",
                ClientHashesFile = AppDomain.CurrentDomain.BaseDirectory + "hashes.txt",
            };
            client.ProgressUpdate += ActionProgress;

            isMoving = false;

            InitializeComponent();
        }

        public async Task<bool> CheckClientOnStart()
        {
            await client.CheckClient(true);
            if ((client.LocalDifference!=null))
            {
                if (client.LocalDifference.Count > 0)
                {
                    PlayL2.IsEnabled = false;
                    UpdateL2.IsEnabled = true;
                }
                else
                {
                    PlayL2.IsEnabled = false;
                    UpdateL2.IsEnabled = true;
                }
                return true;
            } else
            return false;
        }

        private void ActionProgress(object sender, UpdaterProgressEventArgs args)
        {
            Dispatcher.BeginInvoke((Action)delegate () 
            { 
                updatepercentage.Maximum = args.ProgressMax; 
                updatepercentage.Value = args.ProgressValue; 
                InfoBlock.Text = args.InfoStr;
                InfoBlock.Foreground = new SolidColorBrush(Color.FromArgb(args.InfoStrColor.A, args.InfoStrColor.R, args.InfoStrColor.G, args.InfoStrColor.B));
            });
        }

        private void ClientRun_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Recheck_Click(object sender, RoutedEventArgs e)
        {
        }

        private async void updaterwindow_Initialized(object sender, EventArgs e)
        {
            bool res = await CheckClientOnStart();
        }

        private async void actionselector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            var selectedItem = comboBox.SelectedItem;
            if (selectedItem.GetType() == typeof(ComboBoxItem))
            switch (((ComboBoxItem)selectedItem).Content)
            {
                case "Играть":
                        Process proc = new Process();
                        proc.StartInfo.FileName = client.ClientPath + "//system/l2.exe";
                        proc.StartInfo.UseShellExecute = true;
                        proc.StartInfo.Verb = "runas";
                        proc.Start();
                    break;
                case "Обновление":
                        updatepercentage.Maximum = client.LocalDifference.Count;
                        int percentage = 0;
                        foreach (string file in client.LocalDifference)
                        {
                            updatepercentage.Value = percentage++;
                            string remote_file = loader.ClientPath + "\\" + file;
                            string local_file = client.ClientPath + "\\" + file;
                            InfoBlock.Text = "Скачиваю файл: " + file;
                            await loader.DownloadFile(remote_file, local_file);
                            //TODO: add speed meter NetworkInterface GetIPv4ReceivedBytes
                        }
                        await client.CheckClient(true);
                        if (client.LocalDifference.Count==0)
                            ((ComboBoxItem)selectedItem).Content = "Играть";
                        break;
                case "Параметры":
                        new ConfigWindow(UpdaterConfig).ShowDialog();
                    break;
                case "Перепроверить файлы":
                        client.ClientPath = UpdaterConfig.ConfigParameters["ClientFolder"];
                        await client.CheckClient(false);
                        //Task.Run(checker.ClientFilesCalculateHashes);
                    break;
                case "О программе":
                    break;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MainGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _startX = Mouse.GetPosition(this).X;
            _startY = Mouse.GetPosition(this).Y;
            //isMoving = true;
        }

        private void MainGrid_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!isMoving) return;

            this.Top = Mouse.GetPosition(this).Y - _startY;
            this.Left = Mouse.GetPosition(this).X - _startX;
        }

        private void MainGrid_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            isMoving = false;
        }
    }
}
