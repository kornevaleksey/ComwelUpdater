using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Config
{
    public class Configurator
    {
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly ILogger _logger;

        private static UpdaterConfig? updaterConfig;

        public Configurator(ILogger logger)
        {
            _logger = logger;

            _jsonSerializerOptions = new JsonSerializerOptions()
            {
                WriteIndented = true
            };
        }

        public async Task<UpdaterConfig?> ReadAsync(UpdaterConfig? defaultConfig = null)
        {
            if (updaterConfig != null)
            {
                return updaterConfig;
            }

            UpdaterConfig? _config;

            string fullName = Path.Combine(ConfigDirectory, nameof(UpdaterConfig), ".json");

            if (!File.Exists(fullName) && defaultConfig != null)
            {
                return defaultConfig;
            }

            _logger.LogInformation($"Start read updater config from file {fullName}");

            try
            {
                _config = await JsonSerializer.DeserializeAsync<UpdaterConfig>(File.Open(fullName, FileMode.Open, FileAccess.Read));
                _logger.LogInformation($"Config from file {fullName} succefully readed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing json file!");
                throw;
            }

            updaterConfig = _config;

            return _config;
        }

        public async Task WriteAsync(UpdaterConfig config)
        {
            string fullName = Path.Combine(_configPath, nameof(UpdaterConfig), ".json");
            string jsonConfig = JsonSerializer.Serialize(config, _jsonSerializerOptions);
            await File.WriteAllTextAsync(fullName, jsonConfig);
        }
    }
}
