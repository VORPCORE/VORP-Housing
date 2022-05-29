using Newtonsoft.Json;
using System;
using VORP.Housing.Shared.Models.Json;

namespace VORP.Housing.Shared
{
    // Using "Singleton" design pattern
    public sealed class ConfigurationSingleton
    {
        private const string CONFIG_NAME = "config.json";

        public LangJson Language { get; private set; }
        public ConfigJson Config { get; private set; }

        #region Singleton Setup
        private static ConfigurationSingleton _instance = null;
        private static readonly object _padlock = new object();

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

                string fileContents = LoadResourceFile(GetCurrentResourceName(), $"/{CONFIG_NAME}");

                if (string.IsNullOrEmpty(fileContents))
                {
                    Logger.CriticalError($"{CONFIG_NAME} was not found.");
                    return;
                }

                // Workaround until a solution is found for mysterious parsing issue
                int startingParseIndex = fileContents.IndexOf('{');
                Config = JsonConvert.DeserializeObject<ConfigJson>(fileContents.Substring(startingParseIndex));

                Logger.Trace($"{CONFIG_NAME} loaded");

                LoadLanguage();
            }
            catch (Exception ex)
            {
                Logger.CriticalError(ex, $"Shared.ConfigurationSingleton.LoadConfig()");
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

                languageFileContents = LoadResourceFile(GetCurrentResourceName(), $"/languages/{language}.json");

                if (string.IsNullOrEmpty(languageFileContents))
                {
                    Logger.Error($"{language}.json was not found.");
                    return;
                }

                // Workaround until a solution is found for mysterious parsing issue
                int startingParseIndex = languageFileContents.IndexOf('{');
                Language = JsonConvert.DeserializeObject<LangJson>(languageFileContents.Substring(startingParseIndex));

                Logger.Trace($"Language Loaded: {language}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Shared.ConfigurationSingleton.LoadLanguage()");
            }
        }
        #endregion
    }
}
