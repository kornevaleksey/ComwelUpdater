using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            GoToTheSiteCommand = new DelegateCommand(GoToTheSite);
            OpenLicenseCommand = new DelegateCommand(OpenLicense);
        }

        public string VersionInfo => $"Версия {version} от {buildDate} © Korall";
        public Uri SiteAddress { get; } = new Uri("http://l2-update.gudilap.ru");
        public Uri LicenseAddress { get; } = new Uri("http://l2-update.gudilap.ru/license.txt");

        public DelegateCommand GoToTheSiteCommand { get; }
        public DelegateCommand OpenLicenseCommand { get; }

        public void GoToTheSite()
        {
            Process runbrowser = new Process();
            runbrowser.StartInfo.UseShellExecute = true;
            runbrowser.StartInfo.FileName = SiteAddress.AbsoluteUri;

            runbrowser.Start();
        }

        public void OpenLicense()
        {
            Process runbrowser = new Process();
            runbrowser.StartInfo.UseShellExecute = true;
            runbrowser.StartInfo.FileName = LicenseAddress.AbsoluteUri;

            runbrowser.Start();
        }
    }
}
