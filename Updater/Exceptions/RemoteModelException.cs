using System;
using System.Collections.Generic;
using System.Text;

namespace Updater.Exceptions
{
    public class RemoteModelException : Exception
    {
        public Uri RemoteAddr { get; set; }
        public string RemoteFile { get; set; }
    }
}
