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
        public ClientMain Main = new();

        public PluginManager()
        {
            try
            {
                Logger.Info("Init VORP Housing client");

                Instance = this;

                _configurationInstance.LoadConfig();

                // control the start up order of each script
                Main.Initialize();

                Logger.Info("VORP Housing client loaded");
            }
            catch (Exception ex)
            {
                Logger.CriticalError(ex, $"Client.PluginManager.PluginManager()");
            }
        }
    }
}
