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
using Microsoft.WindowsAPICodePack.Dialogs;
using Updater;

namespace ServerPrepare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            SourceFolder.Text = "D:\\Master\\Lineage2\\Clients\\client";
            ServerFolder.Text = "D:\\Master\\Lineage2\\updater_docker\\files\\client";
        }

        private void SelectSourceFolder_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog commonOpenFileDialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                InitialDirectory = SourceFolder.Text
            };

            if (commonOpenFileDialog.ShowDialog()==CommonFileDialogResult.Ok)
            {
                SourceFolder.Text = commonOpenFileDialog.FileName;
            }
        }

        private void SelectServerFolder_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog commonOpenFileDialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                InitialDirectory = ServerFolder.Text
            };

            if (commonOpenFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ServerFolder.Text = commonOpenFileDialog.FileName;
            }
        }

        private async void ProcessFiles_Click(object sender, RoutedEventArgs e)
        {
            FolderProcess folderProcess = new FolderProcess(SourceFolder.Text, ServerFolder.Text);
            folderProcess.ProgressUpdate += ProcessActionProgress;
            await folderProcess.StartCompress();
        }

        private void ProcessActionProgress(object sender, FolderProgressEventArgs args)
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                InfoProgress.Maximum = args.ProgressMax;
                InfoProgress.Value = args.ProgressValue;
                InfoStr.Text = args.InfoStr;
                InfoStr.Foreground = new SolidColorBrush(Color.FromArgb(args.InfoStrColor.A, args.InfoStrColor.R, args.InfoStrColor.G, args.InfoStrColor.B));
            });
        }

        private async void HashFiles_Click(object sender, RoutedEventArgs e)
        {
            FolderProcess folderProcess = new FolderProcess(SourceFolder.Text, ServerFolder.Text);
            folderProcess.ProgressUpdate += ProcessActionProgress;
            await folderProcess.StartModel();
        }
    }
}
