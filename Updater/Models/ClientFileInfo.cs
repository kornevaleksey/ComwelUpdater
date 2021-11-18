using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Updater
{
    public class ClientFileInfo
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public long FileSizeCompressed { get; set; }
        public DateTime Changed { get; set; }
        public byte[] Hash { get; set; }
        public bool AllowLocalChange { get; set; }
        public bool ImportantFile { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is ClientFileInfo info)
            {
                if (FileName.Equals(info.FileName) &&
                    FileSize == info.FileSize &&
                    Hash.SequenceEqual(info.Hash))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(ClientFileInfo a, ClientFileInfo b)
        {
            if (a is null)
                return b is null;
            else
                return a.Equals(b);
        }

        public static bool operator !=(ClientFileInfo a, ClientFileInfo b)
        {
            if (a is null)
                return !(b is null);
            else
                return !a.Equals(b);
        }
    }
}
