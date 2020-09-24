using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Updater;
using System.IO.Compression;
using System.Drawing;
using System.Security.Cryptography;
using Dasync.Collections;
using SharpCompress.Common;
using SharpCompress.Writers;

namespace ServerPrepare
{
    public class FolderProcess
    {
        public EventHandler<FolderProgressEventArgs> ProgressUpdate;

        readonly SHA256 sha256 = SHA256.Create();

        readonly string sourcefolder;
        readonly string outputfolder;

        public FolderProcess(string sourcefolder, string outputfolder)
        {
            this.sourcefolder = sourcefolder;
            this.outputfolder = outputfolder;
        }

        public async Task StartCompress()
        {
            List<string> folderfiles = await Task.Run(() => Directory.GetFiles(sourcefolder, "*.*", SearchOption.AllDirectories).ToList());
            List<string> subfolders = await Task.Run(() => Directory.GetDirectories(sourcefolder).Select( s => Path.GetRelativePath(sourcefolder, s)).ToList());

            string crsf = Path.Combine(outputfolder, subfolders[0]);
            Directory.CreateDirectory(crsf);

            await Task.Run(() => subfolders.ForEach(sf =>
            {
                string crsf = Path.Combine(outputfolder, sf);
                Directory.CreateDirectory(crsf);
            }));

            int fcount = folderfiles.Count;

            await folderfiles.ParallelForEachAsync(async (sourcefile, index) =>
            {
                string relpath = Path.GetRelativePath(sourcefolder, sourcefile);
                string destfile = Path.Combine(outputfolder, relpath);

                ProgressUpdate?.Invoke(this, new FolderProgressEventArgs()
                {
                    ProgressMax = fcount,
                    ProgressValue = index,
                    InfoStr = String.Format("Сжатие файла {0}", sourcefile),
                    InfoStrColor = Color.Black
                });

                await CompressFileAsync(sourcefile, destfile);
 
            }, 8);
        }

        public async Task<ClientFileInfo> GetFileInfo(string filename, string sourcepath, string compressedpath)
        {
            ClientFileInfo clientFileInfo = new ClientFileInfo
            {
                FileName = filename,
                AllowLocalChange = false,
                FileSize = new FileInfo(filename).Length,
                FileSizeCompressed = new FileInfo(Path.Combine(compressedpath, Path.GetRelativePath(sourcepath, filename))+".zip").Length,
                Changed = new FileInfo(filename).LastWriteTimeUtc
            };

            byte[] hash = await Task.Run(() => sha256.ComputeHash(File.OpenRead(filename)));
            clientFileInfo.Hash = new byte[hash.Length];
            hash.CopyTo(clientFileInfo.Hash, 0);

            return clientFileInfo;
        }

        public async Task<List<ClientFileInfo>> MakeFilesInfo(List<string> filenames, string sourcepath, string compressedpath, EventHandler<FolderProgressEventArgs> ProgressUpdate = null)
        {
            List<ClientFileInfo> res = new List<ClientFileInfo>();

            int progress_counter = 0;

            foreach (string filename in filenames)
            {
                ProgressUpdate?.Invoke(this, new FolderProgressEventArgs()
                {
                    ProgressMax = filenames.Count,
                    ProgressValue = progress_counter++,
                    InfoStr = String.Format("Обработка файла {0}", filename),
                    InfoStrColor = Color.Black
                });

                ClientFileInfo fileinfo = await GetFileInfo(filename, sourcepath, compressedpath);
                fileinfo.FileName = Path.GetRelativePath(sourcepath, filename);
                res.Add(fileinfo);
            }

            return res;
        }

        public async Task StartModel()
        {
            List<string> sourcefiles = Directory.GetFiles(sourcefolder, "*.*", SearchOption.AllDirectories).ToList();
            List<string> outputfiles = Directory.GetFiles(outputfolder, "*.*", SearchOption.AllDirectories).ToList();
            long compress_size = outputfiles.Select(of => new FileInfo(of).Length).Sum();

            List<ClientFileInfo> clientinfo = await MakeFilesInfo(sourcefiles, sourcefolder, outputfolder, ProgressUpdate);

            ClientModel ClientInfo = new ClientModel()
            {
                Changed = DateTime.Now,
                FilesCount = (uint)sourcefiles.Count,
            };

            ClientInfo.FilesInfo = clientinfo;
            ClientInfo.ClientSize = ClientInfo.FilesInfo.Sum(ci => ci.FileSize);
            ClientInfo.ClientCompressedSize = compress_size;

            var serlist = JsonSerializer.Serialize(ClientInfo, new JsonSerializerOptions() { WriteIndented=true });
            await File.WriteAllTextAsync(sourcefolder + "\\clientinfo.inf", serlist);
        }

        public async Task CompressFileAsync(string sourcename, string destinationname)
        {
            using FileStream compressFileStream = File.Create(destinationname+".zip");
            using var compress_writer = WriterFactory.Open(compressFileStream, ArchiveType.Zip, CompressionType.LZMA);
            await Task.Run(() => compress_writer.Write(Path.GetFileName(sourcename), sourcename));
            /*
            using GZipStream compressionStream = new GZipStream(compressedFileStream, CompressionLevel.Optimal);
            await originalFileStream.CopyToAsync(compressionStream);
            */
        }

        public void CompressFile(string sourcename, string destinationname)
        {
            var compress_writer = WriterFactory.Open(File.Create(destinationname), ArchiveType.Zip, CompressionType.LZMA);
            compress_writer.Write(Path.GetFileName(sourcename), new FileInfo(sourcename));

            /*
            using FileStream originalFileStream = File.OpenRead(sourcename);
            using FileStream compressedFileStream = File.Create(destinationname + ".gz");
            using GZipStream compressionStream = new GZipStream(compressedFileStream, CompressionLevel.Optimal);
            originalFileStream.CopyTo(compressionStream);
            */
        }
    }

    public class FolderProgressEventArgs:EventArgs
    {
        public long ProgressMax;
        public long ProgressValue;
        public string InfoStr;
        public Color InfoStrColor;
    }
}
