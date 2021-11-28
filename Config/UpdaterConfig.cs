using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Config
{
    public class UpdaterConfig
    {
        private Uri _localDirectory = new Uri(@"c:\Lineage2");

        public Uri LocalDirectory
        {
            get => PlacedInLocalDirectory ? new Uri(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)) : _localDirectory;
            set { _localDirectory = value; }
        }

        public string ConfigDirectory
        {
            get =>
                PlacedInLocalDirectory ?
                LocalDirectory.LocalPath :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ComwelUpdater");
        }

        public bool PlacedInLocalDirectory { get; set; } = false;

        public Uri RemoteClientPath { get; set; } = new Uri($"http://l2-update.gudilap.ru");
        public string RemoteInfoFile { get => "clientinfo.inf"; }
        public string LocalInfoFile { get => Path.Combine(ConfigDirectory, "clientinfo.inf"); }
        public string ClientExeFile { get => Path.Combine(LocalDirectory.LocalPath, "system", "l2.exe"); }
    }
}
