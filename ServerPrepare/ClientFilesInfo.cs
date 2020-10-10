using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Reflection;
using System.Xml;
using SharpCompress.Common;
using SharpCompress.Writers;

namespace ServerPrepare.FilesInfo
{
    public class SourceFileInfo
    {
        public string FileName { get; set; }
        public bool Important { get; set; }
        public bool UserChangeAllow { get; set; }
        public DateTime LastChange { get; set; }
        public long Size { get; set; }
        public byte[] Hash { get; set; }

        public static async Task<SourceFileInfo> DefaultSet(string filename, string basefolder)
        {
            string relativepath = Path.GetRelativePath(basefolder, filename);
            SourceFileInfo l2fileinfo = new SourceFileInfo()
            {
                FileName = relativepath,
                Important = false,
                UserChangeAllow = false
            };

            if (DefaultFilesInfo.AllowUserEditNames.FindIndex(n => n.Equals(relativepath, StringComparison.InvariantCultureIgnoreCase)) >= 0)
                l2fileinfo.UserChangeAllow = true;

            if (DefaultFilesInfo.ImportantFileNames.FindIndex(n => n.Equals(relativepath, StringComparison.OrdinalIgnoreCase)) >= 0)
                l2fileinfo.Important = true;

            FileInfo inf = new FileInfo(filename);
            l2fileinfo.LastChange = inf.LastWriteTimeUtc;
            l2fileinfo.Size = inf.Length;

            l2fileinfo.Hash = await Task.Run(() => SHA256.Create().ComputeHash(inf.OpenRead()));

            return l2fileinfo;
        }

    }

    static class DefaultFilesInfo
    {
        static DefaultFilesInfo()
        {
            ImportantFileNames = new List<string>();
            AllowUserEditNames = new List<string>();

            XmlDocument clientfileattributes = new XmlDocument();
            var assembly = Assembly.GetExecutingAssembly();

            string infofileresourcename = assembly.GetManifestResourceNames().First(r => r.Contains("ClientFilesAttributes"));
            string xmlstring;
            using (Stream xmlfile = assembly.GetManifestResourceStream(infofileresourcename))
            using (StreamReader reader = new StreamReader(xmlfile))
                xmlstring = reader.ReadToEnd();

            clientfileattributes.LoadXml(xmlstring);
            foreach (XmlNode node in clientfileattributes.DocumentElement.ChildNodes)
            {
                switch (node.Name)
                {
                    case "AllowUserEdit":
                        foreach (XmlNode cnode in node.ChildNodes)
                        {
                            AllowUserEditNames.Add(FormatPath(cnode.InnerText));
                        }
                        break;
                    case "ImportantFiles":
                        foreach (XmlNode cnode in node.ChildNodes)
                        {
                            ImportantFileNames.Add(FormatPath(cnode.InnerText));
                        }
                        break;
                }
            }
        }

        private static string FormatPath(string path)
        {
            string result = path.Replace("/", "\\");
            result = result.TrimStart('\'');
            return result;
        }

        public static List<string> ImportantFileNames;
        public static List<string> AllowUserEditNames;
    }

    public class FolderConfig
    {
        public Uri Folder { get; set; }
        public string ClientFolder { get => Path.Combine(Folder.LocalPath, @"client\"); }
        public string InfoFolder { get => Path.Combine(Folder.LocalPath, @"info\"); }
        public virtual string InfoFile { get => Path.Combine(InfoFolder, "information.json"); }


        public FolderConfig(Uri Folder)
        {
            this.Folder = Folder;
        }

        public async Task InitFolder()
        {
            await Task.Run(() =>
            {
                Directory.CreateDirectory(ClientFolder);
                Directory.CreateDirectory(InfoFolder);
            });

        }

        protected async Task CompressFileAsync(string sourcename, string destinationname)
        {
            using FileStream compressFileStream = File.Create(destinationname + ".zip");
            using var compress_writer = WriterFactory.Open(compressFileStream, ArchiveType.Zip, CompressionType.LZMA);
            await Task.Run(() => compress_writer.Write(Path.GetFileName(sourcename), sourcename));
        }

    }
}
