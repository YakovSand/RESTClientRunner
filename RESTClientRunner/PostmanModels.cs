using System.Text.Json;
using System.Text.Json.Serialization;

namespace RESTClientRunner
{
    public class PostmanCollection
    {
        [JsonPropertyName("info")]
        public PostmanInfo Info { get; set; } = new();

        [JsonPropertyName("item")]
        public List<PostmanItem> Item { get; set; } = new();
    }

    public class PostmanInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [JsonPropertyName("schema")]
        public string Schema { get; set; } = "";
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
    }

    public class PostmanItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("request")]
        public PostmanRequest Request { get; set; } = new();

        [JsonPropertyName("event")]
        public List<PostmanEvent> Event { get; set; } = new();
    }

    public class PostmanEvent
    {
        [JsonPropertyName("listen")]
        public string Listen { get; set; } = "";

        [JsonPropertyName("script")]
        public PostmanScript Script { get; set; } = new();
    }

    public class PostmanRequest
    {
        [JsonPropertyName("method")]
        public string Method { get; set; } = "";

        // Support both string and object URLs
        [JsonPropertyName("url")]
        [JsonConverter(typeof(PostmanUrlConverter))]
        public PostmanUrl Url { get; set; } = new();

        [JsonPropertyName("header")]
        public List<PostmanHeader> Header { get; set; } = new();

        [JsonPropertyName("body")]
        public PostmanBody Body { get; set; } = new();
    }

    public class PostmanUrl
    {
        public string Raw { get; set; } = "";
    }

    public class PostmanHeader
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = "";

        [JsonPropertyName("value")]
        public string Value { get; set; } = "";
    }

    public class PostmanBody
    {
        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "";

        [JsonPropertyName("raw")]
        public string Raw { get; set; } = "";
    }
  
    public class PostmanScript
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("exec")]
        public List<string> Exec { get; set; } = new();
    }

    // Custom converter to handle both string and object URL formats in PostmanRequest model
    public class PostmanUrlConverter : JsonConverter<PostmanUrl>
    {
        public override PostmanUrl Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Check the token type to determine if it's a string or an object
            if (reader.TokenType == JsonTokenType.String)
            {
                return new PostmanUrl { Raw = reader.GetString() ?? "" };
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                // Deserialize the object into PostmanUrl
                var obj = JsonSerializer.Deserialize<PostmanUrl>(ref reader, options);
                return obj ?? new PostmanUrl();
            }

            throw new JsonException("Invalid PostmanUrl!");
        }

        public override void Write(Utf8JsonWriter writer, PostmanUrl value, JsonSerializerOptions options)
        {
            // Always serialize as string using Raw property for simplicity
            writer.WriteStringValue(value.Raw);
        }
    }
}
