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

namespace Updater
{
    public class Loader
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static HttpClient httpClient;

        public Uri RemoteAddr { get=>httpClient.BaseAddress; set=>httpClient.BaseAddress = value; }

        public event EventHandler<LoaderProgressEventArgs> LoaderProgress;

        private readonly int download_tries;

        public Loader(int download_tries = 5)
        {
            httpClient = new HttpClient();

            this.download_tries = download_tries;

            //httpClient.DefaultRequestHeaders.Accept.Clear();
            //httpClient.DefaultRequestHeaders.Add("authorization", access_token); //if any
            //httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<bool> CheckConnect()
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(RemoteAddr.ToString());
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                logger.Info(String.Format("Success connection to client remote storage on {0}", RemoteAddr.ToString()));
                return true;
            }
            catch (HttpRequestException e)
            {
                logger.Error(e, "Connection check failed!");
                return false;
            }
        }

        public async Task DownloadFile (string remoteRelativePath, string localFileName)
        {
            UriBuilder uriBuilder = new UriBuilder(RemoteAddr)
            {
                Path = remoteRelativePath + ".zip"
            };

            logger.Info("Download file {0} into local file {1}", uriBuilder.ToString(), localFileName);


            if (Directory.Exists(new FileInfo(localFileName).Directory.FullName) == false)
            {
                logger.Info("Creating directory {0}", new FileInfo(localFileName).Directory.FullName);
                Directory.CreateDirectory(new FileInfo(localFileName).Directory.FullName);
            }

            using var streamresp = httpClient.GetStreamAsync(uriBuilder.Uri);
            using (var zipreader = ReaderFactory.Open(await streamresp))
            {
                zipreader.MoveToNextEntry();
                await Task.Run(() => zipreader.WriteEntryToFile(localFileName, new ExtractionOptions() { Overwrite = true }));

                logger.Info("File loaded");
            }
        }

        public async Task DownloadClientFiles (string remoteRelativePath, string LocalPath, List<ClientFileInfo> FilesList)
        {
            logger.Info("Start download client {0} files", FilesList.Count);

            List<ClientFileInfo> failed_loads = new List<ClientFileInfo>();
            FileChecker checker = new FileChecker();
            long filessize = FilesList.Sum(ci => ci.FileSize);
            long downloadsize=0;
            double downloadspeed = 0;

            foreach (ClientFileInfo clientFileInfo in FilesList)
            {
                int tries = 0;
                bool file_ok = false;
                string local_filename = LocalPath + "\\" + clientFileInfo.FileName;
                while ((tries<download_tries)&&(file_ok==false))
                {
                    string remote_uri = remoteRelativePath + clientFileInfo.FileName;

                    LoaderProgress?.Invoke(this, new LoaderProgressEventArgs()
                    {
                        Percentage = (double)downloadsize / filessize,
                        FileName = clientFileInfo.FileName,
                        DownloadTry = tries,
                        Speed = downloadspeed
                    }) ;

                    try
                    {
                        DateTime starttime = DateTime.Now;
                        await DownloadFile(remote_uri, local_filename);
                        TimeSpan timeSpan = DateTime.Now - starttime;
                        FileInfo loaded_info = new FileInfo(local_filename);
                        downloadspeed = loaded_info.Length / timeSpan.TotalSeconds;

                        if (loaded_info.Length == clientFileInfo.FileSize)
                        {
                            ClientFileInfo loaded_file_info = await checker.GetFileInfo(local_filename, true);
                            if (clientFileInfo.Hash.SequenceEqual(loaded_file_info.Hash))
                            {
                                file_ok = true;
                            }
                        }
                    }
                    finally
                    {

                    }

                    tries++;
                }

                downloadsize += clientFileInfo.FileSize;

                if (file_ok==false)
                {
                    logger.Info(String.Format("File {0} download error", local_filename));
                    failed_loads.Add(clientFileInfo);
                }
            }

            if (failed_loads.Count > 0)
                throw new LoaderFilesLoadException()
                {
                    Files = failed_loads
                };
        }
    }

    public class LoaderProgressEventArgs : EventArgs
    {
        public string FileName;
        public int DownloadTry;
        public double Percentage;
        public double Speed;
    }

    public class LoaderFilesLoadException : Exception
    {
        public List<ClientFileInfo> Files { get; set; }
    }
}
