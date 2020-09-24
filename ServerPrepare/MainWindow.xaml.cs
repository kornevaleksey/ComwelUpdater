using System;
using System.Collections.Generic;
using System.IO;
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
using Updater;
using Ookii.Dialogs.Wpf;

namespace ServerPrepare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PrepareSettings Settings;
        SourceConfig sourcefolderconfig;
        ServerConfig serverfolderconfig;

        public MainWindow()
        {
            Settings = new PrepareSettings();
            InitializeComponent();
        }

        private void SelectSourceFolder_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog folderdialog = new VistaFolderBrowserDialog()
            {
                SelectedPath = SourceFolder.Text,
                ShowNewFolderButton = true
            };

            if (folderdialog.ShowDialog(this) == true)
            {
                SourceFolder.Text = folderdialog.SelectedPath;
            }
        }

        private void TreeUpdateSource (SourceConfig fconfig, TreeView treeview)
        {
            treeview.Items.Clear();
            List<string> toplevel = Directory.GetDirectories(fconfig.ClientFolder).ToList();

            foreach (string dir in toplevel)
            {
                string additem = System.IO.Path.GetRelativePath(fconfig.ClientFolder, dir);

                TreeViewItem item = new TreeViewItem()
                {
                    Header = additem
                };
                treeview.Items.Add(item);

                List<string> files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories).ToList();

                foreach (string file in files)
                {
                    IL2FileInfo info = fconfig.FileInfos?.Find(inf => String.Compare(inf.FileName, System.IO.Path.GetRelativePath(dir, file), true) == 0);

                    StackPanel addpanel = new StackPanel()
                    {
                        Orientation = Orientation.Horizontal
                    };
                    TextBlock tbFileName = new TextBlock()
                    {
                        Text = System.IO.Path.GetRelativePath(dir, file)
                    };
                    TextBlock tbImportant = new TextBlock()
                    {
                        Text = info!=null&&info.Important==true?"Важен":""
                    };

                    addpanel.Children.Add(tbFileName);
                    addpanel.Children.Add(tbImportant);

                    item.Items.Add(addpanel);

                    /*
                    item.Items.Add(new TreeViewItem()
                    {
                        Header = System.IO.Path.GetRelativePath(dir, file)
                    });
                    */
                }
                
            }
        }

        private void TreeUpdateServer(ServerConfig fconfig, TreeView treeview)
        {
            treeview.Items.Clear();
            List<string> toplevel = Directory.GetDirectories(fconfig.ClientFolder).ToList();

            foreach (string dir in toplevel)
            {
                string additem = System.IO.Path.GetRelativePath(fconfig.ClientFolder, dir);

                TreeViewItem item = new TreeViewItem()
                {
                    Header = additem
                };
                treeview.Items.Add(item);

                List<string> files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories).ToList();

                foreach (string file in files)
                {
                    IL2FileInfo info = fconfig.FileInfos?.Find(inf => String.Compare(inf.FileName, System.IO.Path.GetRelativePath(dir, file), true) == 0);

                    StackPanel addpanel = new StackPanel();
                    TextBlock tbFileName = new TextBlock()
                    {
                        Text = System.IO.Path.GetRelativePath(fconfig.ClientFolder, dir)
                    };
                    TextBlock tbImportant = new TextBlock()
                    {
                        Text = info != null ? info.Important.ToString() : ""
                    };

                    item.Items.Add(new TreeViewItem()
                    {
                        Header = System.IO.Path.GetRelativePath(dir, file)
                    });
                }

            }
        }

        private void SelectServerFolder_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog folderdialog = new VistaFolderBrowserDialog()
            {
                SelectedPath = ServerFolder.Text,
                ShowNewFolderButton = true
            };

            if (folderdialog.ShowDialog(this) == true)
            {
                ServerFolder.Text = folderdialog.SelectedPath;
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

        private void Window_Initialized(object sender, EventArgs e)
        {
            Task.Run( async () =>
            {
                await Settings.ReadSettings();
                sourcefolderconfig = new SourceConfig(Settings.SourceFolder);
                serverfolderconfig = new ServerConfig(Settings.ServerFolder);
                await sourcefolderconfig.ReadInfo();
                await serverfolderconfig.ReadInfo();
                Dispatcher.Invoke(() =>
                {
                    SourceFolder.Text = Settings.SourceFolder.LocalPath;
                    ServerFolder.Text = Settings.ServerFolder.LocalPath;
                });
            });
        }

        private void SourceFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.SourceFolder = new Uri(SourceFolder.Text);
            Settings.SaveSettings();
            sourcefolderconfig = new SourceConfig(Settings.SourceFolder);
            TreeUpdateSource(sourcefolderconfig, TreeSourceFiles);
        }

        private void ServerFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.ServerFolder = new Uri(ServerFolder.Text);
            Settings.SaveSettings();
            serverfolderconfig = new ServerConfig(Settings.ServerFolder);
            TreeUpdateServer(serverfolderconfig, TreeServerFiles);
        }

        private void SourceInfoProgress (double progress, string message)
        {
            Dispatcher.Invoke(() =>
            {
                InfoStr.Text = message;
                InfoProgress.Value = progress;
            });
        }

        private void SourceHashFiles_Click(object sender, RoutedEventArgs e)
        {
            Task.Run( async () =>
            {
                sourcefolderconfig.CreateInfoProgress = SourceInfoProgress;
                await sourcefolderconfig.InitFolder();
                await sourcefolderconfig.CreateInfo();
                await sourcefolderconfig.WriteInfo();
            });

        }
    }
}
