using ConfigUpdater;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Config
{
    class L2Updater : IConfigUpdater
    {
        public Dictionary<string, string> Config { get; }
        public string FileName { get; set; }

        private string config_filename;

        public Task<bool> ReadAsync()
        {
            JsonSerializer jsonSerializer = new JsonSerializer();
            jsonSerializer.Deserialize()
            JsonReader reader = new JsonReader();
        }
        public Task<bool> WriteAsync()
        {

        }
        public void SetDefault()
        {

        }
    }

    class L2ConfigFile
    {
        public string Language;
        public string NewsAddress;
        public string DownloadAddress;
        public string ClientFolder;
        public string ClientExecutable;
    }
}

