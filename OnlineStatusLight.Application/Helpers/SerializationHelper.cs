using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OnlineStatusLight.Application.Helpers
{
    public class SerializationHelper
    {
        public static JsonSerializerSettings Settings { get; private set; }

        static SerializationHelper()
        {
            Settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
            };

            Settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            Settings.ContractResolver = new CamelCasePropertyNamesContractResolver();

        }

        public static string ToJson(object o)
        {
            return JsonConvert.SerializeObject(o, Settings);
        }

        public static T FromJson<T>(string data)
        {
            return JsonConvert.DeserializeObject<T>(data, Settings);
        }

        public static object FromJson(string data, Type type)
        {
            return JsonConvert.DeserializeObject(data, type, Settings);
        }
    }
}
