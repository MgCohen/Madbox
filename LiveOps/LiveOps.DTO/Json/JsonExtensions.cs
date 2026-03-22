using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Madbox.LiveOps.DTO.Json
{
    /// <summary>
    /// JSON helpers for Cloud Save and module payloads.
    /// </summary>
    public static class JsonExtensions
    {
        private static readonly ISerializationBinder Binder = new CrossPlatformTypeBinder();

        public static T FromJson<T>(this string json)
        {
            if (string.IsNullOrWhiteSpace(json) || (!json.TrimStart().StartsWith("{") && !json.TrimStart().StartsWith("[")))
            {
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)json;
                }
            }

            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            var settings = new JsonSerializerSettings
            {
                SerializationBinder = Binder,
                TypeNameHandling = TypeNameHandling.All,
                MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
            };

            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        public static string ToJson(this object obj, Formatting formatting = Formatting.None)
        {
            if (obj == null)
            {
                return null;
            }

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                Formatting = formatting,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = ShouldSerializeContractResolver.Instance
            };

            return JsonConvert.SerializeObject(obj, settings);
        }
    }
}
