using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Launcher.ViewModels
{
    public class NavigatePanelViewModel : BindableBase
    {
        private readonly IRegionManager regionManager;

        public NavigatePanelViewModel(IRegionManager regionManager)
        {
            this.regionManager = regionManager;

            AboutCommand = new DelegateCommand(NavigateToAbout);
            UpdaterCommand = new DelegateCommand(NavigateToUpdater);
            SettingsCommand = new DelegateCommand(NavigateToSettings);
        }

        public DelegateCommand AboutCommand { get; private set; }
        public DelegateCommand UpdaterCommand { get; private set; }
        public DelegateCommand SettingsCommand { get; private set; }

        public void NavigateToUpdater()
        {
            regionManager.RequestNavigate("ContentRegion", "Update");
        }

        public void NavigateToSettings()
        {
            regionManager.RequestNavigate("ContentRegion", "Settings");
        }

        public void NavigateToAbout()
        {
            regionManager.RequestNavigate("ContentRegion", "About");
        }
    }
}
