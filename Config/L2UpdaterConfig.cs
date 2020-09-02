using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace Config
{
    public class L2UpdaterConfig
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public string FileName { get; }

        public string RemoteInfoFile { get => "info//clientinfo.inf"; }
        public string LocalInfoFile { get => LocalWorkingFolder + "//clientinfo.inf"; }
        public string ClientExeFile { get => ConfigFields.ClientFolder.LocalPath+"//system//l2.exe"; }
        public string RemoteClientPath { get => "client/"; }

        private string LocalWorkingFolder { get => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\ComwelUpdater"; }

        

        public class Fields
        {
            public Uri DownloadAddress { get; set; }
            public Uri ClientFolder { get; set; }
        }

        public Fields ConfigFields;

        public L2UpdaterConfig()
        {
            ConfigFields = new Fields();

            if (!Directory.Exists(LocalWorkingFolder))
                Directory.CreateDirectory(LocalWorkingFolder);

            FileName = LocalWorkingFolder + "\\l2updater.json";
            logger.Info("Select config file " + FileName);
        }

        public async Task<bool> Read()
        {
            logger.Info("Start loading config file");
            try
            {
                string configString = await File.ReadAllTextAsync(FileName);
                ConfigFields = JsonSerializer.Deserialize<Fields>(configString);
                logger.Info("Reading config fields");
            }
            catch (Exception ex)
            {
                logger.Info("Error loading config file "+ex.Message);
                return false;
            }

            logger.Info("Config file loaded");
            return true;
        }

        public async Task<bool> Write()
        {
            logger.Info("Start writing config file");
            try
            {
                string configString = JsonSerializer.Serialize<Fields>(ConfigFields);
                await File.WriteAllTextAsync(FileName, configString);
                logger.Info("Writing text in config file: " + configString);
            }
            catch (Exception ex)
            {
                logger.Info("Error writing config file " + ex.Message);
                return false;
            }
            logger.Info("Config file writed");
            return true;
        }

    }


}

