using System;
using System.Collections.Generic;
using System.Text;

namespace Updater.Events
{
    public class LoaderProgressEventArgs : EventArgs
    {
        public string? FileName;
        public int DownloadTry;
        public double Percentage;
        public double Speed;
    }
}
