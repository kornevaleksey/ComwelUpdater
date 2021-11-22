using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Updater
{
    class FileListEnumerator : IEnumerable<FileInfo>
    {
        public DirectoryInfo Dir { get; }

        public FileListEnumerator(DirectoryInfo dir)
        {
            Dir = dir;
        }

        public IEnumerator<FileInfo> GetEnumerator()
        {
            IEnumerable<FileInfo> list;

            try
            {
                list = Dir.EnumerateFiles("*", SearchOption.TopDirectoryOnly);
            }
            catch (UnauthorizedAccessException)
            {
                yield break;
            }
            catch (PathTooLongException)
            {
                yield break;
            }
            catch (IOException)
            {
                yield break;
            }

            foreach (var file in list)
                yield return file;

            foreach (var dir in Dir.EnumerateDirectories())
            {
                var fileslist = new FileListEnumerator(dir);
                foreach (var file in fileslist)
                    yield return file;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
