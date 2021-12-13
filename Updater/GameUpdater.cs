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
using System.Linq;
using Updater.Events;

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
        private readonly Func<Uri, LocalUpdateDirectory> localDirectoryFactory;
        private readonly Func<string, RemoteSourceDirectory> remoteSourceFactory;

        private readonly UpdaterConfig config;

        public GameUpdater(
            ILogger<GameUpdater>? logger,
            FileChecker checker,
            SimpleHttpLoader loader,
            UpdaterConfig config,
            Func<Uri, LocalUpdateDirectory> localDirectoryFactory,
            Func<string, RemoteSourceDirectory> remoteSourceFactory)
        {
            _logger = logger;

            this.loader = loader;
            this.checker = checker;
            this.localDirectoryFactory = localDirectoryFactory;
            this.remoteSourceFactory = remoteSourceFactory;

            this.config = config;
        }

        public bool IsBusy { get; private set; }
        public bool ClientCanRun { get => File.Exists(config?.ClientExeFile); }
        public bool UpdateIsNeed { get => Difference != null && Difference.Count > 0; }

        public List<ClientFileInfo>? Difference { get; private set; }

        public async Task<bool> FastCheckAsync(CancellationToken token)
        {
            IsBusy = true;

            loader.RemoteAddr = config.RemoteDirectoryInfo;

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
                    return false;
                }

                ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() { InfoStr = "Получена информация об игре с сервера" });

                await localClient.LoadDirectoryCache(config.LocalInfoFile, token);
                ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() { InfoStr = "Считана сохраненная информация о файлах игры" });

                await localClient.CreateDirectoryModelAsync(token);
                await localClient.CalculateHashesofImportantFiles(remoteClient.Model.FilesInfo.Where(fi => fi.ImportantFile), token);

                if (localClient.Model == null)
                {
                    ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() { InfoStr = "Не получилось проверить файлы игры" });
                    return false;
                }

                ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() { InfoStr = "Проверены важные файлы игры" });

                Difference = CompareModels(localClient.Model, remoteClient.Model, localClient.CachedModel);

                _logger?.LogInformation("Succefully finished fast local directory check");

                return Difference.Count > 0;

            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fast check error!");
                throw;
            } 
            finally
            { 
                IsBusy = false;
            }
        }

        private void FullCheckCheckerProgress (object? sender, FileCheckerProgressEventArgs args)
        {
            ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs()
            {
                InfoStr = String.Format("Обрабатываю файл {0}", args.FileName),
                Progress = args.CurrentIndex * 1.0 / args.FilesCount
            });
        }

        private void FullCheckCheckerFinish(object? sender, FileCheckerFinishEventArgs args)
        {
            ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs()
            {
                InfoStr = String.Format("Обработка файлов клиента завершена"),
                Progress = 100
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

        public async Task UpdateClient(CancellationToken token)
        {
            _logger?.LogInformation("Start updating client");
            if (Difference == null)
            {
                _logger?.LogInformation("Update don't need");
                return;
            }

            if (remoteClient == null || remoteClient.Model == null)
            {
                _logger?.LogInformation("The remote model hasn't build");
                return;
            }

            IsBusy = true;
            loader.RemoteAddr = config.RemoteDirectoryFiles;

            void OnLoaderProgress(object? sender, LoaderProgressEventArgs args)
            {
                ClientCheckProgress?.Invoke(this, 
                    new UpdaterProgressEventArgs() 
                    {
                        InfoStr = $" Скачиваю файл {args.FileName}", 
                        Progress = args.Percentage 
                    });
            }

            void OnUnzipProgress(object? sender, LoaderUnZipProgressEventArgs args)
            {
                ClientCheckProgress?.Invoke(this,
                    new UpdaterProgressEventArgs()
                    {
                        Overall = false,
                        InfoStr = $"Скачиваю и распаковываю файл {args.FileName}",
                        Progress = args.Percentage
                    });
            }

            try
            {
                loader.LoaderProgress += OnLoaderProgress;
                loader.UnZipProgress += OnUnzipProgress;

                await loader.DownloadClientFiles(config.RemoteDirectoryFiles.AbsoluteUri, config.LocalDirectory.LocalPath, Difference, token);
                await DirectoryModel.WriteAsync(config.LocalInfoFile, remoteClient.Model);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Update error!");
                throw;
            }
            finally 
            {
                loader.LoaderProgress -= OnLoaderProgress;
                loader.UnZipProgress -= OnUnzipProgress;
                IsBusy = false;
            }

            _logger?.LogInformation("Finished updating client");
        }

        public async Task RewriteClient(CancellationToken token)
        {
            _logger?.LogInformation("Start rewriting client");

            localClient = localDirectoryFactory(config.LocalDirectory);
            remoteClient = remoteSourceFactory(config.RemoteInfoFile);

            if (remoteClient == null || remoteClient.Model == null)
            {
                _logger?.LogInformation("The remote model hasn't build");
                return;
            }

            IsBusy = true;

            try
            {
                loader.RemoteAddr = config.RemoteDirectoryInfo;
                ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() { InfoStr = "Загрузка перечня файлов игры" });
                await remoteClient.LoadRemoteModelAsync(token);
                ClientCheckProgress?.Invoke(this, new UpdaterProgressEventArgs() { InfoStr = "Получена информация о игре с сервера" });

                loader.RemoteAddr = config.RemoteDirectoryFiles;
                await loader.DownloadClientFiles(config.RemoteDirectoryFiles.AbsoluteUri, config.LocalDirectory.LocalPath, remoteClient.Model.FilesInfo, token);
                await DirectoryModel.WriteAsync(config.LocalInfoFile, remoteClient.Model);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Rewrite error!");
                throw;
            }
            finally
            {
                IsBusy = false;
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
        public bool Overall { get; set; } = true;
        public double Progress { get; set; } = 0;
        public string InfoStr { get; set; } = "";
    }

}
