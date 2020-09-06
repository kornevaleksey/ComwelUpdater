using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using System.Drawing;
using System.Threading;

namespace Updater
{

    public class ClientModel
    {
        public DateTime Changed { get; set; }
        public uint FilesCount { get; set; }
        public long ClientSize { get; set; }
        public long ClientCompressedSize { get; set; }
        public List<ClientFileInfo> FilesInfo { get; set; }
    }

    public class ClientFileInfo
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public long FileSizeCompressed { get; set; }
        public DateTime Changed { get; set; }
        public byte[] Hash { get; set; }
        public bool AllowLocalChange { get; set; }
        public bool ImportantFile { get; set; }

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

        public FileChecker Checker { get; }
        public ClientModel ClientInfo { get; protected set; }

        public L2ClientBase()
        {
            Checker = new FileChecker();
        }

        public async Task WriteClientModel(string filename)
        {
            logger.Info(String.Format("Read client model from file {0}", filename));

            var serlist = JsonSerializer.Serialize(ClientInfo);
            await File.WriteAllTextAsync(filename, serlist);

            logger.Info(String.Format("Finish writing model info file {0}", filename));
        }

        public async Task ReadClientModel(string filename)
        {
            logger.Info("Read client model from file {0}", filename);

            if (File.Exists(filename) == false) throw new FileNotFoundException("File not found", filename);
            string deserial = await File.ReadAllTextAsync(filename);
            ClientInfo = JsonSerializer.Deserialize<ClientModel>(deserial);

            logger.Info(String.Format("Finish read client model from file {0}", filename));
        }
    }

    public class L2ClientLocal : L2ClientBase
    {

        public L2ClientLocal() : base()
        {
        }

        public async Task CreateModelFromDirectory (Uri localDir, CancellationToken token)
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

            ClientInfo.FilesInfo = await Checker.GetFilesListInfo(filenames_local, localDir.LocalPath, token);
            ClientInfo.ClientSize += ClientInfo.FilesInfo.Sum( ci => ci.FileSize);
        }

        public async Task CreateModelFromDirectory(Uri localDir, CancellationToken token, bool complete = false)
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

            ClientInfo.FilesInfo = await Checker.GetFilesListInfo(filenames_local, localDir.LocalPath, token, complete);
            ClientInfo.ClientSize += ClientInfo.FilesInfo.Sum(ci => ci.FileSize);
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
                await loader.DownloadFile(remoteModelAddr, temporary_file);
                await ReadClientModel(temporary_file);
            }
            finally
            {
                File.Delete(temporary_file);
            }
        }
    }

    public class RemoteModelException : Exception
    {
        public Uri RemoteAddr { get; set; }
        public string RemoteFile { get; set; }
    }
}
