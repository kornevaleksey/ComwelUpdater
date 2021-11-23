using Launcher.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Text;

namespace Launcher.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        public MainWindowViewModel()
        {
            CloseCommand = new DelegateCommand<ICloseable>(Close);
            MinimizeCommand = new DelegateCommand<IWindowState>(Minimize);
        }

        public DelegateCommand<ICloseable> CloseCommand { get; private set; }
        public DelegateCommand<IWindowState> MinimizeCommand { get; private set; }

        private void Close(ICloseable window)
        {
            window.Close();
        }

        private void Minimize(IWindowState windowState)
        {
            windowState.WindowState = System.Windows.WindowState.Minimized;
        }
    }
}
