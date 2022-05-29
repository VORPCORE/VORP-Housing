using System.Threading.Tasks;
using VORP.Housing.Server.Utility;

namespace VORP.Housing.Server.Extensions
{
    public static class PlayerExtensions
    {
        public async static Task<dynamic> GetCoreUserAsync(this Player player)
        {
            dynamic core = await PluginManager.GetVorpCoreAsync();
            return core.getUser(int.Parse(player.Handle));
        }

        public async static Task<dynamic> GetCoreUserCharacterAsync(this Player player)
        {
            dynamic coreUser = await player.GetCoreUserAsync();
            if (coreUser == null)
            {
                Logger.Warn($"Server.PlayerExtensions.GetCoreUserCharacterAsync(): " +
                    $"Player \"{player.Handle}\" does not exist.");

                return null;
            }

            return coreUser.getUsedCharacter;
        }

        public async static Task<int> GetCoreUserCharacterIdAsync(this Player player)
        {
            dynamic character = await player.GetCoreUserCharacterAsync();

            if (character == null)
            {
                if (!PluginManager.ActiveCharacters.ContainsKey(player.Handle))
                {
                    Logger.Warn("Server.PlayerExtensions.GetCoreUserCharacterIdAsync(): " +
                        $"The active player \"{player.Handle}\" was not found via \"GetCoreUserCharacterAsync()\"");

                    return -1;
                }

                return PluginManager.ActiveCharacters[player.Handle];
            }

            if (!Common.HasProperty(character, "charIdentifier"))
            {
                if (!PluginManager.ActiveCharacters.ContainsKey(player.Handle))
                {
                    Logger.Warn("Server.PlayerExtensions.GetCoreUserCharacterIdAsync(): " +
                        $"The active player \"{player.Handle}\" was not found via \"charIdentifier\" property");

                    return -1;
                }

                return PluginManager.ActiveCharacters[player.Handle];
            }

            return character?.charIdentifier;
        }
    }
}
