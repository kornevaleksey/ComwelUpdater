using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

namespace Updater
{
    class L2ClientBase
    {
        protected static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public Uri Folder { get; set; }
        public Uri HashesFile { get; set; }

        protected string ShadowHashesFile;
        protected List<ClientFileInfo> FilesInfo;

        public L2ClientBase(Uri Folder, Uri HashesFile)
        {
            this.Folder = Folder;
            this.HashesFile = HashesFile;
        }

        public virtual void PrepareInfo()
        {
        }

        protected async Task<bool> WriteFilesInfo(List<ClientFileInfo> clientFileInfos, string filename)
        {
            var serlist = JsonSerializer.Serialize(clientFileInfos);
            await File.WriteAllTextAsync(filename, serlist);
            return true;
        }

        protected async Task<List<ClientFileInfo>> ReadFilesInfo(string filename)
        {
            try
            {
                string deserial = await File.ReadAllTextAsync(filename);
                return JsonSerializer.Deserialize<List<ClientFileInfo>>(deserial);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Read files info error!");
                return null;
            }
        }
    }
}
