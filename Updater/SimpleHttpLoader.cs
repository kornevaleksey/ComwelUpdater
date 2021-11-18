using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Drawing;
using SharpCompress.Common;
using SharpCompress.Writers;
using SharpCompress.Readers;
using System.Net.NetworkInformation;
using System.Threading;
using Microsoft.Extensions.Logging;
using Updater.Exceptions;
using Updater.Events;

namespace Updater
{
    public class SimpleHttpLoader
    {
        public event EventHandler<LoaderProgressEventArgs> LoaderProgress;
        public event EventHandler<LoaderUnZipProgressEventArgs> UnZipProgress;
        public event EventHandler<LoaderConnectionCheckEventArgs> ConnectionCheck;

        private readonly ILogger _logger;
        private readonly FileChecker _fileChecker;

        private string loadFileName;
        private CancellationToken token;
        private Stream stream;

        private static HttpClient httpClient;

        private readonly int _downloadTries;

        public SimpleHttpLoader(ILogger<SimpleHttpLoader> logger, FileChecker fileChecker, int downloadTries = 5)
        {
            _logger = logger;
            httpClient = new HttpClient();
            _fileChecker = fileChecker;

            _downloadTries = downloadTries;
        }

        public Uri RemoteAddr { get; set; }
        public Uri RemoteInfoAddr { get; set; }

        public async Task<bool> CheckConnect()
        {
            HttpResponseMessage response = await httpClient.GetAsync(RemoteAddr.ToString());
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Success connection to remote storage on {RemoteAddr}");
            return true;
        }

        public async Task DownloadFile (string remoteRelativePath, string localFileName, CancellationToken token)
        {
            UriBuilder uriBuilder = new UriBuilder(RemoteAddr)
            {
                Path = remoteRelativePath + ".zip"
            };
            loadFileName = uriBuilder.Uri.ToString();

            _logger.LogInformation($"Download file {uriBuilder} into local file {localFileName}");

            FileInfo fileInfo = new FileInfo(localFileName);

            if (!fileInfo.Directory.Exists)
            {
                _logger.LogInformation($"Creating directory {0}", new FileInfo(localFileName).Directory.FullName);
                fileInfo.Directory.Create();
            }

            using var streamlocalfile = File.Create(localFileName);
            using var streamresp = httpClient.GetStreamAsync(uriBuilder.Uri);
            using var zipreader = ReaderFactory.Open(await streamresp);
            stream = streamlocalfile;
            this.token = token;

            EventHandler<ReaderExtractionEventArgs<IEntry>> eventDelegate = (sender, args) =>
            {
                if (token.IsCancellationRequested)
                {
                    stream.Close();
                }
                else
                {
                    UnZipProgress?.Invoke(this, new LoaderUnZipProgressEventArgs()
                    {
                        Percentage = args.ReaderProgress.PercentageReadExact,
                        FileName = loadFileName,
                    });
                }
            };

            zipreader.EntryExtractionProgress += eventDelegate;

            zipreader.MoveToNextEntry();
            await Task.Run(() => zipreader.WriteEntryTo(streamlocalfile), token);

            zipreader.EntryExtractionProgress -= eventDelegate;

            _logger.LogInformation($"File {loadFileName} loaded into {localFileName}");
        }

        public async Task DownloadClientFiles (string remoteRelativePath, string localPath, IReadOnlyList<ClientFileInfo> filesList, CancellationToken token)
        {
            _logger.LogInformation($"Start download client {filesList.Count} files");

            List<ClientFileInfo> failed_loads = new List<ClientFileInfo>();
            long filessize = filesList.Sum(ci => ci.FileSize);
            long downloadsize=0;
            double downloadspeed = 0;

            foreach (ClientFileInfo clientFileInfo in filesList)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                int tries = 0;
                bool file_ok = false;
                string local_filename = Path.Combine(localPath, clientFileInfo.FileName);
                while ((tries<_downloadTries)&&(file_ok==false))
                {
                    string remote_uri = remoteRelativePath + clientFileInfo.FileName;

                    LoaderProgress?.Invoke(this, new LoaderProgressEventArgs()
                    {
                        Percentage = (double)downloadsize / filessize,
                        FileName = clientFileInfo.FileName,
                        DownloadTry = tries,
                        Speed = downloadspeed
                    });

                    try
                    {
                        DateTime starttime = DateTime.Now;
                        await DownloadFile(remote_uri, local_filename, token);
                        TimeSpan timeSpan = DateTime.Now - starttime;
                        FileInfo loaded_info = new FileInfo(local_filename);
                        downloadspeed = loaded_info.Length / timeSpan.TotalSeconds;

                        ClientFileInfo loaded_file_info = await _fileChecker.GetFileInfo(local_filename, true);
                        if (clientFileInfo.Hash.SequenceEqual(loaded_file_info.Hash))
                        {
                            file_ok = true;
                        }
                        tries++;
                    }
                    finally
                    {
                    }
                }

                if (file_ok==false)
                {
                    _logger.LogError($"File {local_filename} download error");
                    failed_loads.Add(clientFileInfo);
                } else
                {
                    downloadsize += clientFileInfo.FileSize;
                }
            }

            if (failed_loads.Count > 0)
            {
                throw new LoaderFilesLoadException()
                {
                    Files = failed_loads
                };
            }
        }
    }
}
