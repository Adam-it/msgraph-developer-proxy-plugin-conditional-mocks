using System.Text.Json.Serialization;

namespace Microsoft.Graph.DeveloperProxy.Plugins.Conditional.Mocks.Model
{
    public class ConditionalMockResponse
    {
        [JsonPropertyName("body")]
        public IDictionary<string, string>? ResponseBody { get; set; }

        [JsonPropertyName("code")]
        public int? ResponseCode { get; set; }

        [JsonPropertyName("responseHeaders")]
        public IDictionary<string, string>? ResponseHeaders { get; set; }
    }
}
