using Newtonsoft.Json;

namespace VORP.Housing.Shared.Extensions
{
    public static class ObjectExtensions
    {
        public static string ToJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}
