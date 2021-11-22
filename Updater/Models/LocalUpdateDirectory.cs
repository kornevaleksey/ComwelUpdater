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
        protected ILogger _logger;

        private readonly FileChecker _checker;

        private DirectoryModel clientInfo;
        private readonly Uri _localDirectoryPath;

        public LocalUpdateDirectory(ILogger logger, FileChecker fileChecker, Uri localDirectoryPath)
        {
            _logger = logger;
            _checker = fileChecker;
            _localDirectoryPath = localDirectoryPath;
        }

        public DirectoryModel Model { get; private set; }

        public async Task CreateDirectoryModel(CancellationToken token)
        {
            string directoryName = _localDirectoryPath.LocalPath;
            DirectoryInfo directoryInfo = new DirectoryInfo(directoryName);

            FileListEnumerator files = new FileListEnumerator(directoryInfo);

            IEnumerable<string> filesList = files.Select(info => info.FullName);

            clientInfo.FilesInfo = await _checker.GetFilesListInfo(filesList, directoryName, token);
            clientInfo.ClientSize += clientInfo.FilesInfo.Sum(ci => ci.FileSize);
        }

        public async Task CreateModelFromDirectory(Uri localDir, CancellationToken token)
        {
            List<string> filenames_local;
            if (Directory.Exists(localDir.LocalPath) == true)
                filenames_local = await Task.Run(() => Directory.GetFiles(localDir.LocalPath, "*.*", SearchOption.AllDirectories).ToList());
            else
                filenames_local = new List<string>();

            clientInfo = new DirectoryModel()
            {
                Changed = DateTime.Now,
                FilesCount = (uint)filenames_local.Count,
                ClientSize = 0,
                FilesInfo = new List<ClientFileInfo>()
            };

            clientInfo.FilesInfo = await _checker.GetFilesListInfo(filenames_local, localDir.LocalPath, token);
            clientInfo.ClientSize += clientInfo.FilesInfo.Sum(ci => ci.FileSize);
        }

        public async Task CreateModelFromDirectory(Uri localDir, CancellationToken token, bool complete = false)
        {
            List<string> filenames_local;
            if (Directory.Exists(localDir.LocalPath) == true)
                filenames_local = await Task.Run(() => Directory.GetFiles(localDir.LocalPath, "*.*", SearchOption.AllDirectories).ToList());//.Select(fn => Path.GetRelativePath(localDir.LocalPath, fn)).ToList());
            else
                filenames_local = new List<string>();

            clientInfo = new DirectoryModel()
            {
                Changed = DateTime.Now,
                FilesCount = (uint)filenames_local.Count,
                ClientSize = 0,
                FilesInfo = new List<ClientFileInfo>()
            };

            clientInfo.FilesInfo = await _checker.GetFilesListInfo(filenames_local, localDir.LocalPath, token, complete);
            clientInfo.ClientSize += clientInfo.FilesInfo.Sum(ci => ci.FileSize);
        }

        public async Task CalculateHashesofImportantFiles(Uri localDir, RemoteSourceDirectory clientRemote, CancellationToken token)
        {
            List<ClientFileInfo> important_files = clientRemote.Model.FilesInfo.Where(info => info.ImportantFile).ToList();

            foreach (ClientFileInfo fileinfo in important_files)
            {
                if (token.IsCancellationRequested)
                    return;

                int index = clientInfo.FilesInfo.FindIndex(f => f.FileName.Equals(fileinfo.FileName));
                if (index >= 0)
                {
                    string path_to_local_file = Path.Combine(localDir.LocalPath, clientInfo.FilesInfo[index].FileName);
                    ClientFileInfo local_hash = await _checker.GetFileInfo(path_to_local_file, true);
                    clientInfo.FilesInfo[index].Hash = local_hash.Hash;
                }
            }
        }

    }
}
