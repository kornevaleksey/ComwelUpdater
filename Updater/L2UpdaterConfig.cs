using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace Config
{
    public class L2UpdaterConfig
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public string RemoteInfoFile { get => "info/clientinfo.inf"; }
        public string LocalInfoFile { get => LocalWorkingFolder + "clientinfo.inf"; }
        public string ClientExeFile { get => ConfigFields.ClientFolder.LocalPath+"\\system\\l2.exe"; }
        public string RemoteClientPath { get => "client/"; }

        public string ConfigFile { get => LocalWorkingFolder + "l2updater.json"; }
        private string LocalWorkingFolder { get => ConfigFields.PlacedInClientFolder? ConfigFields.ClientFolder.LocalPath:Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\ComwelUpdater\\"; }

        public class Fields
        {
            public Uri DownloadAddress { get; set; } = new Uri("http://l2-update.gudilap.ru");
            public Uri ClientFolder { 
                get => PlacedInClientFolder ? new Uri(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)) : _ClientFolder;
                set { _ClientFolder = value; }
            }
            public bool PlacedInClientFolder { get; set; } = false;

            private Uri _ClientFolder = new Uri ("c:\\Lineage2");
        }

        public Fields ConfigFields { get; private set; }

        public EventHandler ConfigFinishedRead;
        public EventHandler ConfigFinishedWrite;
        public EventHandler<ConfigRWErrorEventArgs> ConfigReadError;
        public EventHandler<ConfigRWErrorEventArgs> ConfigWriteError;

        public L2UpdaterConfig()
        {
            ConfigFields = new Fields
            {
                PlacedInClientFolder = true
            };

            //Check local config placement
            if (File.Exists(ConfigFile))
            {
                logger.Info("Select config file " + ConfigFile);
            } else
            {
                ConfigFields.PlacedInClientFolder = false;
                if (!Directory.Exists(LocalWorkingFolder))
                    Directory.CreateDirectory(LocalWorkingFolder);
            }

//            string ss = Assembly.GetEntryAssembly().CodeBase;
        }

        public async void Read()
        {
            logger.Info("Start loading config file");
            try
            {
                string configString = await File.ReadAllTextAsync(ConfigFile);
                ConfigFields = JsonSerializer.Deserialize<Fields>(configString);
                logger.Info("Reading config fields");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error loading config file");
                ConfigReadError?.Invoke(this, new ConfigRWErrorEventArgs()
                {
                    FileName = ConfigFile,
                    ReadException = ex
                } );
                return;
            }

            logger.Info("Config file loaded");

            ConfigFinishedRead?.Invoke(this, new EventArgs());
        }

        public async void Write()
        {
            logger.Info("Start writing config file");
            try
            {
                string configString = JsonSerializer.Serialize<Fields>(ConfigFields);
                await File.WriteAllTextAsync(ConfigFile, configString);
                logger.Info("Writing text in config file: " + configString);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error writing config file");
                ConfigWriteError?.Invoke(this, new ConfigRWErrorEventArgs()
                {
                    FileName = ConfigFile,
                    ReadException = ex
                });
                return;
            }

            logger.Info("Config file writed");

            ConfigFinishedWrite?.Invoke(this, new EventArgs());
        }
    }

    public class ConfigRWErrorEventArgs : EventArgs
    {
        public string FileName;
        public Exception ReadException;
    }


}

