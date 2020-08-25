using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;

namespace Updater
{
    public class Loader
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        static HttpClient httpClient;

        public string RemoteAddr { get; set; }
        public int RemotePort { get; set; }
        public string RemoteHashesFile { get; private set; }
        public string LocalHashesFile { get; private set; }

        private readonly string scheme = "http";

        public string ClientPath { get => "client"; }

        public Loader(string RemoteAddr, int RemotePort)
        {
            UriBuilder uriBuilder = new UriBuilder
            {
                Scheme = scheme,
                Host = RemoteAddr,
                Port = RemotePort,
            };

            httpClient = new HttpClient()
            {
                BaseAddress = new Uri(uriBuilder.ToString())
            };
            //httpClient.DefaultRequestHeaders.Accept.Clear();
            //httpClient.DefaultRequestHeaders.Add("authorization", access_token); //if any
            //httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            this.RemoteAddr = RemoteAddr;
            this.RemotePort = RemotePort;

            RemoteHashesFile = "hashes/hashes.txt";
        }

        public async Task<bool> CheckConnect()
        {
            UriBuilder uriBuilder = new UriBuilder
            {
                Scheme = scheme,
                Host = RemoteAddr,
                Port = RemotePort,
            };

            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(uriBuilder.ToString());
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                logger.Info(String.Format("Success connection to client remote storage on {0}", RemoteAddr));
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
            UriBuilder uriBuilder = new UriBuilder
            {
                Scheme = scheme,
                Host = RemoteAddr,
                Port = RemotePort,
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
                    logger.Info(String.Format("Success download to client remote storage on {0}", RemoteAddr));
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

        public async Task<bool> DownloadHashes ()
        {
            LocalHashesFile = Path.GetTempFileName();
            return await DownloadFile(RemoteHashesFile, LocalHashesFile);
        }

    }
}
