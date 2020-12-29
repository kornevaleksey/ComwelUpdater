using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ServerPrepare.Configuration
{
    class PrepareSettings
    {
        public Uri SourceFolder { get; set; }
        public Uri ServerFolder { get; set; }

        Uri SettingsFile { get => new Uri (Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\ComwelUpdaterPrepare\settings.json"); }

        public async void SaveSettings ()
        {
            if (!Directory.Exists(Path.GetDirectoryName(SettingsFile.LocalPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile.LocalPath));

            string settingsjson = JsonSerializer.Serialize(this, new JsonSerializerOptions() { WriteIndented = true });

            await File.WriteAllTextAsync(SettingsFile.LocalPath, settingsjson);
        }

        public async void ReadSettings()
        {
            if ((!Directory.Exists(Path.GetDirectoryName(SettingsFile.LocalPath))) ||
                 (!File.Exists(SettingsFile.LocalPath)) )
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile.LocalPath));
                this.SourceFolder = new Uri("c:\\");
                this.ServerFolder = new Uri("c:\\");

                SaveSettings();
            } else
            {
                string settingsjson = await File.ReadAllTextAsync(SettingsFile.LocalPath);
                PrepareSettings p = JsonSerializer.Deserialize<PrepareSettings>(settingsjson);
                this.ServerFolder = p.ServerFolder;
                this.SourceFolder = p.SourceFolder;
            }
        }
    }


}
