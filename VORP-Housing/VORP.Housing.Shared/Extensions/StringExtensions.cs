using Newtonsoft.Json;
using System;

namespace VORP.Housing.Shared.Extensions
{
    public static class StringExtensions
    {
        public static T FromJson<T>(this string str)
        {
            return JsonConvert.DeserializeObject<T>(str);
        }

        public static TEnum ToEnum<TEnum>(this string strEnumValue, TEnum defaultValue)
        {
            if (!Enum.IsDefined(typeof(TEnum), strEnumValue))
                return defaultValue;

            return (TEnum)Enum.Parse(typeof(TEnum), strEnumValue);
        }
    }
}
