using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Drawing;

namespace Updater
{
    public class L2Client
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public string ClientPath { get; set; }
        public string ClientHashesFile { get; set; }
        public string RemoteHashesFile { get; set; }

        public List<string> LocalDifference { get; private set; }

        public event EventHandler<UpdaterProgressEventArgs> ProgressUpdate;

        string remotehashes_localtemp;

        long ClientSize;

        List<ClientFileInfo> LocalFilesInfo;
        List<ClientFileInfo> RemoteFilesInfo;

        Loader loader;
        FileChecker checker;

        public L2Client(Loader loader)
        {
            this.loader = loader;
            checker = new FileChecker();
        }

        public async Task<bool> CheckClient (bool fast)
        {
            List<ClientFileInfo> clientinfo;

            logger.Info("Start check client files");

            if (fast==true)
            {
                logger.Info("Fast check");
                //Check only files list and hashes

                logger.Info("Check client info files list and client files list accordance");
                clientinfo = await ReadFilesInfo(ClientHashesFile);
                if (clientinfo==null)
                {
                    logger.Info("Cant read client files info");
                    return false;
                }
            } else
            {
                clientinfo = await GetClientFilesInfo();
                await WriteFilesInfo(clientinfo, ClientHashesFile);

                return false;
            }

            List<string> clientinfo_files = clientinfo.Select(ci => Uri.UnescapeDataString(ci.FileName)).ToList();
            List<string> local_files = await ClientFiles();

            List<string> local_difference = clientinfo_files.Except(local_files).ToList();
            if (local_difference.Count > 0)
            {
                logger.Info("Wrong information of client files!");
                logger.Info(String.Join(Environment.NewLine, local_difference.ToArray()));
                ProgressUpdate?.Invoke(this, new UpdaterProgressEventArgs()
                {
                    ProgressMax = 100,
                    ProgressValue = 0,
                    InfoStr = "Ошибка: несовпадение информации о клиенте с актуальной информацией!",
                    InfoStrColor = Color.Red
                });
                return false;
            }
            else
            {
                logger.Info("Information of client files list is actual");
            }

            logger.Info("Compare local files hashes to remote files hashes");

            LocalDifference = new List<string>();
            List<ClientFileInfo> remote_clientinfo = await GetRemoteFilesInfo();
            foreach (ClientFileInfo fi_remote in remote_clientinfo)
            {
                ClientFileInfo fi_local = clientinfo.Find(f => Uri.UnescapeDataString(fi_remote.FileName).Equals(f.FileName));
                if (fi_local != null)
                {
                    if (fi_remote.Hash.SequenceEqual(fi_local.Hash) == false)
                    {
                        logger.Info(String.Format("Local file {0} different from remote", fi_remote.FileName));
                        LocalDifference.Add(fi_remote.FileName);
                    }
                    clientinfo.Remove(fi_local);
                }
                else
                {
                    logger.Info(String.Format("Local file {0} isn't exist", fi_remote.FileName));
                    LocalDifference.Add(fi_remote.FileName);
                }
            }
            return true;
        }

        public async Task<List<string>> ClientFiles(bool absolutepaths = false)
        {
            if (absolutepaths==false)
                return await Task.Run(() => Directory.GetFiles(ClientPath, "*.*", SearchOption.AllDirectories).Select(fn => Path.GetRelativePath(ClientPath, fn)).ToList());
            else
                return await Task.Run(() => Directory.GetFiles(ClientPath, "*.*", SearchOption.AllDirectories).ToList());
        }

        public async Task<List<string>> RemoteFiles()
        {
            List<ClientFileInfo> clientFiles = await GetRemoteFilesInfo();
            return clientFiles.Select(cf => cf.FileName).ToList();
        }

        public async Task<List<ClientFileInfo>> GetClientFilesInfo()
        {
            List<string> FilesNames = await ClientFiles(true);

            //Calculate client size
            this.ClientSize = 0;
            foreach (string file in FilesNames)
                this.ClientSize += new FileInfo(file).Length;

            long HashedSize = 0;
            List<ClientFileInfo> result = new List<ClientFileInfo>();
            if (FilesNames.Count > 0)
            {
                foreach (string filename in FilesNames)
                {
                    ClientFileInfo clientFileInfo = await checker.GetFileInfo(filename);
                    clientFileInfo.FileName = Path.GetRelativePath(ClientPath, filename);

                    HashedSize += clientFileInfo.FileSize;

                    ProgressUpdate?.Invoke(this, new UpdaterProgressEventArgs()
                    {
                        ProgressMax = this.ClientSize,
                        ProgressValue = HashedSize,
                        InfoStr = filename,
                        InfoStrColor = Color.Black
                    });

                    result.Add(clientFileInfo);
                }
            }
            return result;
        }

        public async Task<List<ClientFileInfo>> GetRemoteFilesInfo()
        {
            remotehashes_localtemp = Path.GetTempFileName();
            await loader.DownloadFile(RemoteHashesFile, remotehashes_localtemp);

            return await ReadFilesInfo(remotehashes_localtemp);
        }

        public async Task<bool> WriteFilesInfo(List<ClientFileInfo> clientFileInfos, string filename)
        {
            var serlist = JsonSerializer.Serialize(clientFileInfos);
            await File.WriteAllTextAsync(filename, serlist);
            return true;
        }

        public async Task<List<ClientFileInfo>> ReadFilesInfo(string filename)
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
