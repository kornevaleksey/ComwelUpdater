using Serilog;
using Prism.Common;
using Prism.Modularity;
using Prism.Unity;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Launcher.Views;
using Config;

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
            containerRegistry.RegisterSingleton<ILogger>(p =>
            {
                return new LoggerConfiguration().MinimumLevel.Information()
                .WriteTo.File(AppDomain.CurrentDomain.GetData("DataDirectory").ToString() + "/Log-{Date}.txt")
                .CreateLogger();
            });

            containerRegistry.RegisterSingleton<UpdaterConfig>();


        }
    }
}
