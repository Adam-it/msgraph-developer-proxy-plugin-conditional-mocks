using System.Text.Json.Serialization;

namespace Microsoft.Graph.DeveloperProxy.Plugins.Conditional.Mocks.Model
{
    public class ConditionalMockResponseConfiguration
    {
        public bool NoMocks { get; set; } = false;
        public string MocksFile { get; set; } = "responses.json";

        [JsonPropertyName("responses")]
        public IEnumerable<ConditionalMock> Responses { get; set; } = Array.Empty<ConditionalMock>();
    }
}
