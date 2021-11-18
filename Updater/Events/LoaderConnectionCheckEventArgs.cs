using System;
using System.Collections.Generic;
using System.Text;

namespace Updater.Events
{
    public class LoaderConnectionCheckEventArgs : EventArgs
    {
        public Uri RemoteAddr;
        public Exception CheckException;
    }
}
