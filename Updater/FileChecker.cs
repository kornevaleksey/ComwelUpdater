using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Drawing;
using Dasync.Collections;

namespace Updater
{
    public class FileChecker
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public EventHandler<FileCheckerProgressEventArgs> FileCheckerProgress;
        public EventHandler<FileCheckerFinishEventArgs> FileCheckerFinish;

        readonly SHA256 sha256;

        public FileChecker()
        {
            logger.Info("Creating new FileChecker");
            sha256 = SHA256.Create();
        }

        public async Task<ClientFileInfo> GetFileInfo (string filename, bool complete = false)
        {
            ClientFileInfo clientFileInfo = new ClientFileInfo
            {
                FileName = filename,
                AllowLocalChange = false,
                FileSize = new FileInfo(filename).Length,
                Changed = new FileInfo(filename).LastWriteTimeUtc
            };

            if (complete)
            {
                byte[] hash = await Task.Run(() => sha256.ComputeHash(File.OpenRead(filename)));
                clientFileInfo.Hash = new byte[hash.Length];
                hash.CopyTo(clientFileInfo.Hash, 0);
            }

            return clientFileInfo;
        }

        public async Task<List<ClientFileInfo>> GetFilesListInfo (List<string> filenames, string localpath, CancellationToken token, bool complete = false)
        {
            List<ClientFileInfo> res = new List<ClientFileInfo>();

            int progress_counter = 0;

            foreach (string filename in filenames)
            {
                if (token.IsCancellationRequested)
                    return res;

                FileCheckerProgress?.Invoke(this, new FileCheckerProgressEventArgs()
                {
                    FilesCount = filenames.Count,
                    CurrentIndex = progress_counter++,
                    FileName = filename
                });

                ClientFileInfo fileinfo = await GetFileInfo(filename, complete);
                fileinfo.FileName = Path.GetRelativePath(localpath, filename);
                res.Add(fileinfo);
            }

            FileCheckerFinish?.Invoke(this, new FileCheckerFinishEventArgs()
            {
                FilesCount = filenames.Count
            });

            return res;
        }

    }

    public class FileCheckerProgressEventArgs : EventArgs
    {
        public int FilesCount;
        public int CurrentIndex;
        public string FileName;
    }

    public class FileCheckerFinishEventArgs : EventArgs
    {
        public int FilesCount;
    }
}
