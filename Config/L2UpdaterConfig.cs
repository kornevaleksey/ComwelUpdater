using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.IO;

namespace Config
{
    public class L2UpdaterConfig
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public string FileName { get; }

        public Dictionary<string, string> ConfigParameters { get; set; }

        public L2UpdaterConfig()
        {
            ConfigParameters = new Dictionary<string, string>();

            FileName = AppDomain.CurrentDomain.BaseDirectory + "\\l2updater.json";
            logger.Info("Select config file " + FileName);

            if (File.Exists(FileName))
            {
                Read();

            } else
            {
                logger.Info("Config file doesn't exist, creating new");
                SetDefault();
                Write();
            }
        }

        public bool Read()
        {
            logger.Info("Start loading config file");
            try
            {
                string configString = File.ReadAllText(FileName);
                ConfigParameters = JsonSerializer.Deserialize<Dictionary<string, string>>(configString);
                foreach (var item in ConfigParameters)
                {
                    logger.Info("Reading key "+item.Key+" with value "+item.Value);
                }
            }
            catch (Exception ex)
            {
                logger.Info("Error loading config file "+ex.Message);
                return false;
            }

            logger.Info("Config file loaded");
            return true;
        }

        public bool Write()
        {
            logger.Info("Start writing config file");
            try
            {
                string configString = JsonSerializer.Serialize<Dictionary<string, string>>(ConfigParameters);
                File.WriteAllText(FileName, configString);
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
        public void SetDefault()
        {
            ConfigParameters.Add("DownloadAddress", "http:\\\\localhost");
            ConfigParameters.Add("DownloadPort", "9000");
            ConfigParameters.Add("DownloadMaxSpeed", "1000");
            ConfigParameters.Add("ClientFolder", "c:\\Lineage2");
        }

    }


}

