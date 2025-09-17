using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Loader;
using Jellyfin.Plugin.CollectionSections.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.CollectionSections
{
    public class CollectionSectionPlugin : BasePlugin<PluginConfiguration>, IHasPluginConfiguration, IHasWebPages
    {
        public override Guid Id => Guid.Parse("043b2c48-b3e0-4610-b398-8217b146d1a4");

        public override string Name => "Collection Sections";

        private readonly IServerApplicationHost m_serverApplicationHost;
        private readonly ILogger<CollectionSectionPlugin> m_logger;

        public static CollectionSectionPlugin Instance { get; set; } = null!;
    
        public CollectionSectionPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IServerApplicationHost serverApplicationHost,
            ILogger<CollectionSectionPlugin> logger) : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        
            m_serverApplicationHost = serverApplicationHost;
            m_logger = logger;
        
            ConfigurationChanged += OnConfigurationChanged;
        }

        /// <summary>
        /// Waits for Home Screen plugin readiness with exponential backoff.
        /// </summary>
        private async Task<(bool success, int elapsedSeconds)> WaitForHomeScreenReady(HttpClient client, int maxAttempts = 60, int intervalSeconds = 5)
        {
            int totalDelaySeconds = 0;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var response = await client.GetAsync("/HomeScreen/Ready");
                    if (response.IsSuccessStatusCode)
                    {
                        m_logger.LogInformation($"Home Screen plugin ready (attempt {attempt})");
                        return (true, totalDelaySeconds);
                    }

                    m_logger.LogDebug($"Home Screen not ready (attempt {attempt}/{maxAttempts}): {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    m_logger.LogDebug($"Readiness check failed (attempt {attempt}/{maxAttempts}): {ex.Message}");
                }

                totalDelaySeconds += intervalSeconds;
                await Task.Delay(intervalSeconds * 1000);
            }

            return (false, totalDelaySeconds);
        }

        internal async void OnConfigurationChanged(object? sender, BasePluginConfiguration e)
        {
            if (e is PluginConfiguration pluginConfiguration)
            {
                List<JObject> payloads = new List<JObject>();
                foreach (SectionsConfig section in pluginConfiguration.Sections)
                {
                    JObject jsonPayload = new JObject();
                    jsonPayload.Add("id", section.UniqueId);
                    jsonPayload.Add("displayText", section.DisplayText);
                    jsonPayload.Add("limit", 1);
                    jsonPayload.Add("additionalData", section.CollectionName);

                    jsonPayload.Add("resultsAssembly", GetType().Assembly.FullName);
                    jsonPayload.Add("resultsClass", typeof(ResultsHandler).FullName);
                    if (section.SectionType == SectionType.Collection)
                    {
                        jsonPayload.Add("resultsMethod", nameof(ResultsHandler.GetCollectionResults));
                    }
                    else if (section.SectionType == SectionType.Playlist)
                    {
                        jsonPayload.Add("resultsMethod", nameof(ResultsHandler.GetPlaylistResults));
                    }
                    
                    payloads.Add(jsonPayload);
                    continue;
                    
                    try
                    {
                        string? publishedServerUrl = m_serverApplicationHost.GetType()
                            .GetProperty("PublishedServerUrl", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(m_serverApplicationHost) as string;

                        HttpClient client = new HttpClient();
                        client.BaseAddress = new Uri(publishedServerUrl ?? $"http://localhost:{m_serverApplicationHost.HttpPort}");

                        var (success, elapsedSeconds) = await WaitForHomeScreenReady(client);
                        if (!success)
                        {
                            m_logger.LogError($"Home Screen plugin not ready after {elapsedSeconds}s. Cannot register sections.");
                            return;
                        }

                        var response = await client.PostAsync("/HomeScreen/RegisterSection",
                            new StringContent(jsonPayload.ToString(Formatting.None),
                                MediaTypeHeaderValue.Parse("application/json")));

                        if (response.IsSuccessStatusCode)
                        {
                            m_logger.LogInformation($"Registered section '{section.DisplayText}'");
                        }
                        else
                        {
                            m_logger.LogWarning($"Failed to register section '{section.DisplayText}': {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
                
                
                Assembly? homeScreenSectionsAssembly =
                    AssemblyLoadContext.All.SelectMany(x => x.Assemblies).FirstOrDefault(x =>
                        x.FullName?.Contains(".HomeScreenSections") ?? false);

                if (homeScreenSectionsAssembly == null)
                {
                    m_logger.LogError($"Couldn't find Home Screen Sections assembly when attempting to register section. Ensure you have `Home Screen Sections` installed on your server.");
                    return;
                }

                Type? pluginInterfaceType = homeScreenSectionsAssembly.GetType("Jellyfin.Plugin.HomeScreenSections.PluginInterface");

                if (pluginInterfaceType == null)
                {
                    m_logger.LogError($"Couldn't find PluginInterface type in Home Screen Sections plugin when attempting to register section. Ensure you have the latest version of `Home Screen Sections` installed on your server.");
                    return;
                }

                foreach (JObject payload in payloads)
                {
                    m_logger.LogInformation($"Registering section '{payload["displayText"]}'");
                    pluginInterfaceType.GetMethod("RegisterSection")?.Invoke(null, new object?[] { payload });
                }
            }
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            string? prefix = GetType().Namespace;

            yield return new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = $"{prefix}.Configuration.config.html"
            };
        }
    }
}