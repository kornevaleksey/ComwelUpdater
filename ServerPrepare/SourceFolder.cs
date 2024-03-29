﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using ServerPrepare.FilesInfo;


namespace ServerPrepare.Process
{
    public class SourceFolder : FolderConfig
    {

        public Action<double, string> CompressProgress;
        public Action FinishCompress;

        public Action<double, string> CreateInfoProgress;
        public Action CreateInfoFinish;

        public virtual List<SourceFileInfo> FileInfos { get; private set; }

        public SourceFolder(Uri Folder) : base(Folder)
        {

        }

        public async void StartCompress(ServerFolder serverfolder)
        {
            List<string> folderfiles = await Task.Run(() => Directory.GetFiles(this.ClientFolder, "*.*", SearchOption.AllDirectories).ToList());
            List<string> subfolders = await Task.Run(() => Directory.GetDirectories(this.ClientFolder).Select(s => Path.GetRelativePath(this.ClientFolder, s)).ToList());

            string crsf = Path.Combine(serverfolder.ClientFolder, subfolders[0]);
            Directory.CreateDirectory(crsf);

            await Task.Run(() => subfolders.ForEach(sf =>
            {
                string crsf = Path.Combine(serverfolder.ClientFolder, sf);
                Directory.CreateDirectory(crsf);
            }));

            int fcount = folderfiles.Count;

            await folderfiles.ParallelForEachAsync(async (sourcefile, index) =>
            {
                string relpath = Path.GetRelativePath(this.ClientFolder, sourcefile);
                string destfile = Path.Combine(serverfolder.ClientFolder, relpath);

                CompressProgress?.Invoke(100.0 * index/fcount, String.Format("Сжатие файла {0}", sourcefile));

                await CompressFileAsync(sourcefile, destfile);

            }, 4);

            FinishCompress?.Invoke();
        }

        public async Task CompressFilesList (List<SourceFileInfo> files, string destination)
        {
            List<string> compressfiles = files.Select(s => Path.Combine(this.ClientFolder, s.FileName)).ToList();

            await compressfiles.ParallelForEachAsync(async (sourcefile, index) =>
            {
                string relpath = Path.GetRelativePath(this.ClientFolder, sourcefile);
                string destfile = Path.Combine(destination, relpath);

                CompressProgress?.Invoke(100.0 * compressfiles.Count / index, String.Format("Сжатие файла {0}", sourcefile));

                await CompressFileAsync(sourcefile, destfile);

            }, 4);
        }

        public async void ReadInfo()
        {
            if (File.Exists(InfoFile))
            {
                string jsonstr = await File.ReadAllTextAsync(InfoFile);
                FileInfos = JsonSerializer.Deserialize<List<SourceFileInfo>>(jsonstr);
            }
        }

        public async Task WriteInfo()
        {
            string jsonstr = JsonSerializer.Serialize(FileInfos, new JsonSerializerOptions() { WriteIndented = true });
            await File.WriteAllTextAsync(InfoFile, jsonstr);
        }

        public async Task CreateInfo()
        {
            List<string> sourcefiles = await Task.Run(() => Directory.GetFiles(Path.Combine(Folder.LocalPath, ClientFolder), "*.*", SearchOption.AllDirectories).ToList());
            FileInfos = new List<SourceFileInfo>();

            int filenum = 0;
            foreach (string file in sourcefiles)
            {
                SourceFileInfo si = (SourceFileInfo)await SourceFileInfo.DefaultSet(file, ClientFolder);
                FileInfos.Add(si);

                filenum++;
                CreateInfoProgress?.Invoke(100.0 * (double)filenum / sourcefiles.Count, String.Format("Обрабатываю файл {0}", file));
            }
            CreateInfoFinish?.Invoke();
        }
    }
}
