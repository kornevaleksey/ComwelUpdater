using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Updater
{
    public class L2ClientRemote:L2ClientBase
    {
        readonly Loader loader;

        public L2ClientRemote(Uri RemoteAddr, Loader loader) :base (RemoteAddr, new Uri(RemoteAddr,"info/clientinfo.inf"))
        {
            this.loader = loader;
            this.loader.RemoteAddr = RemoteAddr;
        }

        public override async Task<bool> PrepareInfo()
        {
            logger.Info("Start prepare remote client model");
            ShadowHashesFile = Path.GetTempFileName();
            if (await loader.DownloadFile(HashesFile.ToString(), ShadowHashesFile) == true)
            {
                logger.Info("Load client files info");
                FilesInfo = await ReadFilesInfo(ShadowHashesFile);
                logger.Info("Read client files info");
                return true;
            }
            else
            {
                logger.Info("Failed to load client files info");
                FilesInfo = new List<ClientFileInfo>();
                return false;
            }

        }

        public List<ClientFileInfo> CompareToLocal(L2ClientLocal localclient)
        {
            List<ClientFileInfo> difference_list = new List<ClientFileInfo>();

            foreach (ClientFileInfo clientFileInfo in FilesInfo)
            {
                ClientFileInfo localfile = localclient.FilesInfo.Find(m => m.FileName.Equals(clientFileInfo.FileName));
                if ( (localfile!=null)|| (clientFileInfo.FileSize != localfile.FileSize) || (clientFileInfo.Hash.SequenceEqual(localfile.Hash) == false) )
                {
                    logger.Info(String.Format("Local file {0} isn't equal to remote", localfile.FileName));
                    difference_list.Add(clientFileInfo);
                }
            }

            return difference_list;

        }
    }
}
