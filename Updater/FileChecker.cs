using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Updater
{
    public class FileChecker
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public List<string> FilesRemoteDifferent;
        public Dictionary<string, bool> FilesLocalExtra;

        public string ClientPath { get; set; }
        public string ClientHashesFile { get; set; }
        public string RemoteHashesFile { get; set; }
        protected List<string[]> HashList;

        SHA256 sha256;

        public event EventHandler<FileCheckerProgressEventArgs> ProgressUpdate;

        public class FileCheckerProgressEventArgs : EventArgs
        {
            public long ClientSize { get; set; }
            public long HashedSize { get; set; }
            public string HashingFileName { get; set; }
        }

        private long ClientSize;

        public FileChecker()
        {
            logger.Info("Creating new FileChecker");
            FilesRemoteDifferent = new List<string>();
            HashList = new List<string[]>();
            sha256 = SHA256.Create();
        }

        public FileChecker (string clientpath, string hashespath, EventHandler<FileCheckerProgressEventArgs> ProgressUpdate, bool rehash, string clienthashes)
        {
            logger.Info("Creating new FileChecker");
            this.ClientPath = clientpath;
            this.RemoteHashesFile = hashespath;
            this.ProgressUpdate = ProgressUpdate;
            this.ClientHashesFile = clienthashes;

            FilesRemoteDifferent = new List<string>();
            HashList = new List<string[]>();
            sha256 = SHA256.Create();

            if (rehash == true)
                Task.Run<Dictionary<string, string>>(() => ClientFilesCalculateHashes());
            
        }

        public async Task<bool> CheckClientHashes()
        {
            Dictionary<string, string> remote_hashes = await ReadHashesAsync(RemoteHashesFile);
            Dictionary<string, string> client_hashes = await ReadHashesAsync(ClientHashesFile);

            var difference = remote_hashes.Except(client_hashes);//client_hashes.Except(remote_hashes);

            if (difference.ToList().Count > 0)
            {
                FilesRemoteDifferent.Clear();
                foreach (KeyValuePair<string, string> pair in difference)
                {
                    FilesRemoteDifferent.Add(pair.Key);
                }
                return false;
            }
            else
                return true;

        }

        public void CheckClientFiles ()
        {
            Dictionary<string, string> client_files_hashes = ClientFilesCalculateHashes();

            Dictionary<string, string> remote_hashes = ReadHashes(RemoteHashesFile);

            var difference = client_files_hashes.Except(remote_hashes);
        }

        public Dictionary<string, string> ClientFilesCalculateHashes()
        {
            //Get all files in client directory
            List<string> all_files = Directory.GetFiles(ClientPath, "*.*", SearchOption.AllDirectories).ToList();
            //Calculate client size
            ClientSize = 0;
            foreach (string file in all_files)
                ClientSize += new FileInfo(file).Length;

            Dictionary<string, string> client_files_hashes = CalculateHashes(all_files);

            WriteHashes(client_files_hashes);

            ProgressUpdate?.Invoke(this, new FileCheckerProgressEventArgs()
            {
                ClientSize = 100,
                HashedSize = 100,
                HashingFileName = "Complete!"
            });

            

            return client_files_hashes;
        }

        public Dictionary<string, string> CalculateHashes(List<string> FilesNames)
        {
            long HashedSize = 0;
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (FilesNames.Count > 0)
            {
                foreach (string filename in FilesNames)
                {
                    HashedSize += new FileInfo(filename).Length;

                    byte[] hash = sha256.ComputeHash(File.OpenRead(filename));


                    ProgressUpdate?.Invoke(this, new FileCheckerProgressEventArgs()
                    {
                        ClientSize = this.ClientSize,
                        HashedSize = HashedSize,
                        HashingFileName = filename
                    });

                    result.Add (new Uri(ClientPath + "\\").MakeRelativeUri(new Uri(filename)).ToString(),
                        Convert.ToBase64String(hash) );
                }
            }
            return result;
        }

        public async void WriteHashesAsync (Dictionary<string, string> hashes)
        {
            using (TextWriter tw = new StreamWriter(ClientHashesFile))
            {
                foreach (var record in hashes)
                {
                    await tw.WriteLineAsync(record.Key + ":" + record.Value);
                }
                tw.Close();
            }
        }

        public void WriteHashes (Dictionary<string, string> hashes)
        {
            using (TextWriter tw = new StreamWriter(ClientHashesFile))
            {
                foreach (var record in hashes)
                {
                    tw.WriteLine(record.Key + ":" + record.Value);
                }
                tw.Close();
            }
        }

        public async Task<Dictionary<string, string>> ReadHashesAsync(string filename)
        {
            Dictionary<string, string> readhashes = new Dictionary<string, string>();

            try
            {
                using (TextReader tr = new StreamReader(filename))
                {
                    string readline = await tr.ReadLineAsync();
                    while (readline != null)
                    {
                        string[] pair = readline.Split(":", StringSplitOptions.RemoveEmptyEntries);
                        readhashes.Add(pair[0], pair[1]);
                        readline = await tr.ReadLineAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Read hashes failed");
            }

            return readhashes;
        }

        public Dictionary<string, string> ReadHashes(string filename)
        {
            Dictionary<string, string> readhashes = new Dictionary<string, string>();

            using (TextReader tr = new StreamReader(filename))
            {
                string readline = tr.ReadLine();
                while (readline != null)
                {
                    string[] pair = readline.Split(":", StringSplitOptions.RemoveEmptyEntries);
                    readhashes.Add(pair[0], pair[1]);
                    readline = tr.ReadLine();
                }
            }

            return readhashes;
        }

    }
}
