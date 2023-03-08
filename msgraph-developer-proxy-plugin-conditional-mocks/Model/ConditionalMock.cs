using System.Text.Json.Serialization;

namespace Microsoft.Graph.DeveloperProxy.Plugins.Conditional.Mocks.Model
{
    public class ConditionalMock
    {
        [JsonPropertyName("request")]
        public ConditionalMockRequest Request { get; set; } = new ConditionalMockRequest();

        [JsonPropertyName("response")]
        public ConditionalMockResponse Response { get; set; } = new ConditionalMockResponse();
    }
}
