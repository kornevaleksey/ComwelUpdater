using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Updater
{
    public class DirectoryModel
    {
        public DateTime Changed { get; set; }
        public uint FilesCount { get; set; }
        public long ClientSize { get; set; }
        public long ClientCompressedSize { get; set; }
        public List<ClientFileInfo> FilesInfo { get; set; }

        public static async Task WriteAsync(string filename, DirectoryModel model)
        {
            var serlist = JsonSerializer.Serialize(model);
            await File.WriteAllTextAsync(filename, serlist);
        }

        /// <summary>
        /// Считать модель директории из файла
        /// </summary>
        /// <param name="filename"></param>
        /// <exception cref="FileNotFoundException"/>
        /// <returns></returns>
        public static async Task<DirectoryModel> ReadAsync(string filename)
        {
            string modelStr = await File.ReadAllTextAsync(filename);
            return JsonSerializer.Deserialize<DirectoryModel>(modelStr);
        }
    }
}
