using Microsoft.Graph.DeveloperProxy.Abstractions;
using Microsoft.Graph.DeveloperProxy.Plugins.Conditional.Mocks.Model;
using System.Text.Json;

namespace Microsoft.Graph.DeveloperProxy.Plugins.Conditional.Mocks.Plugin
{
    public class ConditionalMockResponseLoader : IDisposable
    {
        private readonly ILogger _logger;
        private readonly ConditionalMockResponseConfiguration _configuration;

        public ConditionalMockResponseLoader(ILogger logger, ConditionalMockResponseConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        private string _responsesFilePath => Path.Combine(Directory.GetCurrentDirectory(), _configuration.MocksFile);
        private FileSystemWatcher? _watcher;

        public void LoadResponses()
        {
            if (!File.Exists(_responsesFilePath))
            {
                _logger.LogWarn($"File {_configuration.MocksFile} not found in the current directory. No mocks will be provided");
                _configuration.Responses = Array.Empty<ConditionalMock>();
                return;
            }

            try
            {
                var responsesString = File.ReadAllText(_responsesFilePath);
                var responsesConfig = JsonSerializer.Deserialize<ConditionalMockResponseConfiguration>(responsesString);
                IEnumerable<ConditionalMock>? configResponses = responsesConfig?.Responses;
                if (configResponses is not null)
                {
                    _configuration.Responses = configResponses;
                    _logger.LogInfo($"Mock responses for {configResponses.Count()} url patterns loaded from {_configuration.MocksFile}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error has occurred while reading {_configuration.MocksFile}:");
                _logger.LogError(ex.Message);
            }
        }

        public void InitResponsesWatcher()
        {
            if (_watcher is not null)
            {
                return;
            }

            string path = Path.GetDirectoryName(_responsesFilePath) ?? throw new InvalidOperationException($"{_responsesFilePath} is an invalid path");
            _watcher = new FileSystemWatcher(path);
            _watcher.NotifyFilter = NotifyFilters.CreationTime
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Size;
            _watcher.Filter = Path.GetFileName(_responsesFilePath);
            _watcher.Changed += ResponsesFile_Changed;
            _watcher.Created += ResponsesFile_Changed;
            _watcher.Deleted += ResponsesFile_Changed;
            _watcher.Renamed += ResponsesFile_Changed;
            _watcher.EnableRaisingEvents = true;

            LoadResponses();
        }

        private void ResponsesFile_Changed(object sender, FileSystemEventArgs e)
        {
            LoadResponses();
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }
    }

}
