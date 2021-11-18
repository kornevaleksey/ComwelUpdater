using System;
using System.Collections.Generic;
using System.Text;

namespace Updater.Events
{
    public class LoaderUnZipProgressEventArgs : EventArgs
    {
        public string FileName;
        public double Percentage;
    }
}
