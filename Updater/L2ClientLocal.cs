using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace Updater
{
    
    public class L2ClientLocal:L2ClientBase
    {

        public bool ClientisRunnable { get => new FileInfo(Folder + "//system/l2.exe").Exists; }

        protected List<ClientFileInfo> LocalFiles;
        public L2ClientLocal(string ClientFolder, string HashesFile):base (new Uri(ClientFolder), new Uri(HashesFile))
        {
            ShadowHashesFile = HashesFile;
        }

        public override async Task<bool> PrepareInfo()
        {
            List<string> filenames_local;
            List<ClientFileInfo> info_from_hashes_file;

            if (Directory.Exists(Folder.LocalPath) == true)
                filenames_local = await Task.Run(() => Directory.GetFiles(Folder.LocalPath, "*.*", SearchOption.AllDirectories).Select(fn => Path.GetRelativePath(Folder.LocalPath, fn)).ToList());
            else
                filenames_local = new List<string>();

            if (File.Exists(ShadowHashesFile))
                info_from_hashes_file = await ReadFilesInfo(ShadowHashesFile);
            else
                info_from_hashes_file = new List<ClientFileInfo>();

            LocalFiles = new List<ClientFileInfo>();
            foreach (string filename in filenames_local)
            {
                ClientFileInfo addcfi = new ClientFileInfo()
                {
                    FileName = filename,
                    FileSize = new FileInfo(filename).Length
                };
                   
                ClientFileInfo fileInfo = info_from_hashes_file.Find(cfi => cfi.FileName == filename);
                if (fileInfo != null)
                {
                    addcfi.Hash = new byte[fileInfo.Hash.Length];
                    fileInfo.Hash.CopyTo(addcfi.Hash, 0);
                    addcfi.AllowLocalChange = fileInfo.AllowLocalChange;
                } else
                {
                    addcfi.AllowLocalChange = false;
                    addcfi.Hash = new byte[32];
                }

                LocalFiles.Add(addcfi);
            }

            return true;
        }

    }
}
