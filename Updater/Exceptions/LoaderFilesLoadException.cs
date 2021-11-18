using System;
using System.Collections.Generic;
using System.Text;

namespace Updater.Exceptions
{
    public class LoaderFilesLoadException : Exception
    {
        public List<ClientFileInfo> Files { get; set; }
    }
}
