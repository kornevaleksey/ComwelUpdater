using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Linq;
using System.Drawing;

namespace Updater
{
    public class Loader
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        static HttpClient httpClient;

        public Uri RemoteAddr { get; set; }

        private readonly string scheme = "http";

        private readonly int download_tries;

        public Loader(string RemoteAddr, int RemotePort, int download_tries = 5)
        {
            UriBuilder uriBuilder = new UriBuilder
            {
                Scheme = scheme,
                Host = RemoteAddr,
                Port = RemotePort,
            };

            this.RemoteAddr = uriBuilder.Uri;
            this.download_tries = download_tries;

            httpClient = new HttpClient()
            {
                BaseAddress = new Uri(uriBuilder.ToString())
            };
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

        public async Task<bool> DownloadFile (string remoteRelativePath, string localFileName)
        {
            UriBuilder uriBuilder = new UriBuilder(RemoteAddr)
            {
                Path = remoteRelativePath
            };

            //Copy old file to temporary location
            string temporaryOldFile = String.Empty;
            if (File.Exists(localFileName))
            {
                logger.Info("Copy old file to temporary: " + temporaryOldFile);
                temporaryOldFile = Path.GetTempFileName();
                File.Copy(localFileName, temporaryOldFile, true);
            } else
            {
                if (Directory.Exists(new FileInfo(localFileName).Directory.FullName)==false)
                {
                    Directory.CreateDirectory(new FileInfo(localFileName).Directory.FullName);
                }
            }

            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(uriBuilder.ToString());

                if (response.IsSuccessStatusCode)
                {
                    using (FileStream downloadfilestream = new FileStream(localFileName, FileMode.OpenOrCreate))
                    {
                        await response.Content.CopyToAsync(downloadfilestream);
                    }
                    logger.Info(String.Format("Success download to client remote storage on {0}", uriBuilder.ToString()));
                    if (temporaryOldFile!="")
                        File.Delete(temporaryOldFile);
                }
                else
                {
                    throw new FileNotFoundException();
                }

                return true;
            }
            catch (HttpRequestException e)
            {
                logger.Error(e, "File download failed!");
                logger.Info("Copy old file back");
                File.Copy(temporaryOldFile, localFileName, true);
                return false;
            }
        }

        public async Task<List<ClientFileInfo>> DownloadClientFiles (string remoteRelativePath, string LocalPath, List<ClientFileInfo> FilesList, EventHandler<UpdaterProgressEventArgs> ProgressUpdate=null)
        {
            List<ClientFileInfo> failed_loads = new List<ClientFileInfo>();
            FileChecker checker = new FileChecker();
            long filessize = FilesList.Sum(ci => ci.FileSize);
            long downloadsize=0;

            ProgressUpdate?.Invoke(this, new UpdaterProgressEventArgs()
            {
                ProgressMax = filessize,
                ProgressValue = 0,
                InfoStr = "Начинаю скачивание файлов клиента",
                InfoStrColor = Color.Black
            });

            foreach (ClientFileInfo clientFileInfo in FilesList)
            {
                int tries = 0;
                bool file_ok = false;
                string local_filename = LocalPath + "\\" + clientFileInfo.FileName;
                while ((tries<download_tries)&&(file_ok==false))
                {
                    ProgressUpdate?.Invoke(this, new UpdaterProgressEventArgs()
                    {
                        ProgressMax = filessize,
                        ProgressValue = downloadsize,
                        InfoStr = String.Format("Скачиваю файл {0}", clientFileInfo.FileName),
                        InfoStrColor = Color.Black
                    });

                    bool file_loaded = await DownloadFile(remoteRelativePath, local_filename);
                    if (file_loaded)
                    {
                        FileInfo loaded_info = new FileInfo(local_filename);
                        if (loaded_info.Length==clientFileInfo.FileSize)
                        {
                            ClientFileInfo loaded_file_info = await checker.GetFileInfo(local_filename);
                            if (clientFileInfo.Hash.SequenceEqual(loaded_file_info.Hash))
                            {
                                file_ok = true;
                            }
                        }
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

            return failed_loads;
        }
    }
}
