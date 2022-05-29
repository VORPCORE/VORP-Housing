global using CitizenFX.Core;
global using CitizenFX.Core.Native;
global using VORP.Housing.Shared.Diagnostics;
global using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VORP.Housing.Shared;

namespace VORP.Housing.Server
{
    public class PluginManager : BasePluginManager
    {
        private readonly ConfigurationSingleton _configurationInstance = ConfigurationSingleton.Instance;
        private readonly string _GHMattiMySqlResourceState = GetResourceState("ghmattimysql");

        public static PluginManager Instance { get; private set; }
        public PlayerList PlayerList => Players;
        public ExportDictionary ExportRegistry => Exports;

        // private scripts
        public static GeneralApi GeneralApi = new();
        public static HouseInventoryApi HouseInventoryApi = new();

        public static Dictionary<string, int> ActiveCharacters = new();

        public PluginManager()
        {
            Logger.Info($"Init VORP Housing server");

            Instance = this;

            Setup();

            Logger.Info($"VORP Housing server loaded");
        }

        // This needs to become an Export on Core, as an EVENT its just adding more onto the event queue.
        public async static Task<dynamic> GetVorpCoreAsync()
        {
            dynamic core = null;

            TriggerEvent("getCore", new Action<dynamic>((getCoreResult) =>
            {
                core = getCoreResult;
            }));

            while (core == null)
            {
                await Delay(100);
            }

            return core;
        }

        async Task VendorReady()
        {
            string dbResource = _GHMattiMySqlResourceState;
            if (dbResource == "missing")
            {
                while (true)
                {
                    Logger.Error($"ghmattimysql resource not found! Please make sure you have the resource!");
                    await Delay(1000);
                }
            }

            while (!(dbResource == "started"))
            {
                await Delay(500);
                dbResource = _GHMattiMySqlResourceState;
            }
        }

        async void Setup()
        {
            try
            {
                await VendorReady(); // wait till ghmattimysql resource has started

                _configurationInstance.LoadConfig();

                // control the start up order of each script
                GeneralApi.Initialize();
                GeneralApi.LoadAll();
                HouseInventoryApi.Initialize();

                AddEvents();
            }
            catch (Exception ex)
            {
                Logger.CriticalError(ex, $"Server.PluginManager.Setup()");
            }
        }

        void AddEvents()
        {
            Hook("playerJoined", new Action<Player>(([FromSource] player) =>
            {
                if (!ActiveCharacters.ContainsKey(player.Handle))
                    ActiveCharacters.Add(player.Handle, -1);
            }));

            Hook("playerDropped", new Action<Player, string>(([FromSource] player, reason) =>
            {
                try
                {
                    string steamIdent = $"steam:{player.Identifiers["steam"]}";

                    if (ActiveCharacters.ContainsKey(player.Handle))
                        ActiveCharacters.Remove(player.Handle);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"playerDropped: So, they don't exist?!");
                }
            }));

            Hook("onResourceStart", new Action<string>(resourceName =>
            {
                if (resourceName != GetCurrentResourceName()) 
                    return;

                Logger.Info($"VORP Housing resource started");
            }));

            Hook("onResourceStop", new Action<string>(resourceName =>
            {
                if (resourceName != GetCurrentResourceName())
                    return;

                Logger.Info($"Stopping VORP Housing resource");
            }));
        }
    }
}
