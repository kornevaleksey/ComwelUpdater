using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Updater;
using ServerPrepare.FilesInfo;

namespace ServerPrepare.Process
{
    public class ServerFolder : FolderConfig
    {
        readonly SHA256 sha256 = SHA256.Create();

        public Action<double, string> CreateInfoProgress;
        public Action FinishInfoProgress;

        public override string InfoFile { get => Path.Combine(InfoFolder, "clientinfo.inf"); }

        public ServerFolder(Uri Folder) : base(Folder)
        {
        }

        public async void StartModel(SourceFolder sourcefolder)
        {
            List<string> sourcefiles = Directory.GetFiles(sourcefolder.ClientFolder, "*.*", SearchOption.AllDirectories).ToList();
            List<string> serverfiles = Directory.GetFiles(this.ClientFolder, "*.*", SearchOption.AllDirectories).ToList();
            long compress_size = serverfiles.Select(of => new FileInfo(of).Length).Sum();

            List<ClientFileInfo> clientinfo = await MakeFilesInfo(sourcefiles, sourcefolder.ClientFolder, this.ClientFolder);

            ClientModel ClientInfo = new ClientModel()
            {
                Changed = DateTime.Now,
                FilesCount = (uint)sourcefiles.Count,
            };

            ClientInfo.FilesInfo = clientinfo;
            ClientInfo.ClientSize = ClientInfo.FilesInfo.Sum(ci => ci.FileSize);
            ClientInfo.ClientCompressedSize = compress_size;

            var serlist = JsonSerializer.Serialize(ClientInfo, new JsonSerializerOptions() { WriteIndented = true });
            await File.WriteAllTextAsync(this.InfoFile, serlist);
        }

        private async Task<List<ClientFileInfo>> MakeFilesInfo(List<string> filenames, string sourcepath, string compressedpath)
        {
            List<ClientFileInfo> res = new List<ClientFileInfo>();

            int progress_counter = 0;

            foreach (string filename in filenames)
            {
                CreateInfoProgress?.Invoke(100.0* progress_counter / filenames.Count, String.Format("Обработка файла {0}", filename));
                
                ClientFileInfo fileinfo = await GetFileInfo(filename, sourcepath, compressedpath);
                fileinfo.FileName = Path.GetRelativePath(sourcepath, filename);
                res.Add(fileinfo);

                progress_counter++;
            }

            return res;
        }

        private async Task<ClientFileInfo> GetFileInfo(string filename, string sourcepath, string compressedpath)
        {
            ClientFileInfo clientFileInfo = new ClientFileInfo
            {
                FileName = filename,
                AllowLocalChange = false,
                FileSize = new FileInfo(filename).Length,
                FileSizeCompressed = new FileInfo(Path.Combine(compressedpath, Path.GetRelativePath(sourcepath, filename)) + ".zip").Length,
                Changed = new FileInfo(filename).LastWriteTimeUtc
            };

            byte[] hash = await Task.Run(() => sha256.ComputeHash(File.OpenRead(filename)));
            clientFileInfo.Hash = new byte[hash.Length];
            hash.CopyTo(clientFileInfo.Hash, 0);

            return clientFileInfo;
        }
    }
}
