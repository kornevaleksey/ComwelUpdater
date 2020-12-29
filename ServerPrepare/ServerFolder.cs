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
using SharpCompress;

namespace ServerPrepare.Process
{
    public class ServerFolder : FolderConfig
    {
        public ClientModel ClientInfo { get; private set; }

        public Action<double, string> CreateInfoProgress;
        public Action FinishInfoProgress;

        public override string InfoFile { get => Path.Combine(InfoFolder, "clientinfo.inf"); }
        public string PatchFolder { get => Path.Combine(Folder.LocalPath, "patches"); }

        public ServerFolder(Uri Folder) : base(Folder)
        {
        }

        public async void StartModel(SourceFolder sourcefolder)
        {
            List<string> sourcefiles = Directory.GetFiles(sourcefolder.ClientFolder, "*.*", SearchOption.AllDirectories).ToList();
            List<string> serverfiles = Directory.GetFiles(this.ClientFolder, "*.*", SearchOption.AllDirectories).ToList();
            long compress_size = serverfiles.Select(of => new FileInfo(of).Length).Sum();

            ClientModel ClientInfo = new ClientModel()
            {
                Changed = DateTime.Now,
                FilesCount = (uint)sourcefiles.Count,
            };

            ClientInfo.FilesInfo = MakeFilesInfo(sourcefiles, sourcefolder);

            ClientInfo.ClientSize = ClientInfo.FilesInfo.Sum(ci => ci.FileSize);
            ClientInfo.ClientCompressedSize = compress_size;

            var serlist = JsonSerializer.Serialize(ClientInfo, new JsonSerializerOptions() { WriteIndented = true });
            await File.WriteAllTextAsync(this.InfoFile, serlist);

            await CompressFileAsync(this.InfoFile, this.InfoFile);
        }

        public async void ReadModel ()
        {
            if (File.Exists(this.InfoFile))
            {
                string modeltext = await File.ReadAllTextAsync(this.InfoFile);
                ClientInfo = JsonSerializer.Deserialize<ClientModel>(modeltext);
            }
        }

        public async void CreatePatch(SourceFolder sourcefolder)
        {
            List<SourceFileInfo> patch = new List<SourceFileInfo>();

            foreach (SourceFileInfo info in sourcefolder.FileInfos)
            {
                ClientFileInfo serverinfo = ClientInfo.FilesInfo.Find(si => info.FileName.Equals(si.FileName, StringComparison.InvariantCultureIgnoreCase));
                if (serverinfo!=null)
                {
                    if (serverinfo.Hash.SequenceEqual(info.Hash) == false)
                        patch.Add(info);
                } else
                {
                    patch.Add(info);
                }
            }

            if (patch.Count>0)
            {
                string patchname = DateTime.Now.ToString("yyyyMMdd_hhmmss");
                DirectoryInfo dirinfo =  Directory.CreateDirectory(Path.Combine(PatchFolder, patchname));
                var directories_list = patch.Select(p => Path.GetDirectoryName(Path.Combine(dirinfo.FullName, p.FileName))).Distinct().ToList();
                directories_list.ForEach(dir => Directory.CreateDirectory(dir));
                await sourcefolder.CompressFilesList(patch, Path.Combine(PatchFolder, patchname));
            }

        }

        private List<ClientFileInfo> MakeFilesInfo(List<string> filenames, SourceFolder sourcefolder)
        {
            List<ClientFileInfo> res = new List<ClientFileInfo>();

            int progress_counter = 0;

            foreach (string filename in filenames)
            {
                CreateInfoProgress?.Invoke(100.0* progress_counter / filenames.Count, String.Format("Обработка файла {0}", filename));

                string relative_filename = Path.GetRelativePath(sourcefolder.ClientFolder, filename);

                SourceFileInfo sourceinfo = sourcefolder.FileInfos.Find(s => s.FileName.Equals(relative_filename, StringComparison.InvariantCultureIgnoreCase));

                ClientFileInfo fileinfo = new ClientFileInfo()
                {
                    FileName = relative_filename,
                    FileSize = new FileInfo(filename).Length,
                    FileSizeCompressed = new FileInfo(Path.Combine(this.ClientFolder, relative_filename + ".zip")).Length,
                    Changed = new FileInfo(filename).LastWriteTimeUtc,
                    Hash = sourceinfo.Hash,
                    AllowLocalChange = sourceinfo.UserChangeAllow,
                    ImportantFile = sourceinfo.Important
                };

                res.Add(fileinfo);

                progress_counter++;
            }

            return res;
        }

    }
}
