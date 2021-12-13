using Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Launcher.Models
{
    public class GameLauncher
    {
        public event EventHandler<bool>? CanLaunchChanged;
        public event EventHandler? GameInstancesClosed;

        private readonly UpdaterConfig config;
        private readonly Timer timer;

        public GameLauncher(UpdaterConfig config)
        {
            this.config = config;

            CanLaunch = File.Exists(config.ClientExeFile);

            var processes = SearchForRunningLineageProcesses();
            foreach (var process in processes)
            {
                process.Exited += OnExited;
            }

            timer = new Timer(CheckExecutable, null, 1000, 1000);
        }

        private bool canLaunch;
        public bool CanLaunch 
        { 
            get => canLaunch; 
            private set
            {
                canLaunch = value;
                CanLaunchChanged?.Invoke(this, value);
            }
        }

        public void Launch()
        {
            CanLaunch = false;

            ProcessStartInfo l2info = new();
            l2info.EnvironmentVariables["__COMPAT_LAYER"] = "RunAsInvoker";
            l2info.FileName = config.ClientExeFile;

            Process l2Run = new()
            {
                EnableRaisingEvents = true,
                StartInfo = l2info
            };
            l2Run.Exited += OnExited;

            l2Run.Start();
        }

        private void OnExited(object? sender, EventArgs e)
        {
            var processes = SearchForRunningLineageProcesses();
            if (processes.Count() == 0)
            {
                CanLaunch = true;
                GameInstancesClosed?.Invoke(this, EventArgs.Empty);
            }
        }

        private IEnumerable<Process> SearchForRunningLineageProcesses()
        {
            return Process.GetProcessesByName("l2").Where(p => p.MainModule.FileName.Equals(config.ClientExeFile));
        }

        private void CheckExecutable(object? obj)
        {
            CanLaunch = File.Exists(config.ClientExeFile);
        }

    }
}
