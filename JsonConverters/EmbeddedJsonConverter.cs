using System;
using System.IO;

using Newtonsoft.Json;

namespace AwsSecretsManagerService.JsonConverters
{
    public class EmbeddedJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            using (var stringReader = new StringReader((string)reader.Value))
            {
                return serializer.Deserialize(stringReader, objectType);
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}
