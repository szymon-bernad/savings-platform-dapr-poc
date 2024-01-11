using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Nodes;

namespace SavingsPlatform.Common.Helpers
{
    public static class JsonDeserializeHelper
    {
        public static object? Deserialize(JsonObject json, Type resultType)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            var jsonProp = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            jsonProp.Converters.Add(new JsonStringEnumConverter());

            return JsonSerializer.Deserialize(
                json,
                resultType,
                jsonProp);
        }
    }
}
