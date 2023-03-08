# Microsoft Graph Developer Proxy Plugin - Conditional mocks

### 📝 Description

This repo contains additional plugin that might be used in [Microsoft Graph Developer Proxy Plugin](https://github.com/microsoftgraph/msgraph-developer-proxy) instead of the default mocks plugin.
The conditional mock plugin allows to define multiple responses for the same request. The response which condition is met is returned.

### 🚀 How to install

> The current installtion process is still to be clarified. Especially the part how to use the msgraph-developer-proxy-abstractions in a custom proxy plugin

1. pull the [Microsoft Graph Developer Proxy Plugin](https://github.com/microsoftgraph/msgraph-developer-proxy) repo and build it
2. pull this repo locally and build the project (in order to build you will need to restore NuGets and correct the reference to msgraph-developer-proxy-abstractions.dll file)
3. copy the full output of the build to the location to GraphProxyPlugins catalog
4. modify the `appsettings.json` file replacing the default `MockResponsePlugin` plugin with the following:

```json
    {
      "name": "ConditionalMockResponsePlugin",
      "disabled": false,
      "pluginPath": "GraphProxyPlugins\\msgraph-developer-proxy-plugin-conditional-mocks.dll",
      "configSection": "mocksPlugin"
    }
```

5. start the Microsoft Graph Developer Proxy with `--mocks-file` option passing the path to mock.json file with conditional responses. Please check the sample [response.sample.json](/responses.sample.json) file. 


### 🛣️ Roadmap

The current solution is still in preview/prototype stage. Before the release the following milestones must be fulfilled:

- [ ] Add possibility to define condition based on request headers
- [ ] Clarify the plugin install process
- [ ] Wait for the Microsoft Graph Developer Proxy release (align with it if needed)

### ⚠ Disclaimer

This code is provided as is without warranty of any kind, either express or implied, including any implied warranties of fitness for a particular purpose, merchantability, or non-infringement.