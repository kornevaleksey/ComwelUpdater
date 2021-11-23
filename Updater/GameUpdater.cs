using Config;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Updater
{
    public class GameUpdater
    {
        public event EventHandler<ClientCheckUpdateEventArgs> ClientCheckUpdate;
        public event EventHandler<ClientCheckErrorEventArgs> ClientCheckError;
        public event EventHandler<ClientCheckFinishEventArgs> ClientCheckFinished;
        public event EventHandler ClientUpdateFinished;

        private ILogger _logger;

        public List<ClientFileInfo> Difference { get; private set; }
        public bool ClientCanRun { get => File.Exists(_config.ClientExeFile); }
        public bool UpdateIsNeed { get => Difference!=null&&Difference.Count > 0; }

        LocalUpdateDirectory cacheClient;
        LocalUpdateDirectory localClient;
        RemoteSourceDirectory remoteClient;

        private readonly SimpleHttpLoader _loader;
        private readonly UpdaterConfig _config;
        private readonly FileChecker _checker;

        public GameUpdater(
            ILogger<GameUpdater> logger,
            FileChecker checker,
            SimpleHttpLoader loader,
            UpdaterConfig config)
        {
            _logger = logger;

            _loader = loader;
            _config = config;
            _checker = checker;
        }

        public async Task FastCheckAsync()
        {
            _logger.LogInformation("Start fast local directory check");
            ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Быстрая проверка файлов игры" });
        }

        public async Task FastLocalClientCheck()
        {
            CancellationTokenSource _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            cacheClient = new LocalUpdateDirectory(_logger, _checker, _config.ConfigFields.ClientFolder);
            localClient = new LocalUpdateDirectory(_logger, _checker, _config.ConfigFields.ClientFolder);
            remoteClient = new RemoteSourceDirectory(_logger, _loader);

            try
            {
                ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Получаю информацию об игре с сервера" });
                await remoteClient.LoadRemoteModel(_config.RemoteInfoFile, token);
                ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Получена информация об игре с сервера" });

                await DirectoryModel.ReadAsync(_config.LocalInfoFile);
                ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Считана сохраненная информация о файлах игры" });

                await localClient.CreateModelFromDirectory(_config.ConfigFields.ClientFolder, token);
                await localClient.CalculateHashesofImportantFiles(_config.ConfigFields.ClientFolder, remoteClient, token);
                ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Собрана сокращённая информация о файлах игры" });

                //Compare cached model to remote
                Difference = CompareModels(localClient, remoteClient, cacheClient);

                string msg = Difference.Count > 0 ? "Необходимо обновление" : "Файлы игры проверены";
                ClientCheckFinished?.Invoke(this, new ClientCheckFinishEventArgs() { FinishMessage = msg, UpdateRequired = Difference.Count > 0 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fast check error!");
                if (ClientCheckError == null)
                    throw ex;
                else
                    ClientCheckError.Invoke(this, new ClientCheckErrorEventArgs()
                    {
                        Exeption = ex
                    });
            }

            _logger.LogInformation("Finish fast local client check");
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
            _logger.LogInformation("Start full local client check");

            remoteClient = new RemoteSourceDirectory(_logger, _loader);
            localClient = new LocalUpdateDirectory(_logger, _checker, _config.ConfigFields.ClientFolder);
            _checker.FileCheckerProgress += FullCheckCheckerProgress;
            _checker.FileCheckerFinish += FullCheckCheckerFinish;

            ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Полная проверка файлов игры" });

            try
            {
                await remoteClient.LoadRemoteModel(_config.RemoteInfoFile, token);
                ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Получена информация о игре с сервера" });

                await localClient.CreateModelFromDirectory(_config.ConfigFields.ClientFolder, token, true);
                if (token.IsCancellationRequested) return;
                await DirectoryModel.WriteAsync(_config.LocalInfoFile, localClient.Model);
                ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Собрана полная информация о файлах игры" });

                if (token.IsCancellationRequested) return;
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
            finally
            {
                _checker.FileCheckerProgress -= FullCheckCheckerProgress;
                _checker.FileCheckerFinish -= FullCheckCheckerFinish;
            }

            _logger.LogInformation("Finish full local client check");

        }

        public async void UpdateClient(CancellationToken token)
        {
            _logger.LogInformation("Start updating client");

            try
            {
                await _loader.DownloadClientFiles(_config.RemoteClientPath, _config.ConfigFields.ClientFolder.LocalPath, Difference, token);
                await DirectoryModel.WriteAsync(_config.LocalInfoFile, remoteClient.Model);
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
                return;
            }

            ClientUpdateFinished?.Invoke(this, null);

            _logger.LogInformation("Finished updating client");
        }

        public async void RewriteClient(CancellationToken token)
        {
            _logger.LogInformation("Start rewriting client");

            remoteClient = new RemoteSourceDirectory(_logger, _loader);

            ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Загрузка перечня файлов игры" });

            try
            {
                await remoteClient.LoadRemoteModel(_config.RemoteInfoFile, token);
                ClientCheckUpdate?.Invoke(this, new ClientCheckUpdateEventArgs() { InfoStr = "Получена информация о игре с сервера" });

                await _loader.DownloadClientFiles(_config.RemoteClientPath, _config.ConfigFields.ClientFolder.LocalPath, remoteClient.Model.FilesInfo, token);
                await DirectoryModel.WriteAsync(_config.LocalInfoFile, remoteClient.Model);
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

            _logger.LogInformation("Finished updating client");
        }

        private List<ClientFileInfo> CompareModels (LocalUpdateDirectory local, RemoteSourceDirectory remote, LocalUpdateDirectory shadow)
        {
            List<ClientFileInfo> difference = new List<ClientFileInfo>();

            foreach (ClientFileInfo fileInfo in remote.Model.FilesInfo)
            {
                ClientFileInfo cachedinfo = shadow.Model.FilesInfo.Find(m => 0 == String.Compare(m.FileName, fileInfo.FileName, StringComparison.OrdinalIgnoreCase));
                ClientFileInfo localinfo = local.Model.FilesInfo.Find(m => 0 == String.Compare(m.FileName, fileInfo.FileName, StringComparison.OrdinalIgnoreCase));

                if (FileChecker.FilesCompare(localinfo, fileInfo, cachedinfo) == false)
                    difference.Add(fileInfo);
            }

            return difference;
        }

        private List<ClientFileInfo> CompareModels (LocalUpdateDirectory local, RemoteSourceDirectory remote)
        {
            List<ClientFileInfo> difference = new List<ClientFileInfo>();

            foreach (ClientFileInfo fileinfo in remote.Model.FilesInfo)
            {
                ClientFileInfo localinfo = local.Model.FilesInfo.Find(m => 0 == String.Compare(m.FileName, fileinfo.FileName, StringComparison.OrdinalIgnoreCase));
                if (FileChecker.FilesCompare(localinfo, fileinfo) == false)
                {
                    difference.Add(fileinfo);
                }
        
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
