using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Config;
using Microsoft.WindowsAPICodePack.Dialogs;
using Serilog;

namespace Launcher
{
    /// <summary>
    /// Логика взаимодействия для ConfigWindow.xaml
    /// </summary>

    public partial class ConfigWindow : Window
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        L2UpdaterConfig cfg;

        public ConfigWindow(L2UpdaterConfig cfg)
        {
            InitializeComponent();

            this.cfg = cfg;

            foreach (TextBox textBox in LogicalTreeHelper.GetChildren(configGrid).OfType<TextBox>() )
            {
                textBox.Text = cfg.ConfigParameters[(string)textBox.Tag];
                textBox.TextChanged += ParameterChanged;
            }    
        }

        private void ParameterChanged(object sender, TextChangedEventArgs e)
        {
            cfg.ConfigParameters[(string)((TextBox)sender).Tag] = ((TextBox)sender).Text;
        }

        private void SelectClick(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog commonOpenFileDialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                InitialDirectory = cfg.ConfigParameters[(string)clientdestTextBox.Tag]
            };

            if (commonOpenFileDialog.ShowDialog()==CommonFileDialogResult.Ok)
            {
                cfg.ConfigParameters[(string)clientdestTextBox.Tag] = commonOpenFileDialog.FileName;
                clientdestTextBox.Text = commonOpenFileDialog.FileName;
            }
        }

        private void ConfigClosed(object sender, EventArgs e)
        {
            foreach (TextBox textBox in LogicalTreeHelper.GetChildren(configGrid).OfType<TextBox>())
            {
                cfg.ConfigParameters[(string)textBox.Tag] = textBox.Text;
            }

            cfg.Write();
        }
    }
}
