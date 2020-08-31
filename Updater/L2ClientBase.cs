using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Drawing;

namespace Updater
{
    public abstract class L2ClientBase
    {
        protected static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public Uri Folder { get; set; }
        public Uri HashesFile { get; set; }

        public event EventHandler<UpdaterProgressEventArgs> ProgressUpdate;

        protected string ShadowHashesFile;
        public List<ClientFileInfo> FilesInfo { get; protected set; }

        public L2ClientBase(Uri Folder, Uri HashesFile)
        {
            this.Folder = Folder;
            this.HashesFile = HashesFile;
        }

        public abstract Task<bool> PrepareInfo();


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

    public class ClientFileInfo
    {
        public string FileName { get; set; }
        public byte[] Hash { get; set; }
        public long FileSize { get; set; }
        public bool AllowLocalChange { get; set; }
    }

    public class UpdaterProgressEventArgs : EventArgs
    {
        public long ProgressMax { get; set; }
        public long ProgressValue { get; set; }
        public string InfoStr { get; set; }
        public Color InfoStrColor { get; set; }
    }
}
