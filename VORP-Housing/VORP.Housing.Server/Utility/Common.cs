using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace VORP.Housing.Server.Utility
{
    static class Common
    {
        public async static Task<dynamic> GetCoreUserAsync(int playerHandle)
        {
            dynamic core = await PluginManager.GetVorpCoreAsync();
            return core.getUser(playerHandle);
        }

        public static bool HasProperty(ExpandoObject obj, string propertyName)
        {
            return obj != null && ((IDictionary<string, object>)obj).ContainsKey(propertyName);
        }
    }
}
