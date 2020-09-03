using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Linq;
using System.Drawing;

namespace Updater
{
    public class Loader
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static HttpClient httpClient;

        public Uri RemoteAddr { get=>httpClient.BaseAddress; set=>httpClient.BaseAddress = value; }

        public event EventHandler<UpdaterProgressEventArgs> ProgressUpdate;

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

        public async Task<bool> DownloadFile (string remoteRelativePath, string localFileName, long offset = 0, long maxsize = 0)
        {
            UriBuilder uriBuilder = new UriBuilder(RemoteAddr)
            {
                Path = remoteRelativePath
            };

            /*
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
            }*/

            if (Directory.Exists(new FileInfo(localFileName).Directory.FullName) == false)
            {
                Directory.CreateDirectory(new FileInfo(localFileName).Directory.FullName);
            }

            WebClient webclient = new WebClient();
            DateTime start_dl = DateTime.Now;

            webclient.DownloadProgressChanged += (object sender, DownloadProgressChangedEventArgs e) => 
            {
                if (maxsize > 0)
                {
                    double dl_speed = e.BytesReceived / (DateTime.Now - start_dl).TotalSeconds;
                    double time2finish = (maxsize - offset - e.BytesReceived) / dl_speed;
                    ProgressUpdate?.Invoke(this, new UpdaterProgressEventArgs()
                    {
                        ProgressMax = maxsize,
                        ProgressValue = offset + e.BytesReceived,
                        InfoStr = String.Format("Скачиваю файл {0}, \n скорость {1:F2} КБ/с \n Оставшееся время скачивания {2:F2} минут", remoteRelativePath, dl_speed/1024, time2finish/60),
                        InfoStrColor = Color.Black
                    });
                } else
                {
                    ProgressUpdate?.Invoke(this, new UpdaterProgressEventArgs()
                    {
                        ProgressMax = 100,
                        ProgressValue = e.ProgressPercentage,
                        InfoStr = String.Format("Скачиваю файл {0}", remoteRelativePath),
                        InfoStrColor = Color.Black
                    });
                }
            };

            try
            {
                await webclient.DownloadFileTaskAsync(uriBuilder.Uri, localFileName);
                return true;
            }
            catch (WebException webex)
            {
                logger.Error(webex, "File {0} download to {1} failed!", uriBuilder.Uri.ToString(), localFileName);
                return false;
            }

            /*
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
                return false;
            }
            */
        }

        public async Task<List<ClientFileInfo>> DownloadClientFiles (string remoteRelativePath, string LocalPath, List<ClientFileInfo> FilesList)
        {
            List<ClientFileInfo> failed_loads = new List<ClientFileInfo>();
            FileChecker checker = new FileChecker();
            long filessize = FilesList.Sum(ci => ci.FileSize);
            long downloadsize=0;

            foreach (ClientFileInfo clientFileInfo in FilesList)
            {
                int tries = 0;
                bool file_ok = false;
                string local_filename = LocalPath + "\\" + clientFileInfo.FileName;
                while ((tries<download_tries)&&(file_ok==false))
                {
                    /*
                    ProgressUpdate?.Invoke(this, new UpdaterProgressEventArgs()
                    {
                        ProgressMax = filessize,
                        ProgressValue = downloadsize,
                        InfoStr = String.Format("Скачиваю файл {0}", clientFileInfo.FileName),
                        InfoStrColor = Color.Black
                    });
                    */

                    string remote_uri = remoteRelativePath + clientFileInfo.FileName;
                    bool file_loaded = await DownloadFile(remote_uri, local_filename, downloadsize, filessize);
                    if (file_loaded)
                    {
                        FileInfo loaded_info = new FileInfo(local_filename);
                        if (loaded_info.Length==clientFileInfo.FileSize)
                        {
                            ClientFileInfo loaded_file_info = await checker.GetFileInfo(local_filename, true);
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

    public class RemoteModelException : Exception
    {
        public Uri RemoteAddr { get; set; }
        public string RemoteFile { get; set; }
    }
}
