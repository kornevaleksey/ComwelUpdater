using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Text.Json;
using Config;


namespace Updater
{
    public class L2Updater
    {
        protected static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public event EventHandler<UpdaterProgressEventArgs> ProgressUpdate;

        L2ClientLocal cacheClient;
        L2ClientLocal localClient;
        L2ClientRemote remoteClient;

        Loader loader;
        L2UpdaterConfig config;

        public List<ClientFileInfo> Difference { get; private set; }

        public L2Updater(Loader loader, L2UpdaterConfig config)
        {
            this.loader = loader;
            this.config = config;
        }

        private void ProgressIndicate(long current, long max, string text, Color color)
        {
            ProgressUpdate?.Invoke(this, new UpdaterProgressEventArgs()
            {
                ProgressMax = max,
                ProgressValue = current,
                InfoStr = text,
                InfoStrColor = color,
                RaiseMessageBox = false
            });
        }

        private void ProgressBox(string text)
        {
            ProgressUpdate?.Invoke(this, new UpdaterProgressEventArgs()
            {
                InfoStr = text,
                RaiseMessageBox = true
            });
        }

        public async Task FastLocalClientCheck()
        {
            if (File.Exists(config.LocalInfoFile))
            {
                //Read cached client model
                cacheClient = new L2ClientLocal();
                await cacheClient.ReadClientModel(config.LocalInfoFile);
                logger.Info("Read client model from " + config.LocalInfoFile);
                ProgressIndicate(0, 100, "Считана информация о клиенте", Color.Black);

                if (Directory.Exists(config.ConfigFields.ClientFolder.LocalPath))
                {
                    localClient = new L2ClientLocal();
                    //Make short model of client (without hashes)
                    await localClient.CreateModelFromDirectory(config.ConfigFields.ClientFolder);
                    logger.Info("Make client model from directory: " + config.ConfigFields.ClientFolder.LocalPath);
                    ProgressIndicate(50, 100, "Составлен список файлов клиента", Color.Black);
                } else
                {
                    ClientDirectoryException ex = new ClientDirectoryException() { Directory = config.ConfigFields.ClientFolder.LocalPath };
                    logger.Error(ex, "Impossible to make client model from non-existent directory: " + config.ConfigFields.ClientFolder.LocalPath);
                    throw ex; 
                }

                //Load remote model client
                loader.RemoteAddr = config.ConfigFields.DownloadAddress;
                remoteClient = new L2ClientRemote(loader);
                await remoteClient.LoadRemoteModel(config.RemoteInfoFile);

                if (remoteClient.ClientInfo == null)
                {
                    RemoteModelException ex = new RemoteModelException() { RemoteAddr = loader.RemoteAddr, RemoteFile = config.RemoteInfoFile };
                    logger.Error(ex, "Can't load remote client model file!");
                    throw ex;
                } else
                {
                    ProgressIndicate(100, 100, "Скачана информация о клиенте с сервера", Color.Black);
                }

                //Compare cached model to remote
                Difference = new List<ClientFileInfo>();
                int progress_counter = 0;
                foreach (ClientFileInfo fileinfo in remoteClient.ClientInfo.FilesInfo)
                {
                    ProgressIndicate(progress_counter++, remoteClient.ClientInfo.FilesInfo.Count, "Проверяю файл "+fileinfo.FileName, Color.Black);
                    ClientFileInfo cachedinfo = cacheClient.ClientInfo.FilesInfo.Find(m => m.FileName.Equals(fileinfo.FileName));
                    if ((cachedinfo == null) || (cachedinfo != fileinfo))
                    {
                        Difference.Add(fileinfo);
                    }
                    else
                    {
                        ClientFileInfo localinfo = localClient.ClientInfo.FilesInfo.Find(m => m.FileName.Equals(fileinfo.FileName));
                        if ((localinfo == null) || (localinfo.FileSize != fileinfo.FileSize))
                            Difference.Add(fileinfo);
                    }
                }
            } else
            {
                ProgressIndicate(0, 100, "Не обнаружено информации о клиенте игры!", Color.Red);
                logger.Info("Local info of client files doesn't exist!");
                await FullLocalClientCheck();
            }
        }

        public async Task FullLocalClientCheck(bool saveclientmodel=false)
        {
            //Load remote model client
            //loader.RemoteAddr = config.ConfigFields.DownloadAddress;
            remoteClient = new L2ClientRemote(loader);
            await remoteClient.LoadRemoteModel(config.RemoteInfoFile);

            if (remoteClient.ClientInfo == null)
            {
                RemoteModelException ex = new RemoteModelException() { RemoteAddr = loader.RemoteAddr, RemoteFile = config.RemoteInfoFile };
                logger.Error(ex, "Can't load remote client model file!");
                throw ex;
            }
            else
            {
                ProgressIndicate(0, 100, "Скачана информация о клиенте с сервера", Color.Black);
            }

            //Make complete model of client
            localClient = new L2ClientLocal();
            await localClient.CreateModelFromDirectory(config.ConfigFields.ClientFolder, true, ProgressUpdate);
            logger.Info("Make client model from directory: " + config.ConfigFields.ClientFolder.LocalPath);
            ProgressIndicate(0, 100, "Определена информация о файлах клиента", Color.Black);

            if (saveclientmodel)
                await localClient.WriteClientModel(config.LocalInfoFile);

            //await localClient.WriteClientModel("C:\\Users\\korall_admin\\AppData\\Roaming\\ComwelUpdater\\clientinfo.inf");

            //Compare complete local model to remote
            Difference = new List<ClientFileInfo>();
            int progress_counter = 0;
            foreach (ClientFileInfo fileinfo in remoteClient.ClientInfo.FilesInfo)
            {
                ProgressIndicate(progress_counter++, remoteClient.ClientInfo.FilesInfo.Count, "Проверяю файл " + fileinfo.FileName, Color.Black);

                ClientFileInfo localinfo = localClient.ClientInfo.FilesInfo.Find(m => m.FileName.Equals(fileinfo.FileName));
                if ((localinfo==null)||(localinfo != fileinfo) )
                    Difference.Add(fileinfo);
            }
        }

        public async Task UpdateClient()
        {
            //Download different files
            List<ClientFileInfo> notloadedfiles = await loader.DownloadClientFiles(config.RemoteClientPath, config.ConfigFields.ClientFolder.LocalPath, Difference);

            if (notloadedfiles.Count > 0)
            {
                LoaderFilesLoadException ex = new LoaderFilesLoadException() { Files = notloadedfiles };
                logger.Error(ex, "Cant't load files");
                throw ex;
            }
            else
            {
                //Write current server file info as local cached info
                await remoteClient.WriteClientModel(config.LocalInfoFile);
            }
        }

    }

    public class UpdaterProgressEventArgs : EventArgs
    {
        public long ProgressMax { get; set; }
        public long ProgressValue { get; set; }
        public string InfoStr { get; set; }
        public Color InfoStrColor { get; set; }
        public bool RaiseMessageBox { get; set; }
    }

    public class ClientDirectoryException:Exception
    {
        public string Directory { get; set; }
    }

    public class LoaderFilesLoadException: Exception
    {
        public List<ClientFileInfo> Files { get; set; }
    }
}
