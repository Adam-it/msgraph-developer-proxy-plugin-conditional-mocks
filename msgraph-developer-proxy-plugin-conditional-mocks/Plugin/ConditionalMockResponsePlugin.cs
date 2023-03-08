using Dynamitey;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph.DeveloperProxy.Abstractions;
using Microsoft.Graph.DeveloperProxy.Plugins.Conditional.Mocks.Model;
using Newtonsoft.Json;
using System.CommandLine;
using System.Net;
using System.Text.RegularExpressions;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;

namespace Microsoft.Graph.DeveloperProxy.Plugins.Conditional.Mocks.Plugin
{
    public class ConditionalMockResponsePlugin : BaseProxyPlugin
    {
        private ConditionalMockResponseConfiguration _configuration = new();
        private ConditionalMockResponseLoader? _loader = null;
        private readonly Option<bool?> _noMocks;
        private readonly Option<string?> _mocksFile;
        public override string Name => nameof(ConditionalMockResponsePlugin);

        public ConditionalMockResponsePlugin()
        {
            _noMocks = new Option<bool?>("--no-mocks", "Disable loading mock requests");
            _noMocks.AddAlias("-n");
            _noMocks.ArgumentHelpName = "no mocks";
            _noMocks.SetDefaultValue(null);

            _mocksFile = new Option<string?>("--mocks-file", "Provide a file populated with mock responses");
            _mocksFile.ArgumentHelpName = "mocks file";
            _mocksFile.SetDefaultValue(null);
        }

        public override void Register(IPluginEvents pluginEvents, IProxyContext context, ISet<Regex> urlsToWatch, IConfigurationSection? configSection = null)
        {
            base.Register(pluginEvents, context, urlsToWatch, configSection);

            configSection?.Bind(_configuration);
            _loader = new ConditionalMockResponseLoader(_logger!, _configuration);

            pluginEvents.Init += OnInit;
            pluginEvents.OptionsLoaded += OnOptionsLoaded;
            pluginEvents.BeforeRequest += OnRequest;
        }

        private void OnInit(object? sender, InitArgs e)
        {
            e.RootCommand.AddOption(_noMocks);
            e.RootCommand.AddOption(_mocksFile);
        }

        private void OnOptionsLoaded(object? sender, OptionsLoadedArgs e)
        {
            var context = e.Context;

            // allow disabling of mocks as a command line option
            var noMocks = context.ParseResult.GetValueForOption(_noMocks);
            if (noMocks.HasValue)
            {
                _configuration.NoMocks = noMocks.Value;
            }
            if (_configuration.NoMocks)
            {
                // mocks have been disabled. No need to continue
                return;
            }

            // update the name of the mocks file to load from if supplied
            string? mocksFile = context.ParseResult.GetValueForOption(_mocksFile);
            if (mocksFile is not null)
            {
                _configuration.MocksFile = mocksFile;
            }
            // load the responses from the configured mocks file
            _loader?.InitResponsesWatcher();
        }

        private void OnRequest(object? sender, ProxyRequestArgs e)
        {
            var request = e.Session.HttpClient.Request;
            var requestBody = e.Session.GetRequestBodyAsString();
            var state = e.ResponseState;
            if (!_configuration.NoMocks && _urlsToWatch is not null && e.ShouldExecute(_urlsToWatch))
            {
                var matchingResponse = GetMatchingMockResponse(request, requestBody.Result);
                if (matchingResponse is not null)
                {
                    ProcessMockResponse(e.Session, matchingResponse);
                    state.HasBeenSet = true;
                }
            }
        }

        private ConditionalMock? GetMatchingMockResponse(Request request, string body)
        {
            if (_configuration.NoMocks ||
                _configuration.Responses is null ||
                !_configuration.Responses.Any())
            {
                return null;
            }

            var mockResponse = _configuration.Responses.FirstOrDefault(mockResponse =>
            {
                if (mockResponse.Request.Method == request.Method &&
                mockResponse.Request.Url.ToLower() == request.Url.ToLower())
                {
                    if (request.Method.ToLower() == "get" || mockResponse.Request.Body is null)
                        return true;

                    // match request with response by condition in body
                    var condition = mockResponse.Request.Body.First();
                    var requestBody = JsonConvert.DeserializeObject<dynamic>(body);
                    IEnumerable<string> requestBodyParams = Dynamic.GetMemberNames(requestBody);

                    var matchingRequestBodyParam = requestBodyParams.FirstOrDefault(param => param == condition.Key);
                    if (matchingRequestBodyParam is not null)
                    {
                        var matchingRequestBodyParamValue = Dynamic.InvokeGet(requestBody, matchingRequestBodyParam);
                        if (matchingRequestBodyParamValue?.ToString().ToLower() == condition.Value.ToLower())
                        {
                            return true;
                        }
                    }
                }

                return false;
            });
            return mockResponse;
        }

        private void ProcessMockResponse(SessionEventArgs e, ConditionalMock matchingResponse)
        {
            string? body = null;
            string requestId = Guid.NewGuid().ToString();
            string requestDate = DateTime.Now.ToString();
            var headers = ProxyUtils.BuildGraphResponseHeaders(e.HttpClient.Request, requestId, requestDate);
            HttpStatusCode statusCode = HttpStatusCode.OK;
            if (matchingResponse.Response.ResponseCode is not null)
            {
                statusCode = (HttpStatusCode)matchingResponse.Response.ResponseCode;
            }

            if (matchingResponse.Response.ResponseHeaders is not null)
            {
                foreach (var key in matchingResponse.Response.ResponseHeaders.Keys)
                {
                    headers.Add(new HttpHeader(key, matchingResponse.Response.ResponseHeaders[key]));
                }
            }

            // default the content type to application/json unlesss set in the mock response
            if (!headers.Any(h => h.Name.Equals("content-type", StringComparison.OrdinalIgnoreCase)))
            {
                headers.Add(new HttpHeader("content-type", "application/json"));
            }

            if (matchingResponse.Response.ResponseBody is not null)
            {
                var bodyString = System.Text.Json.JsonSerializer.Serialize(matchingResponse.Response.ResponseBody) as string;
                // we get a JSON string so need to start with the opening quote
                if (bodyString?.StartsWith("\"@") ?? false)
                {
                    // we've got a mock body starting with @-token which means we're sending
                    // a response from a file on disk
                    // if we can read the file, we can immediately send the response and
                    // skip the rest of the logic in this method
                    // remove the surrounding quotes and the @-token
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), bodyString.Trim('"').Substring(1));
                    if (!File.Exists(filePath))
                    {
                        _logger?.LogError($"File {filePath} not found. Serving file path in the mock response");
                        body = bodyString;
                    }
                    else
                    {
                        var bodyBytes = File.ReadAllBytes(filePath);
                        e.GenericResponse(bodyBytes, statusCode, headers);
                    }
                }
                else
                {
                    body = bodyString;
                }
            }
            e.GenericResponse(body ?? string.Empty, statusCode, headers);

            _logger?.LogRequest(new[] { $"{matchingResponse.Response.ResponseCode ?? 200} {matchingResponse.Request.Url}" }, MessageType.Mocked, new LoggingContext(e));
        }
    }
}
