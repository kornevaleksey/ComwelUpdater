using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Updater
{
    public class FileChecker
    {
        string path;
        List<string[]> HashList;

        SHA256 sha256;
        public FileChecker (string path)
        {
            this.path = path;

            HashList = new List<string[]>();
            sha256 = SHA256.Create();

            string[] dir_files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            TextWriter tw = new StreamWriter(path + "\\Hashes.txt");

            foreach (string filename in dir_files)
            {
                byte[] hash = sha256.ComputeHash(File.OpenRead(filename));

                HashList.Add(new string[]
                {
                    new Uri(path+"\\").MakeRelativeUri(new Uri(filename)).ToString(),
                    Convert.ToBase64String(hash)
                }
                );

                tw.WriteLine(HashList[HashList.Count-1][0] + "     " + HashList[HashList.Count - 1][1]);
            }

            tw.Close();
        }

    }
}
