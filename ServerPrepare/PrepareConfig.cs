using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ServerPrepare
{
    class PrepareConfig
    {
    }

    class SourceConfig:FolderConfig<SourceFileInfo>
    {
        public SourceConfig(Uri Folder):base(Folder)
        {

        }
    }

    class ServerConfig : FolderConfig<ServerFileInfo>
    {
        public ServerConfig(Uri Folder) : base(Folder)
        {

        }
    }

    class PrepareSettings
    {
        public Uri SourceFolder { get; set; }
        public Uri ServerFolder { get; set; }

        public Uri SettingsFile { get => new Uri (Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\ComwelUpdaterPrepare\settings.json"); }

        public async Task SaveSettings ()
        {
            if (!Directory.Exists(Path.GetDirectoryName(SettingsFile.LocalPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile.LocalPath));

            string settingsjson = JsonSerializer.Serialize(this, new JsonSerializerOptions() { WriteIndented = true });

            await File.WriteAllTextAsync(SettingsFile.LocalPath, settingsjson);
        }

        public async Task ReadSettings()
        {
            if ((!Directory.Exists(Path.GetDirectoryName(SettingsFile.LocalPath))) ||
                 (!File.Exists(SettingsFile.LocalPath)) )
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile.LocalPath));
                SourceFolder = new Uri("");
                ServerFolder = new Uri("");
            } else
            {
                string settingsjson = await File.ReadAllTextAsync(SettingsFile.LocalPath);
                PrepareSettings p = JsonSerializer.Deserialize<PrepareSettings>(settingsjson);
                this.ServerFolder = p.ServerFolder;
                this.SourceFolder = p.SourceFolder;
            }
        }
    }

    public class ServerFileInfo : SourceFileInfo, IL2FileInfo
    {
        public long UnCompressedSize { get; set; }
        public byte[] UnCompressedHash { get; set; }

    }

    public class SourceFileInfo : IL2FileInfo
    {
        public string FileName { get; set; }
        public bool Important { get; set; }
        public bool UserChangeAllow { get; set; }
        public DateTime LastChange { get; set; }
        public long Size { get; set; }
        public byte[] Hash { get; set; }

    }

    public interface IL2FileInfo
    {
        string FileName { get; set; }
        bool Important { get; set; }
        bool UserChangeAllow { get; set; }
        DateTime LastChange { get; set; }
        long Size { get; set; }
        byte[] Hash { get; set; }

        static async Task<IL2FileInfo> DefaultSet(string filename, string basefolder)
        {
            IL2FileInfo l2fileinfo = new SourceFileInfo()
            {
                FileName = Path.GetRelativePath(basefolder, filename),
                Important = false,
                UserChangeAllow = false
            };

            FileInfo inf = new FileInfo(filename);
            l2fileinfo.LastChange = inf.LastWriteTimeUtc;
            l2fileinfo.Size = inf.Length;

            l2fileinfo.Hash = await Task.Run( () => SHA256.Create().ComputeHash(inf.OpenRead()));

            return l2fileinfo;
        }
    }

    public class FolderConfig<T> where T : IL2FileInfo
    {
        public Uri Folder { get; }
        public string ClientFolder { get => Path.Combine(Folder.LocalPath, @"client\"); }
        public string InfoFolder { get => Path.Combine(Folder.LocalPath, @"info\"); }
        public virtual string InfoFile { get => Path.Combine(InfoFolder, "information.json"); }

        public Action<double, string> CreateInfoProgress;
        public Action FinishInfoProgress;

        public virtual List<T> FileInfos { get; private set; }

        public FolderConfig (Uri Folder)
        {
            this.Folder = Folder;
        }

        public async Task ReadInfo()
        {
            if (File.Exists(InfoFile))
            {
                string jsonstr = await File.ReadAllTextAsync(InfoFile);
                FileInfos = JsonSerializer.Deserialize<List<T>>(jsonstr);
            }
        }

        public async Task WriteInfo()
        {
            string jsonstr = JsonSerializer.Serialize(FileInfos, new JsonSerializerOptions() { WriteIndented = true });
            await File.WriteAllTextAsync(InfoFile, jsonstr);
        }

        public async Task InitFolder()
        {
            await Task.Run(() =>
            {
                Directory.CreateDirectory(ClientFolder);
                Directory.CreateDirectory(InfoFolder);
            });

        }

        public async Task CreateInfo()
        {
            List<string> sourcefiles = await Task.Run(() => Directory.GetFiles(Path.Combine(Folder.LocalPath, ClientFolder), "*.*", SearchOption.AllDirectories).ToList());
            FileInfos = new List<T>();

            int filenum = 0;
            foreach (string file in sourcefiles)
            {
                T si =  (T)await IL2FileInfo.DefaultSet(file, ClientFolder);
                FileInfos.Add(si);

                filenum++;
                CreateInfoProgress?.Invoke(100.0*(double)filenum / sourcefiles.Count, String.Format("Обрабатываю файл {0}", file));
            }
            FinishInfoProgress?.Invoke();
        }

    }
}
