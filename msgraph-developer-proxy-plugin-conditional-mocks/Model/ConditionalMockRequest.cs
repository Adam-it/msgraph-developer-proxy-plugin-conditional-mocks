using System.Text.Json.Serialization;

namespace Microsoft.Graph.DeveloperProxy.Plugins.Conditional.Mocks.Model
{
    public class ConditionalMockRequest
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("method")]
        public string Method { get; set; } = "GET";

        [JsonPropertyName("body")]
        public IDictionary<string, string>? Body { get; set; }
    }
}
