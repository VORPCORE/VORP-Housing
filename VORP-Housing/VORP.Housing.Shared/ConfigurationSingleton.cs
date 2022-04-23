using Newtonsoft.Json;
using System;
#if SERVER
using System.IO;
using System.Text;
#endif
using VORP.Housing.Shared.Models.Json;

namespace VORP.Housing.Shared
{
    // Using "Singleton" design pattern
    public sealed class ConfigurationSingleton
    {
        private const string CONFIG_NAME = "config.json";

        private static ConfigurationSingleton _instance = null;
        private static readonly object _padlock = new object();

        public LangJson Language { get; private set; }
        public ConfigJson Config { get; private set; }

        #region Singleton Setup
        ConfigurationSingleton() { }

        public static ConfigurationSingleton Instance
        {
            get
            {
                // first check
                if (_instance == null)
                {
                    lock (_padlock)
                    {
                        // second check
                        if (_instance == null)
                        {
                            _instance = new ConfigurationSingleton();
                        }
                    }
                }

                return _instance;
            }
        }
        #endregion

        #region Public Method
        /// <summary>
        /// Import the housing settings from the "config.json"
        /// </summary>
        public void LoadConfig()
        {
            try
            {
                if (Config != null)
                {
                    return;
                }
#if SERVER
                string resourcePath = $"{API.GetResourcePath(API.GetCurrentResourceName())}";

                if (string.IsNullOrEmpty(resourcePath))
                {
                    Logger.CriticalError($"{CONFIG_NAME} was not found.");
                    return;
                }
                
                string configString = File.ReadAllText($"{resourcePath}/{CONFIG_NAME}", Encoding.UTF8);
                Config = JsonConvert.DeserializeObject<ConfigJson>(configString);
#endif

#if CLIENT
                string fileContents = LoadResourceFile(GetCurrentResourceName(), $"/{CONFIG_NAME}");
                Logger.Trace($"Current resource name: {GetCurrentResourceName()}");
                Logger.Trace($"{CONFIG_NAME} contents");
                Logger.Trace(fileContents);

                if (string.IsNullOrEmpty(fileContents))
                {
                    Logger.CriticalError($"{CONFIG_NAME} was not found.");
                    return;
                }

                Config = JsonConvert.DeserializeObject<ConfigJson>(fileContents);
#endif
                LoadLanguage();
            }
            catch (Exception ex)
            {
                Logger.CriticalError(ex, $"Shared.ConfigJson.LoadConfig()");
            }
        }
        #endregion

        #region Private Method
        /// <summary>
        /// Import the langauge-specific prompts from the "*.json" or "En.json" by default
        /// </summary>
        private void LoadLanguage()
        {
            try
            {
                const string DEFAULT_LANG = "En";
                string languageFileContents;

                string language = Config.DefaultLang;
                if (string.IsNullOrEmpty(language))
                {
                    Logger.Warn($"{CONFIG_NAME} \"defaultlang: '{language}'\" not found. Defaulting to {DEFAULT_LANG}.json...");
                    language = DEFAULT_LANG;
                }

#if SERVER
                string resourcePath = $"{API.GetResourcePath(API.GetCurrentResourceName())}";

                if (string.IsNullOrEmpty(resourcePath))
                {
                    Logger.Error($"{language}.json was not found.");
                    return;
                }

                languageFileContents = File.ReadAllText($"{resourcePath}/{language}.json", Encoding.UTF8);
#endif

#if CLIENT
                languageFileContents = LoadResourceFile(GetCurrentResourceName(), $"/{language}.json");

                if (string.IsNullOrEmpty(languageFileContents))
                {
                    Logger.Error($"{language}.json was not found.");
                    return;
                }
#endif
                Language = JsonConvert.DeserializeObject<LangJson>(languageFileContents);
                Logger.Trace($"Language Loaded: {language}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Shared.ConfigJson.LoadLanguage()");
            }
        }
        #endregion
    }
}
