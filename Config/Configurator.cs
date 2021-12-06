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
        public event EventHandler<UpdaterConfig>? ConfigurationUpdated;

        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly ILogger? _logger;

        private readonly string configDirectory;

        public Configurator(ILogger<Configurator>? logger, string configDirectory)
        {
            _logger = logger;

            _jsonSerializerOptions = new JsonSerializerOptions()
            {
                WriteIndented = true
            };

            this.configDirectory = configDirectory;
        }

        public UpdaterConfig? Read(UpdaterConfig? defaultConfig = null)
        {
            UpdaterConfig? _config;

            string fullName = Path.Combine(configDirectory, nameof(UpdaterConfig)) + ".json";

            if (!File.Exists(fullName))
            {
                _config = defaultConfig;
                if (_config == null)
                {
                    _config = new UpdaterConfig();
                }

                Write(_config);
                return _config;
            }

            _logger?.LogInformation($"Start read updater config from file {fullName}");

            try
            {
                string text = File.ReadAllText(fullName);
                _config = JsonSerializer.Deserialize<UpdaterConfig>(text);
                _logger?.LogInformation($"Config from file {fullName} succefully readed");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error parsing json file!");
                throw;
            }

            if (_config == null)
            {
                _logger?.LogError($"Error parsing json file - config is null!");
                throw new FileLoadException(fullName);
            }

            ConfigurationUpdated?.Invoke(this, _config);
            return _config;
        }

        public async Task WriteAsync(UpdaterConfig config)
        {
            string fullName = Path.Combine(configDirectory, nameof(UpdaterConfig))+".json";
            _logger?.LogInformation($"Start write updater config to file {fullName}");

            string jsonConfig = JsonSerializer.Serialize(config, _jsonSerializerOptions);
            await File.WriteAllTextAsync(fullName, jsonConfig);

            ConfigurationUpdated?.Invoke(this, config);
        }

        public void Write(UpdaterConfig config)
        {
            string fullName = Path.Combine(configDirectory, nameof(UpdaterConfig)) + ".json";
            _logger?.LogInformation($"Start write updater config to file {fullName}");

            string jsonConfig = JsonSerializer.Serialize(config, _jsonSerializerOptions);
            File.WriteAllText(fullName, jsonConfig);

            ConfigurationUpdated?.Invoke(this, config);
        }
    }
}
