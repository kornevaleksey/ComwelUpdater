using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Config
{
    public class UpdaterConfig
    {
        private string LocalWorkingFolder 
        { 
            get => 
                ConfigFields.PlacedInClientFolder? 
                ConfigFields.ClientFolder.LocalPath:
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\ComwelUpdater\\"; 
        }

        public class Fields
        {
            public Uri DownloadAddress { get; set; } = new Uri("http://l2-update.gudilap.ru");
            public Uri ClientFolder { 
                get => PlacedInClientFolder ? new Uri(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)) : _clientFolder;
                set { _clientFolder = value; }
            }
            public bool PlacedInClientFolder { get; set; } = false;

            private Uri _clientFolder = new Uri ("c:\\Lineage2");
        }

        public Fields ConfigFields { get; private set; }



        private readonly ILogger logger;

        public UpdaterConfig(ILogger<UpdaterConfig> logger)
        {
            this.logger = logger;

            ConfigFields = new Fields
            {
                PlacedInClientFolder = true
            };

            //Check local config placement
            if (File.Exists(ConfigFile))
            {
                logger.LogInformation("Select config file " + ConfigFile);
            } else
            {
                ConfigFields.PlacedInClientFolder = false;
                if (!Directory.Exists(LocalWorkingFolder))
                    Directory.CreateDirectory(LocalWorkingFolder);
            }

//            string ss = Assembly.GetEntryAssembly().CodeBase;
        }



        public string RemoteInfoFile { get => "clientinfo.inf"; }
        public string LocalInfoFile { get => LocalWorkingFolder + "clientinfo.inf"; }
        public string ClientExeFile { get => ConfigFields.ClientFolder.LocalPath + "\\system\\l2.exe"; }
        public string RemoteClientPath { get => ""; }

        public string ConfigFile { get => LocalWorkingFolder + "l2updater.json"; }

        public async void Read()
        {
            logger.LogInformation("Start loading config file");
            try
            {
                string configString = await File.ReadAllTextAsync(ConfigFile);
                ConfigFields = JsonSerializer.Deserialize<Fields>(configString);
                logger.LogInformation("Reading config fields");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading config file");
                throw;
            }

            logger.LogInformation("Config file loaded");
        }

        public async void Write()
        {
            logger.LogInformation("Start writing config file");
            try
            {
                string configString = JsonSerializer.Serialize<Fields>(ConfigFields);
                await File.WriteAllTextAsync(ConfigFile, configString);
                logger.LogInformation("Writing text in config file: " + configString);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error writing config file");
                throw;
            }

            logger.LogInformation("Config file writed");
        }
    }

    public class ConfigRWErrorEventArgs : EventArgs
    {
        public string FileName;
        public Exception ReadException;
    }


}

