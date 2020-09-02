﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Updater
{
    public class FileChecker
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
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

    }
}
