using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Net.Http;
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
        
        static readonly HttpClient httpClient = new HttpClient();

        private readonly SynchronizationContext _syncContext;


        public MainWindow()
        {
            InitializeComponent();

            _syncContext = SynchronizationContext.Current;

            UpdaterConfig = new L2UpdaterConfig();

            checker = new FileChecker(UpdaterConfig.ConfigParameters["ClientFolder"], AppDomain.CurrentDomain.BaseDirectory + "//hashes.txt");
        }

        private void ActionProgress(object s, EventArgs args)
        {
        }

        private void ClientRun_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            new ConfigWindow(UpdaterConfig).ShowDialog();
        }

        private async void Recheck_Click(object sender, RoutedEventArgs e)
        {
            checker = new FileChecker(UpdaterConfig.ConfigParameters["ClientFolder"], AppDomain.CurrentDomain.BaseDirectory + "//hashes.txt");
            checker.ProgressUpdate += (s, args) =>
            {
                Dispatcher.BeginInvoke((Action)delegate () { updatepercentage.Maximum = checker.ClientSize; updatepercentage.Value = checker.HashedSize; ActionShower.Content = checker.HashingFileName; });
            };

            await Task.Run(checker.ClientFilesCalculateHashes);
        }
    }
}
