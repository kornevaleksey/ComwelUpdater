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
using CommonwealthUpdater.Config;

namespace CommonwealthUpdater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismAppication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
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
