﻿using Config;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Updater
{
    public class GameUpdater
    {
        public event EventHandler<UpdaterProgressEventArgs>? ClientCheckProgress;
       
        private readonly ILogger? _logger;

        private LocalUpdateDirectory? localClient;
        private RemoteSourceDirectory? remoteClient;

        private readonly SimpleHttpLoader loader;
        private readonly FileChecker checker;
        private readonly UpdaterConfigFactory configFactory;
        private readonly Func<Uri, LocalUpdateDirectory> localDirectoryFactory;
        private readonly Func<string, RemoteSourceDirectory> remoteSourceFactory;

        private UpdaterConfig config;

        public GameUpdater(
            ILogger<GameUpdater>? logger,
            FileChecker checker,
            SimpleHttpLoader loader,
            UpdaterConfigFactory configFactory,
            Func<Uri, LocalUpdateDirectory> localDirectoryFactory,
            Func<string, RemoteSourceDirectory> remoteSourceFactory)
        {
            _logger = logger;

            this.loader = loader;
            this.configFactory = configFactory;
            this.checker = checker;
            this.localDirectoryFactory = localDirectoryFactory;
            this.remoteSourceFactory = remoteSourceFactory;

            config = configFactory.Create();
        }

        public bool IsBusy { get; private set; }
        public bool ClientCanRun { get => File.Exists(config?.ClientExeFile); }
        public bool UpdateIsNeed { get => Difference != null && Difference.Count > 0; }

        public List<ClientFileInfo>? Difference { get; private set; }

        public async Task FastCheckAsync(CancellationToken token)
        {
            IsBusy = true;

            _logger?.LogInformation("Start fast local directory check");

            ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() { InfoStr = "Быстрая проверка файлов игры" });

            localClient = localDirectoryFactory(config.LocalDirectory);
            remoteClient = remoteSourceFactory(config.RemoteInfoFile);

            try
            {
                ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() { InfoStr = "Получаю информацию об игре с сервера" });
                await remoteClient.LoadRemoteModelAsync(token);

                if (remoteClient.Model == null)
                {
                    ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() { InfoStr = "Не получилось собрать информацию об игре с сервера" });
                    return;
                }

                ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() { InfoStr = "Получена информация об игре с сервера" });

                await localClient.LoadDirectoryCache(config.LocalInfoFile, token);
                ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() { InfoStr = "Считана сохраненная информация о файлах игры" });

                await localClient.CreateDirectoryModelAsync(token);
                await localClient.CalculateHashesofImportantFiles(remoteClient.Model.FilesInfo.Where(fi => fi.ImportantFile), token);

                if (localClient.Model == null)
                {
                    ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() { InfoStr = "Не получилось проверить файлы игры" });
                    return;
                }

                ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() { InfoStr = "Проверены важные файлы игры" });

                Difference = CompareModels(localClient.Model, remoteClient.Model, localClient.CachedModel);



            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fast check error!");
                throw;
            }

            _logger?.LogInformation("Succefully finished fast local directory check");
        }

        private void FullCheckCheckerProgress (object? sender, FileCheckerProgressEventArgs args)
        {
            ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() 
            { 
                InfoStr = String.Format("Обрабатываю файл {0}", args.FileName),
                ProgressMax = args.FilesCount,
                ProgressValue = args.CurrentIndex
            });
        }

        private void FullCheckCheckerFinish(object? sender, FileCheckerFinishEventArgs args)
        {
            ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs()
            {
                InfoStr = String.Format("Обработка файлов клиента завершена"),
                ProgressMax = args.FilesCount,
            });
        }

        public async void FullLocalClientCheck(CancellationToken token)
        {
            _logger?.LogInformation("Start full local client check");

            localClient = localDirectoryFactory(config.LocalDirectory);
            remoteClient = remoteSourceFactory(config.RemoteInfoFile);

            checker.FileCheckerProgress += FullCheckCheckerProgress;
            checker.FileCheckerFinish += FullCheckCheckerFinish;

            ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() { InfoStr = "Полная проверка файлов игры" });

            try
            {
                await remoteClient.LoadRemoteModelAsync(token);
                if (remoteClient.Model == null)
                {
                    ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() { InfoStr = "Не получилось собрать информацию об игре с сервера" });
                    return;
                }

                ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() { InfoStr = "Получена информация об игре с сервера" });

                await localClient.CreateDirectoryModelAsync(token, true);
                await localClient.CalculateHashesofImportantFiles(remoteClient.Model.FilesInfo.Where(fi => fi.ImportantFile), token);

                if (localClient.Model == null)
                {
                    ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() { InfoStr = "Не получилось проверить файлы игры" });
                    return;
                }

                await DirectoryModel.WriteAsync(config.LocalInfoFile, localClient.Model);
                ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() { InfoStr = "Собрана полная информация о файлах игры" });

                Difference = CompareModels(localClient.Model, remoteClient.Model);

                //string msg = Difference.Count > 0 ? "Необходимо обновление" : "Файлы игры проверены";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Full check error!");
                throw;
            }
            finally
            {
                checker.FileCheckerProgress -= FullCheckCheckerProgress;
                checker.FileCheckerFinish -= FullCheckCheckerFinish;
            }

            _logger?.LogInformation("Finish full local client check");

        }

        public async void UpdateClient(CancellationToken token)
        {
            _logger?.LogInformation("Start updating client");

            try
            {
                await loader.DownloadClientFiles(config.RemoteClientPath.AbsoluteUri, config.LocalDirectory.LocalPath, Difference, token);
                await DirectoryModel.WriteAsync(config.LocalInfoFile, remoteClient.Model);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Update error!");
                throw;
            }

            _logger?.LogInformation("Finished updating client");
        }

        public async void RewriteClient(CancellationToken token)
        {
            _logger?.LogInformation("Start rewriting client");

            localClient = localDirectoryFactory(config.LocalDirectory);
            remoteClient = remoteSourceFactory(config.RemoteInfoFile);

            try
            {
                ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() { InfoStr = "Загрузка перечня файлов игры" });
                await remoteClient.LoadRemoteModelAsync(token);
                ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() { InfoStr = "Получена информация о игре с сервера" });

                await loader.DownloadClientFiles(config.RemoteClientPath.AbsoluteUri, config.LocalDirectory.LocalPath, remoteClient.Model.FilesInfo, token);
                await DirectoryModel.WriteAsync(config.LocalInfoFile, remoteClient.Model);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Rewrite error!");
                throw;
            }

            _logger?.LogInformation("Finished updating client");
        }

        private List<ClientFileInfo> CompareModels (DirectoryModel local, DirectoryModel remote, DirectoryModel? shadow = null)
        {
            List<ClientFileInfo> difference = new List<ClientFileInfo>();

            foreach (ClientFileInfo fileInfo in remote.FilesInfo)
            {
                ClientFileInfo? cachedinfo = shadow?.FilesInfo.Find(m => 0 == String.Compare(m.FileName, fileInfo.FileName, StringComparison.OrdinalIgnoreCase));
                ClientFileInfo? localinfo = local.FilesInfo.Find(m => 0 == String.Compare(m.FileName, fileInfo.FileName, StringComparison.OrdinalIgnoreCase));

                if (FileChecker.FilesCompare(localinfo, fileInfo, cachedinfo) == false)
                    difference.Add(fileInfo);
            }

            return difference;
        }

    }

    public class UpdaterProgressEventArgs : EventArgs
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
