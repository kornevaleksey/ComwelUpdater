using System;
using System.IO;

namespace Logger
{
    public class CommonLogger
    {
        protected string FileName;
        protected StreamWriter logStream;

        public CommonLogger(string LogsFolder)
        {
            try
            {
                if (Directory.Exists(LogsFolder) == false)
                    Directory.CreateDirectory(LogsFolder);
                FileName = LogsFolder+"//L2Updater_Log_" + DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".log";
                logStream = new StreamWriter(FileName, false);
                Log("Start L2 Updater Logs "+DateTime.Now.ToString());
            }
            catch (Exception ex)
            {

            }
        }

        public void Log(string log)
        {
            logStream.WriteLine(log);
            logStream.Flush();
        }

    }
}
