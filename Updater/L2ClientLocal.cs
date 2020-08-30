using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace Updater
{
    
    class L2ClientLocal:L2ClientBase
    {
        protected List<ClientFileInfo> LocalFiles;
        public L2ClientLocal(string ClientFolder, string HashesFile):base (new Uri(ClientFolder), new Uri(HashesFile))
        {
            ShadowHashesFile = HashesFile;
        }

        public override async void PrepareInfo()
        {
            List<string> filenames_local = await Task.Run(() => Directory.GetFiles(Folder.LocalPath, "*.*", SearchOption.AllDirectories).Select(fn => Path.GetRelativePath(Folder.LocalPath, fn)).ToList());
            List<ClientFileInfo> info_from_hashes_file = await ReadFilesInfo(ShadowHashesFile);

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
                    addcfi.Hash = fileInfo.Hash;
                    addcfi.AllowLocalChange = fileInfo.AllowLocalChange;
                }

                LocalFiles.Add(addcfi);
            }
        }
    }
}
