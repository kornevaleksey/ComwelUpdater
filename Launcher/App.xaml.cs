using Config;
using DryIoc;
using Launcher.Views;
using Microsoft.Extensions.Logging;
using Prism.Common;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using Prism.Regions;
using Serilog;
using Serilog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Updater;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindowView>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<ILoggerProvider, SerilogLoggerProvider>();
            containerRegistry.RegisterSingleton<ILoggerFactory, LoggerFactory>();
            containerRegistry.RegisterSingleton(typeof(ILogger<>), typeof(Logger<>));

            containerRegistry.RegisterSingleton<FileChecker>();
            containerRegistry.RegisterSingleton<SimpleHttpLoader>();
            containerRegistry.RegisterSingleton<GameUpdater>();
            containerRegistry.RegisterSingleton<UpdaterConfig>();

            //Prism
            containerRegistry.RegisterForNavigation<MainWindowView>();
            containerRegistry.RegisterForNavigation<UpdateView>("Update");
            containerRegistry.RegisterForNavigation<SettingsView>("Settings");
            containerRegistry.RegisterForNavigation<FullCheckView>("FullCheck");
            containerRegistry.RegisterForNavigation<NavigatePanelView>("NavigatePanel");
            containerRegistry.RegisterForNavigation<AboutView>("About");
        }

        protected override void InitializeModules()
        {
            base.InitializeModules();

            var _regionManager = Container.Resolve<IRegionManager>();
            _regionManager.RequestNavigate("NavigateRegion", "NavigatePanel");
            _regionManager.RequestNavigate("ContentRegion", "Update");
        }
    }
}
