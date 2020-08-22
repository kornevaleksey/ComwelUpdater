using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Logger;

namespace Updater
{
    public class FileChecker
    {
        public Dictionary<string, bool> FilesCheck;
        
        protected string clientpath;
        protected string hashespath;
        protected List<string[]> HashList;

        SHA256 sha256;

        private readonly int CalculateThreads;
        private readonly int CalculateTimeout_ms = 5 * 60 * 1000;

        private CommonLogger logger;
        public FileChecker (CommonLogger logger, string clientpath, string hashespath, bool rehash = false)
        {
            this.logger = logger;
            this.clientpath = clientpath;
            this.hashespath = hashespath;

            FilesCheck = new Dictionary<string, bool>();

            HashList = new List<string[]>();
            sha256 = SHA256.Create();

            CalculateThreads = Environment.ProcessorCount * 2;

            if (rehash == true)
                ClientFilesCalculateHashes();
        }

        public async void CheckClientFiles (string remote_HashesListFileName)
        {
            Dictionary<string, string> client_files_hashes = ClientFilesCalculateHashes();

            Dictionary<string, string> remote_hashes = ReadHashes(remote_HashesListFileName);

            var difference = client_files_hashes.Except(remote_hashes);
        }

        public Dictionary<string, string> ClientFilesCalculateHashes()
        {
            //Get all files in client directory
            List<string> all_files = Directory.GetFiles(clientpath, "*.*", SearchOption.AllDirectories).ToList();
            //Calculate files count by hash calculate thread
            int FilesByThread = all_files.Count / CalculateThreads;
            List<Task> running_tasks = new List<Task>();

            //Divide files list to parts and run tasks with hashes calcs for each part
            for (int thread_index = 0; thread_index < CalculateThreads; thread_index++)
            {
                int start_items_index = thread_index * FilesByThread;
                int end_items_index = thread_index == (CalculateThreads - 1) ? all_files.Count - 1 : (thread_index + 1) * FilesByThread;
                List<string> files = all_files.GetRange(start_items_index, end_items_index - start_items_index);
                running_tasks.Add(
                Task.Run<Dictionary<string, string>>(() => CalculateHashes(files))
                );
            }

            Task.WaitAll(running_tasks.ToArray(), CalculateTimeout_ms);

            Dictionary<string, string> client_files_hashes = new Dictionary<string, string>();

            foreach (Task<Dictionary<string, string>> task in running_tasks)
            {
                bool ttt = task.IsCompleted;
                client_files_hashes.Concat(task.Result);
            }

            return client_files_hashes;
        }

        public Dictionary<string, string> CalculateHashes(List<string> FilesNames)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (FilesNames.Count > 0)
            {
                foreach (string filename in FilesNames)
                {
                    byte[] hash = sha256.ComputeHash(File.OpenRead(filename));

                    result.Add ( new Uri(hashespath + "\\").MakeRelativeUri(new Uri(filename)).ToString(),
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
