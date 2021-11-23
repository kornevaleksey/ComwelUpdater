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
        private string version = Assembly.GetEntryAssembly().GetName().Version.ToString();
        private string buildDate = Properties.Resources.BuildDate.Trim();

        public AboutViewModel()
        {
            VersionInfo = $"Версия {version} от {buildDate} © Korall";
        }

        public string VersionInfo { get; }
    }
}
