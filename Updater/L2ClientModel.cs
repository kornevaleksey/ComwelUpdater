using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using System.Drawing;

namespace Updater
{

    public class ClientModel
    {
        public DateTime Changed { get; set; }
        public uint FilesCount { get; set; }
        public long ClientSize { get; set; }
        public List<ClientFileInfo> FilesInfo { get; set; }
    }

    public class ClientFileInfo
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public DateTime Changed { get; set; }
        public byte[] Hash { get; set; }
        public bool AllowLocalChange { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is ClientFileInfo)
            {
                if ((this.FileName.Equals(((ClientFileInfo)obj).FileName)) &&
                    (this.FileSize == ((ClientFileInfo)obj).FileSize) &&
                    (this.Hash.SequenceEqual(((ClientFileInfo)obj).Hash)))
                    return true;
                else
                    return false;
            } else
                return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator == (ClientFileInfo a, ClientFileInfo b)
        {
            if (a is null)
                return b is null;
            else
                return a.Equals(b);
        }

        public static bool operator != (ClientFileInfo a, ClientFileInfo b)
        {
            if (a is null)
                return !(b is null);
            else
                return !a.Equals(b);
        }
    }

    public abstract class L2ClientBase
    {
        protected static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        
        public ClientModel ClientInfo { get; protected set; }

        public L2ClientBase()
        {
        }

        public async Task WriteClientModel(string filename)
        {
            try
            {
                var serlist = JsonSerializer.Serialize(ClientInfo);
                await File.WriteAllTextAsync(filename, serlist);
                logger.Info(String.Format("Writing model info into file {0}", filename));
            }
            catch (Exception ex)
            {
                logger.Error(ex, String.Format("Writing file {0} error!", filename));
            }
        }

        public async Task ReadClientModel(string filename)
        {
            try
            {
                string deserial = await File.ReadAllTextAsync(filename);
                ClientInfo = JsonSerializer.Deserialize<ClientModel>(deserial);
                logger.Info(String.Format("Reading model info from file {0}", filename));
            }
            catch (Exception ex)
            {
                logger.Error(ex, String.Format("Reading file {0} error!", filename));
            }
        }
    }

    public class L2ClientLocal : L2ClientBase
    {

        public L2ClientLocal() : base()
        {
        }

        public async Task CreateModelFromDirectory (Uri localDir, bool complete=false, EventHandler<UpdaterProgressEventArgs> ProgressUpdate = null)
        {
            List<string> filenames_local;
            if (Directory.Exists(localDir.LocalPath) == true)
                filenames_local = await Task.Run(() => Directory.GetFiles(localDir.LocalPath, "*.*", SearchOption.AllDirectories).ToList());//.Select(fn => Path.GetRelativePath(localDir.LocalPath, fn)).ToList());
            else
                filenames_local = new List<string>();

            ClientInfo = new ClientModel()
            {
                Changed = DateTime.Now,
                FilesCount = (uint)filenames_local.Count,
                ClientSize = 0,
                FilesInfo = new List<ClientFileInfo>()
            };

            FileChecker checker = new FileChecker();

            int progress_counter = 0;

            foreach (string filename in filenames_local)
            {
                ProgressUpdate?.Invoke(this, new UpdaterProgressEventArgs()
                {
                    ProgressMax = filenames_local.Count,
                    ProgressValue = progress_counter++,
                    InfoStr = String.Format("Обработка файла {0}", filename),
                    InfoStrColor = Color.Black
                });

                ClientFileInfo fileinfo = await checker.GetFileInfo(filename, complete);
                fileinfo.FileName = Path.GetRelativePath(localDir.LocalPath, filename);
                ClientInfo.ClientSize += fileinfo.FileSize;
                ClientInfo.FilesInfo.Add(fileinfo);
            }
        }

    }

    public class L2ClientRemote : L2ClientBase
    {
        readonly Loader loader;

        public L2ClientRemote(Loader loader) : base()
        {
            this.loader = loader;
        }

        public async Task LoadRemoteModel(string remoteModelAddr)
        {
            string temporary_file = Path.GetTempFileName();

            try
            {

                if (await loader.DownloadFile(remoteModelAddr, temporary_file))
                {
                    await ReadClientModel(temporary_file);
                }
            }
            finally
            {
                File.Delete(temporary_file);
            }
        }
    }
}
