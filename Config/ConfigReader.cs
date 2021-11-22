using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Config
{
    public class ConfigReader<T> where T : class
    {
        private readonly string _configPath;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly ILogger _logger;
        private T? _config;

        public ConfigReader(ILogger<ConfigReader<T>> logger, string configPath)
        {
            _logger = logger;
            _configPath = configPath;
            _config = null;

            _jsonSerializerOptions = new JsonSerializerOptions()
            {
                WriteIndented = true
            };
        }

        public async Task<T?> ReadAsync(string configFile, T? defaultConfig = null)
        {
            if (_config != null)
            {
                return _config;
            }

            string fullName = Path.Combine(_configPath, configFile);

            if (!File.Exists(fullName) && defaultConfig != null)
            {
                return defaultConfig;
            }

            _logger.LogInformation($"Start read config {typeof(T)} from file {fullName}");

            try
            {
                _config = await JsonSerializer.DeserializeAsync<T>(File.Open(fullName, FileMode.Open, FileAccess.Read));
                _logger.LogInformation($"Config from file {fullName} succefully readed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing json file!");
                throw;
            }

            return _config;
        }

        public async Task WriteAsync(T config, string configFile)
        {
            string fullName = Path.Combine(_configPath, configFile);
            string jsonConfig = JsonSerializer.Serialize(config, _jsonSerializerOptions);
            await File.WriteAllTextAsync(fullName, jsonConfig);
        }
    }
}
