using System;
using System.Collections.Generic;
using System.Text;

namespace Config
{
    public class UpdaterConfigFactory
    {
        private readonly Configurator configurator;

        private UpdaterConfig? updaterConfig;

        public UpdaterConfigFactory(Configurator configurator)
        {
            this.configurator = configurator;
            this.configurator.ConfigurationUpdated += OnConfigurationUpdated;
        }

        private void OnConfigurationUpdated(object sender, UpdaterConfig e)
        {
            updaterConfig = e;
        }

        public UpdaterConfig Create()
        {
            if (updaterConfig != null)
            {
                return updaterConfig;
            }

            configurator.ReadAsync().Wait();

            return updaterConfig ?? new UpdaterConfig();
        }
    }
}
