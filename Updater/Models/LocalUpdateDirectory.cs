using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Updater
{
    public class LocalUpdateDirectory
    {
        private readonly ILogger? _logger;

        private readonly FileChecker _checker;
        private readonly Uri _localDirectoryPath;

        public LocalUpdateDirectory(ILogger<LocalUpdateDirectory>? logger, FileChecker fileChecker, Uri localDirectoryPath)
        {
            _logger = logger;
            _checker = fileChecker;
            _localDirectoryPath = localDirectoryPath;
        }

        public DirectoryModel? Model { get; private set; }
        public DirectoryModel? CachedModel { get; private set; }

        public async Task LoadDirectoryCache(string cacheFile, CancellationToken token)
        {
            CachedModel = await DirectoryModel.ReadAsync(cacheFile);
        }

        public async Task CreateDirectoryModelAsync(CancellationToken token, bool complete = false)
        {
            string directoryName = _localDirectoryPath.LocalPath;
            DirectoryInfo directoryInfo = new DirectoryInfo(directoryName);

            FileListEnumerator files = new FileListEnumerator(directoryInfo);

            IEnumerable<string> filesList = files.Select(info => info.FullName);

            Model = new DirectoryModel()
            {
                Changed = DateTime.Now,
                FilesInfo = new List<ClientFileInfo>()
            };

            Model.FilesInfo = await _checker.GetFilesListInfo(filesList, directoryName, token, complete);
            Model.FilesCount = (uint)Model.FilesInfo.Count;
            Model.ClientSize += Model.FilesInfo.Sum(ci => ci.FileSize);
        }

        public async Task CalculateHashesofImportantFiles(IEnumerable<ClientFileInfo> importantFiles, CancellationToken token)
        {
            if (Model == null)
            {
                return;
            }

            foreach (ClientFileInfo fileinfo in importantFiles)
            {
                if (token.IsCancellationRequested)
                    return;

                int index = Model.FilesInfo.FindIndex(f => f.FileName.Equals(fileinfo.FileName));
                if (index >= 0)
                {
                    string path_to_local_file = Path.Combine(_localDirectoryPath.LocalPath, Model.FilesInfo[index].FileName);
                    ClientFileInfo local_hash = await _checker.GetFileInfo(path_to_local_file, true);
                    Model.FilesInfo[index].Hash = local_hash.Hash;
                }
            }
        }

    }
}
