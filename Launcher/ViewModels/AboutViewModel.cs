using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Launcher.ViewModels
{
    public class AboutViewModel
    {
        private readonly string version;
        private readonly string buildDate;

        public AboutViewModel()
        {
            var assemblyVersion = Assembly.GetEntryAssembly()?.GetName()?.Version;
            version = assemblyVersion != null ? assemblyVersion.ToString() : "0.0.0.0";
            buildDate = Properties.Resources.BuildDate.Trim();
        }

        public string VersionInfo => $"Версия {version} от {buildDate} © Korall";
    }
}
