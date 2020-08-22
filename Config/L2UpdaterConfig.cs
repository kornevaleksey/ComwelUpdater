using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.IO;
using Logger;

namespace Config
{
    public class L2UpdaterConfig
    {
        public string FileName { get; }

        public Dictionary<string, string> ConfigParameters { get; set; }

        private CommonLogger logger;

        public L2UpdaterConfig(CommonLogger logger)
        {
            this.logger = logger;

            ConfigParameters = new Dictionary<string, string>();

            FileName = AppDomain.CurrentDomain.BaseDirectory + "\\l2updater.json";
            logger.Log("Select config file " + FileName);

            if (File.Exists(FileName))
            {
                Read();

            } else
            {
                logger.Log("Config file doesn't exist, creating new");
                SetDefault();
                Write();
            }
        }

        public bool Read()
        {
            logger.Log("Start loading config file");
            try
            {
                string configString = File.ReadAllText(FileName);
                ConfigParameters = JsonSerializer.Deserialize<Dictionary<string, string>>(configString);
                foreach (var item in ConfigParameters)
                {
                    logger.Log("Reading key "+item.Key+" with value "+item.Value);
                }
            }
            catch (Exception ex)
            {
                logger.Log("Error loading config file "+ex.Message);
                return false;
            }

            logger.Log("Config file loaded");
            return true;
        }

        public bool Write()
        {
            logger.Log("Start writing config file");
            try
            {
                string configString = JsonSerializer.Serialize<Dictionary<string, string>>(ConfigParameters);
                File.WriteAllText(FileName, configString);
                logger.Log("Writing text in config file: " + configString);
            }
            catch (Exception ex)
            {
                logger.Log("Error writing config file " + ex.Message);
                return false;
            }
            logger.Log("Config file writed");
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

