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

                await localClient.CreateModelFromDirectory(config.ConfigFields.ClientFolder, new CancellationTokenSource().Token);
                ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Собрана сокращённая информация о файлах игры" });

                ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Получаю информацию об игре с сервера" });
                await remoteClient.LoadRemoteModel(config.RemoteInfoFile);
                ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Получена информация об игре с сервера" });

                //Compare cached model to remote
                Difference = CompareModels(localClient, remoteClient, cacheClient);

                string msg = Difference.Count > 0 ? "Необходимо обновление" : "Файлы игры проверены";
                ClientCheckFinished?.Invoke(this, new ClientCheckFinishEventArgs() { FinishMessage = msg, UpdateRequired = Difference.Count > 0 });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Fast check error!");
                if (ClientCheckError == null)
                    throw ex;
                else
                    ClientCheckError.Invoke(this, new ClientCheckErrorEventArgs()
                    {
                        Exeption = ex
                    });
            }

            logger.Info("Finish fast local client check");
        }

        private void FullCheckCheckerProgress (object sender, FileCheckerProgressEventArgs args)
        {
            ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() 
            { 
                InfoStr = String.Format("Обрабатываю файл {0}", args.FileName),
                ProgressMax = args.FilesCount,
                ProgressValue = args.CurrentIndex
            });
        }

        private void FullCheckCheckerFinish(object sender, FileCheckerFinishEventArgs args)
        {
            ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs()
            {
                InfoStr = String.Format("Обработка файлов клиента завершена"),
                ProgressMax = args.FilesCount,
            });
        }

        public async void FullLocalClientCheck(CancellationToken token)
        {
            logger.Info("Start full local client check");

            remoteClient = new L2ClientRemote(loader);
            localClient = new L2ClientLocal();
            localClient.Checker.FileCheckerProgress += FullCheckCheckerProgress;
            localClient.Checker.FileCheckerFinish += FullCheckCheckerFinish;

            ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Полная проверка файлов игры" });

            try
            {
                await remoteClient.LoadRemoteModel(config.RemoteInfoFile);
                ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Получена информация о игре с сервера" });

                await localClient.CreateModelFromDirectory(config.ConfigFields.ClientFolder, token, true);
                await localClient.WriteClientModel(config.LocalInfoFile);
                ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Собрана полная информация о файлах игры" });

                Difference = CompareModels(localClient, remoteClient);

                string msg = Difference.Count > 0 ? "Необходимо обновление" : "Файлы игры проверены";
                ClientCheckFinished?.Invoke(this, new ClientCheckFinishEventArgs() { FinishMessage = msg, UpdateRequired = Difference.Count > 0 });
            }
            catch (Exception ex)
            {
                if (ClientCheckError == null)
                    throw ex;
                else
                    ClientCheckError.Invoke(this, new ClientCheckErrorEventArgs()
                    {
                        Exeption = ex
                    });
            }

            logger.Info("Finish full local client check");

        }

        public async void UpdateClient(CancellationToken token)
        {
            logger.Info("Start updating client");

            try
            {
                await loader.DownloadClientFiles(config.RemoteClientPath, config.ConfigFields.ClientFolder.LocalPath, Difference, token);
                await remoteClient.WriteClientModel(config.LocalInfoFile);
            }
            catch (Exception ex)
            {
                if (ClientCheckError == null)
                    throw ex;
                else
                    ClientCheckError.Invoke(this, new ClientCheckErrorEventArgs()
                    {
                        Exeption = ex
                    });
            }

            ClientUpdateFinished?.Invoke(this, null);


            logger.Info("Finished updating client");
        }

        public async void RewriteClient(CancellationToken token)
        {
            logger.Info("Start rewriting client");

            remoteClient = new L2ClientRemote(loader);

            ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Загрузка перечня файлов игры" });

            try
            {
                await remoteClient.LoadRemoteModel(config.RemoteInfoFile);
                ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Получена информация о игре с сервера" });

                await loader.DownloadClientFiles(config.RemoteClientPath, config.ConfigFields.ClientFolder.LocalPath, remoteClient.ClientInfo.FilesInfo, token);
                await remoteClient.WriteClientModel(config.LocalInfoFile);
            }
            catch (Exception ex)
            {
                if (ClientCheckError == null)
                    throw ex;
                else
                    ClientCheckError.Invoke(this, new ClientCheckErrorEventArgs()
                    {
                        Exeption = ex
                    });
            }

            ClientUpdateFinished?.Invoke(this, null);


            logger.Info("Finished updating client");
        }

        private List<ClientFileInfo> CompareModels (L2ClientLocal local, L2ClientRemote remote, L2ClientLocal shadow)
        {
            List<ClientFileInfo> difference = new List<ClientFileInfo>();

            foreach (ClientFileInfo fileinfo in remote.ClientInfo.FilesInfo)
            {
                ClientFileInfo cachedinfo = shadow.ClientInfo?.FilesInfo.Find(m => 0 == String.Compare(m.FileName, fileinfo.FileName, StringComparison.OrdinalIgnoreCase));
                ClientFileInfo localinfo = local.ClientInfo.FilesInfo.Find(m => 0 == String.Compare(m.FileName, fileinfo.FileName, StringComparison.OrdinalIgnoreCase));

                if (localinfo==null)
                    difference.Add(fileinfo);
                else if (cachedinfo != null)
                {
                    if ( (cachedinfo != fileinfo)||(localinfo.FileSize!=fileinfo.FileSize) )
                        difference.Add(fileinfo);
                }
                else
                {
                    if ( (localinfo.FileSize != fileinfo.FileSize) )
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
                ClientFileInfo localinfo = local.ClientInfo.FilesInfo.Find(m => 0 == String.Compare(m.FileName, fileinfo.FileName, StringComparison.OrdinalIgnoreCase));
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
