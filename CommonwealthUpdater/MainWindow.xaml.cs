using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using Logger;


namespace CommonwealthUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        L2UpdaterConfig UpdaterConfig;
        FileChecker checker;
        CommonLogger logger;
        public MainWindow()
        {
            InitializeComponent();

            logger = new CommonLogger(AppDomain.CurrentDomain.BaseDirectory+"logs");

            UpdaterConfig = new L2UpdaterConfig(logger);

            checker = new FileChecker(logger, UpdaterConfig.ConfigParameters["ClientFolder"], AppDomain.CurrentDomain.BaseDirectory + "//hashes.txt");
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
            checker = new FileChecker(logger, UpdaterConfig.ConfigParameters["ClientFolder"], AppDomain.CurrentDomain.BaseDirectory + "//hashes.txt");
            checker.ClientFilesCalculateHashes();
        }
    }
}
