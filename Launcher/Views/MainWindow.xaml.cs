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
using System.Reflection;

namespace Launcher.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindowView : Window
    {
        //static UpdaterConfig UpdaterConfig;

        //L2Updater updater;

        //CancellationTokenSource updaterCancellationTokenSource;
        //CancellationTokenSource fullcheckCancellationTokenSource;
        //readonly Mutex MutexLauncherRunning, MutexClientDirectory;

        //public enum LauncherActions
        //{
        //    WaitForAction,
        //    ConfigNotSet,
        //    SelfUpdate,
        //    CheckFilesFast,
        //    CheckFilesFull,
        //    UpdateFiles,
        //    UpdateFilesFull,
        //    Lineage2Run
        //};

        //LauncherActions LauncherStatus
        //{
        //    get => _LauncherStatus;
        //    set
        //    { 
        //        switch (_LauncherStatus)
        //        {
        //            case LauncherActions.WaitForAction:
        //                switch (value)
        //                {
        //                    case LauncherActions.WaitForAction:
        //                        WaitForAction_Enter();
        //                        break;
        //                    case LauncherActions.SelfUpdate:
        //                        WaitForAction_Exit();
        //                        SelfUpdate_Enter();
        //                        break;
        //                    case LauncherActions.CheckFilesFast:
        //                        WaitForAction_Exit();
        //                        CheckFilesFast_Enter();
        //                        break;
        //                    case LauncherActions.UpdateFiles:
        //                        WaitForAction_Exit();
        //                        UpdateFiles_Enter();
        //                        break;
        //                    case LauncherActions.Lineage2Run:
        //                        WaitForAction_Exit();
        //                        Lineage2Run_Enter();
        //                        break;
        //                    case LauncherActions.CheckFilesFull:
        //                        WaitForAction_Exit();
        //                        CheckFilesFull_Enter();
        //                        break;
        //                    case LauncherActions.UpdateFilesFull:
        //                        WaitForAction_Exit();
        //                        UpdateFilesFull_Enter();
        //                        break;
        //                    case LauncherActions.ConfigNotSet:
        //                        WaitForAction_Exit();
        //                        ConfigNotSet_Enter();
        //                        break;
        //                }
        //                _LauncherStatus = value;
        //                break;
        //            case LauncherActions.SelfUpdate:
        //                switch (value)
        //                {
        //                    case LauncherActions.WaitForAction:
        //                        SelfUpdate_Exit();
        //                        WaitForAction_Enter();
        //                        _LauncherStatus = value;
        //                        break;
        //                    case LauncherActions.SelfUpdate:
        //                        SelfUpdate_Enter();
        //                        _LauncherStatus = value;
        //                        break;
        //                    case LauncherActions.ConfigNotSet:
        //                        SelfUpdate_Exit();
        //                        ConfigNotSet_Enter();
        //                        _LauncherStatus = value;
        //                        break;
        //                    case LauncherActions.CheckFilesFast:
        //                    case LauncherActions.UpdateFiles:
        //                    case LauncherActions.Lineage2Run:
        //                    case LauncherActions.CheckFilesFull:
        //                    case LauncherActions.UpdateFilesFull:
        //                        break;
        //                }
        //                break;
        //            case LauncherActions.ConfigNotSet:
        //                switch (value)
        //                {
        //                    case LauncherActions.WaitForAction:
        //                        ConfigNotSet_Exit();
        //                        WaitForAction_Enter();
        //                        _LauncherStatus = value;
        //                        break;
        //                    case LauncherActions.Lineage2Run:
        //                        ConfigNotSet_Exit();
        //                        Lineage2Run_Enter();
        //                        _LauncherStatus = value;
        //                        break;
        //                    case LauncherActions.ConfigNotSet:
        //                        ConfigNotSet_Enter();
        //                        _LauncherStatus = value;
        //                        break;
        //                    case LauncherActions.SelfUpdate:
        //                    case LauncherActions.CheckFilesFast:
        //                    case LauncherActions.UpdateFiles:
        //                    case LauncherActions.CheckFilesFull:
        //                    case LauncherActions.UpdateFilesFull:
        //                        break;
        //                }
        //                break;
        //            case LauncherActions.CheckFilesFast:
        //                switch (value)
        //                {
        //                    case LauncherActions.WaitForAction:
        //                        CheckFilesFast_Exit();
        //                        WaitForAction_Enter();
        //                        _LauncherStatus = value;
        //                        break;
        //                    case LauncherActions.CheckFilesFast:
        //                        CheckFilesFast_Enter();
        //                        _LauncherStatus = value;
        //                        break;
        //                    case LauncherActions.ConfigNotSet:
        //                        CheckFilesFast_Exit();
        //                        ConfigNotSet_Enter();
        //                        _LauncherStatus = value;
        //                        break;
        //                    case LauncherActions.Lineage2Run:
        //                    case LauncherActions.CheckFilesFull:
        //                    case LauncherActions.UpdateFilesFull:
        //                    case LauncherActions.UpdateFiles:
        //                    case LauncherActions.SelfUpdate:
        //                        break;
        //                }
        //                break;
        //            case LauncherActions.UpdateFiles:
        //                switch (value)
        //                {
        //                    case LauncherActions.WaitForAction:
        //                        UpdateFiles_Exit();
        //                        CheckFilesFast_Enter();
        //                        _LauncherStatus = value;
        //                        break;
        //                    case LauncherActions.ConfigNotSet:
        //                        UpdateFiles_Exit();
        //                        ConfigNotSet_Enter();
        //                        _LauncherStatus = value;
        //                        break;
        //                    case LauncherActions.UpdateFiles:
        //                        UpdateFiles_Enter();
        //                        _LauncherStatus = value;
        //                        break;
        //                    case LauncherActions.SelfUpdate:
        //                    case LauncherActions.CheckFilesFast:
        //                    case LauncherActions.Lineage2Run:
        //                    case LauncherActions.CheckFilesFull:
        //                    case LauncherActions.UpdateFilesFull:
        //                        break;
        //                }
        //                break;
        //            case LauncherActions.Lineage2Run:
        //                switch (value)
        //                {
        //                    case LauncherActions.WaitForAction:
        //                        if (Lineage2Run_Exit())
        //                        {
        //                            WaitForAction_Enter();
        //                            _LauncherStatus = value;
        //                        }
        //                        break;
        //                    case LauncherActions.Lineage2Run:
        //                        Lineage2Run_Enter();
        //                        _LauncherStatus = value;
        //                        break;
        //                    case LauncherActions.ConfigNotSet:
        //                        Lineage2Run_Exit();
        //                        ConfigNotSet_Enter();
        //                        _LauncherStatus = value;
        //                        break;
        //                    case LauncherActions.SelfUpdate:
        //                    case LauncherActions.CheckFilesFast:
        //                    case LauncherActions.UpdateFiles:
        //                    case LauncherActions.CheckFilesFull:
        //                    case LauncherActions.UpdateFilesFull:
        //                        break;
        //                }
        //                break;
        //            case LauncherActions.CheckFilesFull:
        //                switch (value)
        //                {
        //                    case LauncherActions.WaitForAction:
        //                        CheckFilesFull_Exit();
        //                        WaitForAction_Enter();
        //                        _LauncherStatus = value;
        //                        break;
        //                    case LauncherActions.ConfigNotSet:
        //                        CheckFilesFull_Exit();
        //                        ConfigNotSet_Enter();
        //                        _LauncherStatus = value;
        //                        break;
        //                    case LauncherActions.SelfUpdate:
        //                    case LauncherActions.CheckFilesFast:
        //                    case LauncherActions.UpdateFiles:
        //                    case LauncherActions.Lineage2Run:
        //                    case LauncherActions.CheckFilesFull:
        //                    case LauncherActions.UpdateFilesFull:
        //                        break;
        //                }
        //                break;
        //            case LauncherActions.UpdateFilesFull:
        //                switch (value)
        //                {
        //                    case LauncherActions.WaitForAction:
        //                        UpdateFilesFull_Exit();
        //                        WaitForAction_Enter();
        //                        _LauncherStatus = value;
        //                        break;
        //                    case LauncherActions.UpdateFilesFull:
        //                        UpdateFilesFull_Enter();
        //                        _LauncherStatus = value;
        //                        break;
        //                    case LauncherActions.ConfigNotSet:
        //                        UpdateFilesFull_Exit();
        //                        ConfigNotSet_Enter();
        //                        _LauncherStatus = value;
        //                        break;
        //                    case LauncherActions.SelfUpdate:
        //                    case LauncherActions.CheckFilesFast:
        //                    case LauncherActions.UpdateFiles:
        //                    case LauncherActions.Lineage2Run:
        //                    case LauncherActions.CheckFilesFull:
        //                        break;
        //                }
        //                break;
        //        }
        //    }
        //}

        //string runningmutexname = "CommonwealthClientUpdaterMutex";

        //LauncherActions _LauncherStatus;

        public MainWindowView()
        {
            InitializeComponent();

            //UpdaterConfig = new UpdaterConfig();

            /*
            bool mutexnew;
            MutexLauncherRunning = new Mutex(false, runningmutexname, out mutexnew);
            if (mutexnew==false)
            {
                MutexClientDirectory = new Mutex(false, runningmutexname + UpdaterConfig.ConfigFields.ClientFolder.LocalPath, out mutexnew);
                if (mutexnew == false)
                    this.Close();
            }
            */

            /*
            var version = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            version = FileVersionInfo.GetVersionInfo(Process.GetCurrentProcess().MainModule.FileName);
            */



            //UpdaterConfig.ConfigFinishedRead += ConfigFinishedRead;
            //UpdaterConfig.ConfigReadError += ConfigReadError;
            //UpdaterConfig.Read();

            //txtbVersionInfo.Text = String.Format("Версия {0} от {1} © Korall", Assembly.GetEntryAssembly().GetName().Version, Launcher.Properties.Resources.BuildDate.Trim());

            //Timer runninglineage2 = new Timer((object stateinfo) =>
            //{
            //    if (SearchForRunningLineageProcess())
            //        LauncherStatus = LauncherActions.Lineage2Run;
            //});
            //runninglineage2.Change(0, 100);
        }

        //private void WaitForAction_Enter()
        //{
        //    UpdaterSelect.IsEnabled = true;
        //    SettingsBtn.IsEnabled = true;
        //    RecheckBtn.IsEnabled = true;

        //    PlayL2.IsEnabled = updater!=null && updater.ClientCanRun;
        //    UpdateL2.IsEnabled = updater!=null && updater.UpdateIsNeed;
        //    BtnFullCheck.IsEnabled = true;
        //    BtnFullReload.IsEnabled = true;
        //    BtnSelect.IsEnabled = true;
        //    ChkGameFolder.IsEnabled = true;

        //    RemoteAddr.IsReadOnly = false;
        //}
        //private void WaitForAction_Exit()
        //{
        //    UpdaterSelect.IsEnabled = false;
        //    SettingsBtn.IsEnabled = false;
        //    RecheckBtn.IsEnabled = false;

        //    PlayL2.IsEnabled = false;
        //    UpdateL2.IsEnabled = false;
        //    BtnFullCheck.IsEnabled = false;
        //    BtnFullReload.IsEnabled = false;
        //    BtnSelect.IsEnabled = false;
        //    ChkGameFolder.IsEnabled = false;

        //    RemoteAddr.IsReadOnly = true;
        //}

        //private void ConfigNotSet_Enter ()
        //{
        //    SettingsBtn.IsEnabled = true;
        //}

        //private void ConfigNotSet_Exit()
        //{
        //}

        //private void SelfUpdate_Enter()
        //{
        //    UpdaterSelect.IsEnabled = false;
        //    SettingsBtn.IsEnabled = false;
        //    RecheckBtn.IsEnabled = false;

        //    PlayL2.IsEnabled = false;
        //    UpdateL2.IsEnabled = false;
        //    BtnFullCheck.IsEnabled = false;
        //    BtnFullReload.IsEnabled = false;
        //    BtnSelect.IsEnabled = false;
        //    ChkGameFolder.IsEnabled = false;

        //    RemoteAddr.IsReadOnly = true;
        //}

        //private void SelfUpdate_Exit()
        //{

        //}

        //private void CheckFilesFast_Enter()
        //{
        //    UpdaterSelect.IsEnabled = true;
        //    SettingsBtn.IsEnabled = true;
        //    RecheckBtn.IsEnabled = true;
        //}

        //private void CheckFilesFast_Exit()
        //{

        //}

        //private void UpdateFiles_Enter()
        //{
        //    UpdaterSelect.IsEnabled = true;
        //    SettingsBtn.IsEnabled = true;

        //    BtnUpdateL2Text.Text = "Остановить";
        //    UpdateL2.IsEnabled = true;
        //}

        //private void UpdateFiles_Exit()
        //{
        //    BtnUpdateL2Text.Text = "Обновить";
        //}

        //private void Lineage2Run_Enter()
        //{
        //    PlayL2.IsEnabled = true;
        //}

        //private bool Lineage2Run_Exit()
        //{
        //    if (SearchForRunningLineageProcess())
        //        return false;
        //    else
        //        return true;
        //}

        //private void CheckFilesFull_Enter()
        //{
        //    RecheckBtn.IsEnabled = true;
        //    BtnFullCheck.IsEnabled = true;
        //    BtnFullCheckText.Text = "Остановить\nпроверку";
        //}

        //private void CheckFilesFull_Exit()
        //{
        //    BtnFullCheckText.Text = "Полная проверка";
        //}

        //private void UpdateFilesFull_Enter()
        //{
        //    RecheckBtn.IsEnabled = true;
        //    BtnFullReload.IsEnabled = true;
        //    BtnFullReloadText.Text = "Остановить\nобновление";
        //}

        //private void UpdateFilesFull_Exit()
        //{
        //    BtnFullReloadText.Text = "Полное обновление";
        //}

        //private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    if (Mouse.LeftButton == MouseButtonState.Pressed)
        //        this.DragMove();
        //}

        //private void ConfigFinishedRead (object sender, EventArgs args)
        //{
        //    ChkGameFolder.IsChecked = UpdaterConfig.ConfigFields.PlacedInClientFolder;
        //    ClientDestination.Text = UpdaterConfig.ConfigFields.ClientFolder.LocalPath;
        //    RemoteAddr.Text = UpdaterConfig.ConfigFields.DownloadAddress.ToString();

        //    UpdaterSelect.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        //}

        //private void ConfigReadError(object sender, ConfigRWErrorEventArgs args)
        //{
        //    switch (args.ReadException)
        //    {
        //        case ArgumentNullException nullex:
        //            LauncherDialogs.MessageBox(String.Format("Нет конфигурации программы! Введите настройки. {0}{1}", Environment.NewLine, nullex.Message));
        //            break;
        //        default:
        //            //LauncherDialogs.MessageBox(String.Format("Ошибка чтения файла конфигурации! {0}", args.ReadException.Message));
        //            break;
        //    }

        //    UpdaterSelect.IsEnabled = false;
        //    RecheckBtn.IsEnabled = false;

        //    SettingsBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        //}

        //#region Selection DockPanel events

        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    this.Close();
        //}

        //private void Button_Click_1(object sender, RoutedEventArgs e)
        //{
        //    this.WindowState = WindowState.Minimized;
        //}

        //private void UpdaterClick(object sender, RoutedEventArgs e)
        //{
        //    MainSelector.SelectedIndex = 0;

        //    if (LauncherStatus == LauncherActions.WaitForAction)
        //    {
        //        LauncherStatus = LauncherActions.CheckFilesFast;

        //        UpdateL2.IsEnabled = false;

        //        SimpleHttpLoader loader = new SimpleHttpLoader
        //        {
        //            RemoteAddr = new UriBuilder("http", UpdaterConfig.ConfigFields.DownloadAddress.Host, 9000).Uri,
        //            RemoteInfoAddr = new UriBuilder("https", UpdaterConfig.ConfigFields.DownloadAddress.Host, 9001).Uri
        //        };
        //        updater = new L2Updater(loader, UpdaterConfig);
        //        UpdateL2.Tag = updater;

        //        updater.ClientCheckUpdate += ClientCheckUpdate;
        //        updater.ClientCheckFinished += ClientCheckFinish;
        //        updater.ClientCheckError += ClientCheckError;
        //        loader.LoaderProgress += ClientLoadUpdate;
        //        loader.UnZipProgress += LoadFileExtractProgress;
                

        //        InfoBlock.Text = "Проверка клиента игры Lineage II";

        //        try
        //        {
        //            updater.FastLocalClientCheck();
        //        }
        //        finally
        //        {
        //        }
        //    } 
        //    else if (LauncherStatus == LauncherActions.CheckFilesFast)
        //    {

        //    }
        //}

        //private void Settings_Click(object sender, RoutedEventArgs e)
        //{
        //    ChkGameFolder.IsChecked = UpdaterConfig.ConfigFields.PlacedInClientFolder;
        //    RemoteAddr.Text = UpdaterConfig.ConfigFields.DownloadAddress==null?"": UpdaterConfig.ConfigFields.DownloadAddress.ToString();
        //    ClientDestination.Text = UpdaterConfig.ConfigFields.ClientFolder==null?"": UpdaterConfig.ConfigFields.ClientFolder.LocalPath;

        //    if (UpdaterConfig.ConfigFields.PlacedInClientFolder)
        //        BtnSelect.IsEnabled = false;

        //    MainSelector.SelectedIndex = 1;
        //}

        //private void RecheckBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    MainSelector.SelectedIndex = 2;
        //}

        //private void AboutBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    MainSelector.SelectedIndex = 3;
        //}

        //private void GudilapBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    Process runbrowser = new Process();
        //    runbrowser.StartInfo.UseShellExecute = true;
        //    runbrowser.StartInfo.FileName = "http://www.gudilap.ru";

        //    runbrowser.Start();

        //    e.Handled = true;
        //}

        //#endregion

        //#region Settings tab events 

        //private void ConfigFinishedWrite (object sender, EventArgs args)
        //{
        //    UpdaterSelect.IsEnabled = true;
        //    RecheckBtn.IsEnabled = true;

        //    if (UpdaterConfig.ConfigFields.PlacedInClientFolder==false)
        //    {
        //        var cfg = new L2UpdaterConfig();
        //        cfg.ConfigFields.PlacedInClientFolder = true;
        //        if (File.Exists(cfg.ConfigFile))
        //            File.Delete(cfg.ConfigFile);
        //    }

        //    TxtBSaveSettings.Text = "Настройки сохранены";
        //    TxtBSaveSettings.Foreground = Brushes.Green;
        //}

        //private void ConfigWriteError(object sender, ConfigRWErrorEventArgs args)
        //{
        //    TxtBSaveSettings.Text = "Не получилось сохранить настройки!";
        //    TxtBSaveSettings.Foreground = Brushes.Red;
        //}

        //private void SettingsSave()
        //{
        //    if ((Uri.IsWellFormedUriString(RemoteAddr.Text, UriKind.Absolute)) && Directory.Exists(ClientDestination.Text))
        //    {
        //        TxtBSaveSettings.Text = "Сохраняю настройки";

        //        UpdaterConfig.ConfigFinishedWrite = ConfigFinishedWrite;
        //        UpdaterConfig.ConfigWriteError = ConfigWriteError;

        //        UpdaterConfig.ConfigFields.DownloadAddress = new Uri(RemoteAddr.Text);
        //        UpdaterConfig.ConfigFields.ClientFolder = new Uri(ClientDestination.Text);
        //        UpdaterConfig.ConfigFields.PlacedInClientFolder = ChkGameFolder.IsChecked ?? false;
        //        UpdaterConfig.Write();
        //    } else
        //    {
        //        TxtBSaveSettings.Text = "Не могу сохранить настройки - неверные данные!";
        //        TxtBSaveSettings.Foreground = Brushes.Red;
        //    }

        //}

        //private void ChkGameFolder_Checked(object sender, RoutedEventArgs e)
        //{
        //    BtnSelect.IsEnabled = false;
        //    UpdaterConfig.ConfigFields.PlacedInClientFolder = true;
        //    ClientDestination.Text = UpdaterConfig.ConfigFields.ClientFolder.LocalPath;
        //    SettingsSave();
        //}

        //private void ChkGameFolder_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    BtnSelect.IsEnabled = true;
        //    UpdaterConfig.ConfigFields.PlacedInClientFolder = false;
        //    ClientDestination.Text = UpdaterConfig.ConfigFields.ClientFolder.LocalPath;
        //    SettingsSave();
        //}

        //private void BtnSelect_Click(object sender, RoutedEventArgs e)
        //{
        //    VistaFolderBrowserDialog folderdialog = new VistaFolderBrowserDialog()
        //    {
        //        SelectedPath = UpdaterConfig.ConfigFields.ClientFolder==null?"": Path.GetFullPath ( UpdaterConfig.ConfigFields.ClientFolder.LocalPath)+"\\",
        //        ShowNewFolderButton = true
        //    };

        //    if (folderdialog.ShowDialog(this) == true)
        //    {
        //        UpdaterConfig.ConfigFields.ClientFolder = new Uri(folderdialog.SelectedPath);
        //        ClientDestination.Text = folderdialog.SelectedPath;
        //    }
        //}

        //private void RemoteAddrChecked (object sender, LoaderConnectionCheckEventArgs args)
        //{
        //    if (args.CheckException==null)
        //    {
        //        RemoteOK.Fill = Brushes.Green;
        //    } else
        //    {
        //        RemoteAddr.ToolTip = new TextBlock() { Text = "Не могу соединиться с сервером" };
        //    }
        //}
        
        //private void RemoteAddrChanged(object sender, TextChangedEventArgs e)
        //{
        //    SettingsSave();
        //    TextBox remoteaddr = (TextBox)sender;
        //    if (Uri.IsWellFormedUriString(remoteaddr.Text, UriKind.Absolute))
        //    {
        //        remoteaddr.ToolTip = null;
        //        RemoteOK.Fill = Brushes.Orange;
        //        SimpleHttpLoader lr = new SimpleHttpLoader()
        //        {
        //            RemoteAddr = new Uri(remoteaddr.Text)
        //        };
        //        lr.ConnectionCheck += RemoteAddrChecked;

        //        lr.CheckConnect();
        //    } else
        //    {
        //        RemoteOK.Fill = Brushes.Red;
        //        remoteaddr.ToolTip = new TextBlock() { Text = "Неверный формат адреса!" };
        //    }
        //}

        //private void ClientDestination_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    TextBox dest = (TextBox)sender;
        //    if (Directory.Exists(dest.Text))
        //    {
        //        DestinationOK.Fill = Brushes.Green;
        //        dest.ToolTip = null;
        //        SettingsSave();
        //    } else
        //    {
        //        DestinationOK.Fill = Brushes.Red;
        //        dest.ToolTip = new TextBlock() { Text = "Указанного расположения не существует!" };
        //    }
        //}

        //#endregion

        //#region Updater tab events

        //private void ClientCheckFinish (object sender, ClientCheckFinishEventArgs args)
        //{
        //    L2Updater updater = (L2Updater)sender;

        //    Dispatcher.BeginInvoke((Action)delegate ()
        //    {
        //        updatepercentage.Maximum = 0;
        //        updatepercentage.Value = 0;
        //        InfoBlock.Text = args.FinishMessage;
        //        InfoBlock.Foreground = args.UpdateRequired? Brushes.Black : Brushes.Green;

        //        if (updater.Difference?.Count > 0)
        //        {
        //            UpdateL2.IsEnabled = true;
        //            UpdateL2.ToolTip = new TextBlock()
        //            {
        //                Text = String.Format("Обновление {0} файлов. Всего будет скачано: {1} МБ, необходимое место на диске {2} МБ",
        //                updater.Difference.Count,
        //                updater.Difference.Sum(s => s.FileSizeCompressed) / 1024 / 1024,
        //                updater.Difference.Sum(s => s.FileSize) / 1024 / 1024)
        //            };
        //        }

        //        LauncherStatus = LauncherActions.WaitForAction;
        //    });
        //}

        //private void ClientCheckError (object sender, ClientCheckErrorEventArgs args)
        //{
        //    switch (args.Exeption)
        //    {
        //        case HttpRequestException reqex:
        //            Dispatcher.BeginInvoke((Action)delegate ()
        //            {
        //                updatepercentage.Maximum = 0;
        //                updatepercentage.Value = 0;
        //                InfoBlock.Text = "Не могу соединиться с сервером!";
        //                InfoBlock.Foreground = Brushes.Red;
        //            });
        //            break;
        //        default:
        //            LauncherDialogs.MessageBox(args.Exeption.Message);
        //            break;
        //    }
        //    LauncherStatus = LauncherActions.WaitForAction;
        //}

        //private void ClientCheckUpdate(object sender, ClientCheckUpdateEventArgs args)
        //{
        //    Dispatcher.BeginInvoke((Action)delegate ()
        //    {
        //        updatepercentage.Maximum = args.ProgressMax;
        //        updatepercentage.Value = args.ProgressValue;
        //        InfoBlock.Text = args.InfoStr;
        //        InfoBlock.Foreground = Brushes.Black;
        //    });
        //}

        //private void ClientLoadUpdate(object sender, LoaderProgressEventArgs args)
        //{
        //    Dispatcher.BeginInvoke((Action)delegate ()
        //    {
        //        updatepercentage.Maximum = 100;
        //        updatepercentage.Value = args.Percentage*100.0;
        //    });
        //}

        //private void LoadFileExtractProgress(object sender, LoaderUnZipProgressEventArgs args)
        //{
        //    Dispatcher.BeginInvoke((Action)delegate ()
        //    {
        //        InfoBlock.Text = String.Format("Скачиваю файл {0} - {1:F0}% готово", args.FileName, args.Percentage);
        //        InfoBlock.Foreground = Brushes.Black;
        //    });
        //}

        //private void ClientUpdateFinished (object sender, EventArgs args)
        //{
        //    L2Updater updater = (L2Updater)sender;
        //    Dispatcher.BeginInvoke((Action)delegate ()
        //    {
        //        UpdateL2.IsEnabled = false;
        //        PlayL2.IsEnabled = updater.ClientCanRun;

        //        updatepercentage.Maximum = 100;
        //        updatepercentage.Value = 100;
        //        InfoBlock.Text = String.Format("Игра обновлена");
        //        InfoBlock.Foreground = Brushes.Green;

        //        LauncherStatus = LauncherActions.WaitForAction;
        //    });
        //}

        //private void ProcessL2Exited (object sender, EventArgs args)
        //{
        //    Dispatcher.BeginInvoke((Action)delegate ()
        //    {
        //        LauncherStatus = LauncherActions.WaitForAction;
        //    });
        //}

        //private void PlayL2_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        LauncherStatus = LauncherActions.Lineage2Run;
        //        ProcessStartInfo l2info = new ProcessStartInfo();
        //        l2info.EnvironmentVariables["__COMPAT_LAYER"] = "RunAsInvoker";
        //        l2info.FileName = UpdaterConfig.ClientExeFile;

        //        Process l2run = new Process
        //        {
        //            EnableRaisingEvents = true,
        //            StartInfo = l2info
        //        };
        //        l2run.Exited += ProcessL2Exited;

        //        l2run.Start();
        //    }
        //    catch (Exception ex)
        //    {
        //        InfoBlock.Text = "Запуск невозможен!";
        //        InfoBlock.Foreground = Brushes.Red;
        //        InfoBlock2.Text = ex.Message;
        //        InfoBlock2.Foreground = Brushes.Black;
        //    }
        //}

        //private void UpdateL2_Click(object sender, RoutedEventArgs e)
        //{
        //    L2Updater updater = (L2Updater)((Button)sender).Tag;

        //    if (LauncherStatus == LauncherActions.WaitForAction)
        //    {
        //        LauncherStatus = LauncherActions.UpdateFiles;

        //        updaterCancellationTokenSource = new CancellationTokenSource();

        //        SimpleHttpLoader loader = new SimpleHttpLoader();

        //        loader.LoaderProgress += ClientLoadUpdate;
        //        updater.ClientUpdateFinished += ClientUpdateFinished;

        //        try
        //        {
        //            updater.UpdateClient(updaterCancellationTokenSource.Token);
        //        }
        //        finally
        //        {
        //        }
        //    } else if (LauncherStatus == LauncherActions.UpdateFiles)
        //    {
        //        LauncherStatus = LauncherActions.WaitForAction;
        //        InfoBlock.Text = "Обновление остановлено";
        //        InfoBlock.Foreground = Brushes.Black;

        //        updaterCancellationTokenSource.Cancel();
        //    }
        //}

        //#endregion

        //#region Full check tab

        //private void BtnFullCheck_Click(object sender, RoutedEventArgs e)
        //{
        //    if (LauncherStatus == LauncherActions.WaitForAction)
        //    {
        //        LauncherStatus = LauncherActions.CheckFilesFull;

        //        SimpleHttpLoader loader = new SimpleHttpLoader
        //        {
        //            RemoteAddr = new UriBuilder("http", UpdaterConfig.ConfigFields.DownloadAddress.Host, 9000).Uri,
        //            RemoteInfoAddr = new UriBuilder("http", UpdaterConfig.ConfigFields.DownloadAddress.Host, 9001).Uri,
        //        };

        //        updater = new L2Updater(loader, UpdaterConfig);
        //        updater.ClientCheckUpdate += FullCheckActionProgress;
        //        updater.ClientCheckFinished += FullClientCheckFinish;
        //        updater.ClientCheckError += FullClientCheckError;
        //        loader.LoaderProgress += FullClientLoadUpdate;
        //        loader.UnZipProgress += FullLoadFileExtractProgress;

        //        BtnFullCheck.Tag = updater;

        //        fullcheckCancellationTokenSource = new CancellationTokenSource();

        //        try
        //        {
        //            updater.FullLocalClientCheck(fullcheckCancellationTokenSource.Token);
        //        }
        //        finally
        //        {
        //        }
        //    }
        //    else if (LauncherStatus == LauncherActions.CheckFilesFull)
        //    {
        //        fullcheckCancellationTokenSource.Cancel();
        //        LauncherStatus = LauncherActions.WaitForAction;
        //    }
        //}

        //private void BtnFullReload_Click(object sender, RoutedEventArgs e)
        //{
        //    if (LauncherStatus == LauncherActions.WaitForAction)
        //    {
        //        LauncherStatus = LauncherActions.UpdateFilesFull;

        //        SimpleHttpLoader loader = new SimpleHttpLoader
        //        {
        //            RemoteAddr = new UriBuilder("http", UpdaterConfig.ConfigFields.DownloadAddress.Host, 9000).Uri,
        //            RemoteInfoAddr = new UriBuilder("https", UpdaterConfig.ConfigFields.DownloadAddress.Host, 9001).Uri
        //        };

        //        updater = new L2Updater(loader, UpdaterConfig);
        //        updater.ClientCheckUpdate += FullCheckActionProgress;
        //        updater.ClientCheckFinished += FullClientUpdateFinished;
        //        updater.ClientCheckError += FullClientCheckError;
        //        loader.LoaderProgress += FullClientLoadUpdate;
        //        loader.UnZipProgress += FullLoadFileExtractProgress;

        //        BtnFullReload.Tag = updater;

        //        fullcheckCancellationTokenSource = new CancellationTokenSource();

        //        try
        //        {
        //            updater.RewriteClient(fullcheckCancellationTokenSource.Token);
        //        }
        //        finally
        //        {

        //        }
        //    }
        //    else if (LauncherStatus == LauncherActions.UpdateFilesFull)
        //    {
        //        LauncherStatus = LauncherActions.WaitForAction;
        //        fullcheckCancellationTokenSource.Cancel();
        //        FullCheckInfo1.Text = "Перезапись остановлена";
        //        FullCheckInfo1.Foreground = Brushes.Black;
        //    }
        //}

        //private void FullClientUpdateFinished(object sender, EventArgs args)
        //{
        //    L2Updater updater = (L2Updater)sender;
        //    Dispatcher.BeginInvoke((Action)delegate ()
        //    {
        //        FullCheckPercentage.Maximum = 100;
        //        FullCheckPercentage.Value = 100;
        //        FullCheckInfo1.Text = String.Format("Игра обновлена");
        //        FullCheckInfo1.Foreground = Brushes.Green;

        //        BtnFullCheckText.Text = "Полная проверка";
        //        BtnFullReload.IsEnabled = true;

        //        LauncherStatus = LauncherActions.WaitForAction;
        //    });
        //}

        //private void FullClientCheckError(object sender, ClientCheckErrorEventArgs args)
        //{
        //    switch (args.Exeption)
        //    {
        //        case RemoteModelException exr:
        //            MessageBox.Show(String.Format("Не могу соединиться с {0} для получения файла {1}", exr.RemoteAddr, exr.RemoteFile));
        //            break;
        //        case HttpRequestException exhttp:
        //            MessageBox.Show(String.Format("Не могу соединиться с {0}", exhttp.TargetSite));
        //            break;
        //        case Exception ex:
        //            MessageBox.Show(ex.Message);
        //            break;
        //    }
        //    Dispatcher.BeginInvoke((Action)delegate ()
        //    {
        //        LauncherStatus = LauncherActions.WaitForAction;
        //    });
        //}

        //private void FullClientCheckFinish(object sender, ClientCheckFinishEventArgs args)
        //{
        //    L2Updater updater = (L2Updater)sender;

        //    Dispatcher.BeginInvoke((Action)delegate ()
        //    {
        //        BtnFullReload.IsEnabled = true;

        //        FullCheckPercentage.Maximum = 100;
        //        FullCheckPercentage.Value = 1000;
        //        FullCheckInfo1.Text = args.FinishMessage;
        //        FullCheckInfo1.Foreground = args.UpdateRequired ? Brushes.Black : Brushes.Green;

        //        LauncherStatus = LauncherActions.WaitForAction;
        //    });
        //}

        //private void FullCheckActionProgress(object sender, ClientCheckUpdateEventArgs args)
        //{
        //    Dispatcher.BeginInvoke((Action)delegate ()
        //    {
        //        FullCheckPercentage.Maximum = args.ProgressMax;
        //        FullCheckPercentage.Value = args.ProgressValue;
        //        FullCheckInfo1.Text = args.InfoStr;
        //    });
        //}

        //private void FullClientLoadUpdate(object sender, LoaderProgressEventArgs args)
        //{
        //    Dispatcher.BeginInvoke((Action)delegate ()
        //    {
        //        FullCheckPercentage.Maximum = 100;
        //        FullCheckPercentage.Value = args.Percentage * 100.0;
        //    });
        //}

        //private void FullLoadFileExtractProgress(object sender, LoaderUnZipProgressEventArgs args)
        //{
        //    Dispatcher.BeginInvoke((Action)delegate ()
        //    {
        //        FullCheckInfo1.Text = String.Format("Скачиваю файл {0} - {1:F0}% готово", args.FileName, args.Percentage);
        //        FullCheckInfo1.Foreground = Brushes.Black;
        //    });
        //}

        //#endregion

        //#region AboutTab

        //private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        //{
        //    Process runbrowser = new Process();
        //    runbrowser.StartInfo.UseShellExecute = true;
        //    runbrowser.StartInfo.FileName = e.Uri.AbsoluteUri;

        //    runbrowser.Start();

        //    e.Handled = true;
        //}

        //#endregion

        //private void updaterwindow_Closed(object sender, EventArgs e)
        //{
        //}

        //private bool SearchForRunningLineageProcess ()
        //{
        //    /*
        //    Process[] l2processes = Process.GetProcessesByName("l2").Where(p => p. MainModule.FileName.Equals(UpdaterConfig.ClientExeFile)).ToArray();
        //    if (l2processes.Length > 0)
        //        return true;
        //    else
        //        return false;
        //    */
        //    return false;
        //}
    }

}
