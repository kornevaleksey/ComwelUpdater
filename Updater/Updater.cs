using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Text.Json;
using Config;
using System.Threading;

namespace Updater
{
    public class L2Updater
    {
        protected static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public event EventHandler<ClientCheckUpdateEventArgs> ClientCheckUpdate;
        public event EventHandler<ClientCheckErrorEventArgs> ClientCheckError;
        public event EventHandler<ClientCheckFinishEventArgs> ClientCheckFinished;
        public event EventHandler ClientUpdateFinished;

        public List<ClientFileInfo> Difference { get; private set; }
        public bool IsBusy { get; private set; }
        public bool ClientCanRun { get => File.Exists(config.ClientExeFile); }

        L2ClientLocal cacheClient;
        L2ClientLocal localClient;
        L2ClientRemote remoteClient;

        readonly Loader loader;
        readonly L2UpdaterConfig config;

        public L2Updater(Loader loader, L2UpdaterConfig config)
        {
            this.loader = loader;
            this.config = config;

            this.IsBusy = false;
        }

        public async void FastLocalClientCheck()
        {
            if (IsBusy == false)
            {
                logger.Info("Start fast local client check");
                IsBusy = true;
                ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr="Быстрая проверка файлов игры" });

                cacheClient = new L2ClientLocal();
                localClient = new L2ClientLocal();
                remoteClient = new L2ClientRemote(loader);

                try
                {
                    await cacheClient.ReadClientModel(config.LocalInfoFile);
                    ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Считана сохраненная информация о файлах игры" });
                }
                catch (FileNotFoundException fnfex)
                {

                }
                catch (JsonException jsonex)
                {

                }
                catch (ArgumentNullException argnullex)
                {

                }

                try
                {
                    await localClient.CreateModelFromDirectory(config.ConfigFields.ClientFolder);
                    ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Собрана сокращённая информация о файлах игры" });
                }
                catch (FileNotFoundException fnfex)
                {

                }

                try
                {
                    await remoteClient.LoadRemoteModel(config.RemoteInfoFile);
                    ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Получена информация о игре с сервера" });
                }
                finally
                {

                }

                //Compare cached model to remote
                Difference = CompareModels (localClient, remoteClient, cacheClient);

                string msg = Difference.Count > 0 ? "Необходимо обновление" : "Файлы игры проверены";
                ClientCheckFinished?.Invoke(this, new ClientCheckFinishEventArgs() { FinishMessage = msg, UpdateRequired = Difference.Count>0 });

                IsBusy = false;
                logger.Info("Finish fast local client check");

            } else
            {
                logger.Error("Can't start fast local client check - L2Updater busy");
            }
        }

        public async Task FullLocalClientCheck(CancellationToken token)
        {
            if (IsBusy == false)
            {
                IsBusy = true;
                logger.Info("Start full local client check");

                remoteClient = new L2ClientRemote(loader);
                localClient = new L2ClientLocal();

                ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Полная проверка файлов игры" });

                try
                {
                    await remoteClient.LoadRemoteModel(config.RemoteInfoFile);
                    ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Получена информация о игре с сервера" });
                }
                finally
                {

                }

                try
                {
                    await localClient.CreateModelFromDirectory(config.ConfigFields.ClientFolder, true);
                    await localClient.WriteClientModel(config.LocalInfoFile);
                    ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Собрана полная информация о файлах игры" });
                }
                finally
                {

                }

                Difference = CompareModels(localClient, remoteClient);

                string msg = Difference.Count > 0 ? "Необходимо обновление" : "Файлы игры проверены";
                ClientCheckFinished?.Invoke(this, new ClientCheckFinishEventArgs() { FinishMessage = msg, UpdateRequired = Difference.Count > 0 });

                logger.Info("Finish full local client check");
                IsBusy = false;
            }
            else
            {
                logger.Error("Can't start full local client check - L2Updater busy");
            }
        }

        public async Task UpdateClient()
        {
            if (IsBusy==false)
            {
                IsBusy = true;
                logger.Info("Start updating client");

                try
                {
                    await loader.DownloadClientFiles(config.RemoteClientPath, config.ConfigFields.ClientFolder.LocalPath, Difference);
                    await remoteClient.WriteClientModel(config.LocalInfoFile);
                }
                finally
                {

                }

                ClientUpdateFinished?.Invoke(this, null);

                IsBusy = false;
                logger.Info("Finished updating client");
            } else
            {
                logger.Error("Can't start updating local client - L2Updater busy");
            }
        }

        private List<ClientFileInfo> CompareModels (L2ClientLocal local, L2ClientRemote remote, L2ClientLocal shadow)
        {
            List<ClientFileInfo> difference = new List<ClientFileInfo>();

            foreach (ClientFileInfo fileinfo in remote.ClientInfo.FilesInfo)
            {
                ClientFileInfo cachedinfo = shadow.ClientInfo.FilesInfo.Find(m => m.FileName.Equals(fileinfo.FileName));
                if ((cachedinfo == null) || (cachedinfo != fileinfo))
                {
                    difference.Add(fileinfo);
                }
                else
                {
                    ClientFileInfo localinfo = local.ClientInfo.FilesInfo.Find(m => m.FileName.Equals(fileinfo.FileName));
                    if ((localinfo == null) || (localinfo.FileSize != fileinfo.FileSize))
                        difference.Add(fileinfo);
                }
            }

            return difference;
        }

        private List<ClientFileInfo> CompareModels (L2ClientLocal local, L2ClientRemote remote)
        {
            List<ClientFileInfo> difference = new List<ClientFileInfo>();

            foreach (ClientFileInfo fileinfo in remote.ClientInfo.FilesInfo)
            {
                ClientFileInfo localinfo = local.ClientInfo.FilesInfo.Find(m => m.FileName.Equals(fileinfo.FileName));
                if ((localinfo == null) || (localinfo != fileinfo))
                    difference.Add(fileinfo);
            }

            return difference;
        }
    }

    public class ClientCheckUpdateEventArgs : EventArgs
    {
        public long ProgressMax { get; set; } = 0;
        public long ProgressValue { get; set; } = 0;
        public string InfoStr { get; set; } = "";
    }

    public class ClientCheckErrorEventArgs: EventArgs
    {
        public Exception Exeption { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ClientCheckFinishEventArgs : EventArgs
    {
        public string FinishMessage { get; set; }
        public bool UpdateRequired;
    }

    public class ClientDirectoryException:Exception
    {
        public string Directory { get; set; }
    }
    public class UpdaterCheckClientException : Exception
    {
    }

}
