global using CitizenFX.Core;
global using CitizenFX.Core.Native;
global using VORP.Housing.Shared.Diagnostics;
global using static CitizenFX.Core.Native.API;
using System;
using VORP.Housing.Shared;

namespace VORP.Housing.Client
{
    public class PluginManager : BasePluginManager
    {
        private readonly ConfigurationSingleton _configurationInstance = ConfigurationSingleton.Instance;

        public static PluginManager Instance { get; private set; }
        public Init Init = new();

        public PluginManager()
        {
            try
            {
                Logger.Info("VORP Housing INIT");

                Instance = this;

                _configurationInstance.LoadConfig();

                // control the start up order of each script
                Init.Initialize();

                Logger.Info("VORP Housing Loaded");
            }
            catch (Exception ex)
            {
                Logger.CriticalError(ex, $"Shared.PluginManager.PluginManager()");
            }
        }
    }
}
