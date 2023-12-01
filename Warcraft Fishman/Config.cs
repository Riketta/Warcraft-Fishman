using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;

namespace Fishman
{
    internal class Config : IConfig
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        public static readonly string DefaultConfigPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Config.json");

        /// <summary>
        /// Path to the saved version of the current config instance.
        /// </summary>
        [JsonIgnore]
        public string PathToConfig { get; private set; }

        /// <summary>
        /// Used to enable debugging mode.
        /// </summary>
        public bool Debug { get; set; } = false;

        public bool DebugOpenCV { get; set; } = false;

        public RetailBotOptions RetailBotOptions { get; set; } = new RetailBotOptions();
        public ClassicBotOptions ClassicBotOptions { get; set; } = new ClassicBotOptions();

        [JsonConstructor]
        public Config()
        {
            PathToConfig = DefaultConfigPath;
        }

        private Config(string pathToConfig)
        {
            if (string.IsNullOrEmpty(pathToConfig))
                throw new ArgumentNullException(nameof(pathToConfig));

            PathToConfig = pathToConfig;
        }

        ~Config()
        {
        }

        public static Config Load(string pathToConfig)
        {
            if (string.IsNullOrEmpty(pathToConfig))
                throw new ArgumentNullException(nameof(pathToConfig));

            if (!File.Exists(pathToConfig) && pathToConfig == DefaultConfigPath)
            {
                _logger?.Warn("No config file found!");
                return SaveDefault();
            }

            string json = File.ReadAllText(pathToConfig);

            Config config = JsonConvert.DeserializeObject<Config>(json) ?? throw new InvalidOperationException();
            config.PathToConfig = pathToConfig;

            config.Save(); // re-save to add new or missing config fields

            return config;
        }

        public static Config SaveDefault()
        {
            Config config = new Config(DefaultConfigPath);
            config.Save();
            return config;
        }

        public Config Save()
        {
            string json = JsonConvert.SerializeObject(this, new JsonSerializerSettings() { Formatting = Formatting.Indented, });
            File.WriteAllText(PathToConfig, json);

            return this;
        }

        IConfig IConfig.Save()
        {
            return Save();
        }
    }
}
