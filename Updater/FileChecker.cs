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

        public Dictionary<string, bool> FilesCheck;
        
        protected string clientpath;
        protected string hashespath;
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

        public FileChecker (string clientpath, string hashespath, bool rehash = false)
        {
            logger.Info("Creating new FileChecker");
            this.clientpath = clientpath;
            this.hashespath = hashespath;

            FilesCheck = new Dictionary<string, bool>();

            HashList = new List<string[]>();
            sha256 = SHA256.Create();

            if (rehash == true)
                Task.Run<Dictionary<string, string>>(() => ClientFilesCalculateHashes());
            
        }

        public void CheckClientFiles (string remote_HashesListFileName)
        {
            Dictionary<string, string> client_files_hashes = ClientFilesCalculateHashes();

            Dictionary<string, string> remote_hashes = ReadHashes(remote_HashesListFileName);

            var difference = client_files_hashes.Except(remote_hashes);
        }

        public Dictionary<string, string> ClientFilesCalculateHashes()
        {
            //Get all files in client directory
            List<string> all_files = Directory.GetFiles(clientpath, "*.*", SearchOption.AllDirectories).ToList();
            //Calculate client size
            ClientSize = 0;
            foreach (string file in all_files)
                ClientSize += new FileInfo(file).Length;

            CalculateHashes(all_files);

            ProgressUpdate?.Invoke(this, new FileCheckerProgressEventArgs()
            {
                ClientSize = 100,
                HashedSize = 100,
                HashingFileName = "Complete!"
            });

            Dictionary<string, string> client_files_hashes = new Dictionary<string, string>();

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

                    result.Add (new Uri(hashespath + "\\").MakeRelativeUri(new Uri(filename)).ToString(),
                        Convert.ToBase64String(hash) );
                }
            }
            return result;
        }

        public void WriteHashes (Dictionary<string, string> hashes)
        {
            using (TextWriter tw = new StreamWriter(hashespath + "\\Hashes.txt"))
            {
                foreach (var record in hashes)
                {
                    tw.WriteLine(record.Key + ":" + record.Value);
                }
                tw.Close();
            }
        }

        public Dictionary<string, string> ReadHashes(string filename)
        {
            Dictionary<string, string> readhashes = new Dictionary<string, string>();

            using (TextReader tr = new StreamReader(filename))
            {
                string readline = tr.ReadLine();
                while (readline!=null)
                {
                    string[] pair = readline.Split(":", StringSplitOptions.RemoveEmptyEntries);
                    readhashes.Add(pair[0], pair[1]);
                }    
            }

            return readhashes;
        }

    }
}
