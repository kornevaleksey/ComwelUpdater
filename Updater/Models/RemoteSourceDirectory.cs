using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Updater
{
    public class RemoteSourceDirectory
    {
        private readonly ILogger? logger;
        private readonly SimpleHttpLoader loader;
        private readonly string remoteRelativeAddress;

        public RemoteSourceDirectory(ILogger<RemoteSourceDirectory>? logger, SimpleHttpLoader loader, string remoteRelativeAddress)
        {
            this.logger = logger;
            this.loader = loader;
            this.remoteRelativeAddress = remoteRelativeAddress;
        }

        public DirectoryModel? Model { get; private set; }

        public async Task LoadRemoteModelAsync(CancellationToken token)
        {
            string temporaryFileName = Path.GetTempFileName();

            try
            {
                await loader.DownloadFile(remoteRelativeAddress, temporaryFileName, token);
                Model = await DirectoryModel.ReadAsync(temporaryFileName);
            }
            finally
            {
                File.Delete(temporaryFileName);
            }
        }
    }
}
