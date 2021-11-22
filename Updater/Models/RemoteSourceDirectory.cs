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
        private readonly ILogger _logger;
        private readonly SimpleHttpLoader _loader;

        private DirectoryModel _model;

        public RemoteSourceDirectory(ILogger logger, SimpleHttpLoader loader)
        {
            _logger = logger;
            _loader = loader;
        }

        public DirectoryModel Model => _model;

        public async Task LoadRemoteModel(string remoteModelAddr, CancellationToken token)
        {
            string temporaryFileName = Path.GetTempFileName();

            try
            {
                await _loader.DownloadFile(remoteModelAddr, temporaryFileName, token);
                _model = await DirectoryModel.ReadAsync(temporaryFileName);
            }
            finally
            {
                File.Delete(temporaryFileName);
            }
        }
    }
}
