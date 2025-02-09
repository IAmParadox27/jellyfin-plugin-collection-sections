using System.Net.Http.Headers;
using System.Reflection;
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

        internal void OnConfigurationChanged(object? sender, BasePluginConfiguration e)
        {
            if (e is PluginConfiguration pluginConfiguration)
            {
                string? publishedServerUrl = m_serverApplicationHost.GetType()
                    .GetProperty("PublishedServerUrl", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(m_serverApplicationHost) as string;

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(publishedServerUrl ?? $"http://localhost:{m_serverApplicationHost.HttpPort}");

                foreach (SectionsConfig section in pluginConfiguration.Sections)
                {
                    JObject jsonPayload = new JObject();
                    jsonPayload.Add("id", section.UniqueId);
                    jsonPayload.Add("displayText", section.DisplayText);
                    jsonPayload.Add("limit", 1);
                    jsonPayload.Add("additionalData", section.CollectionName);

                    if (section.SectionType == SectionType.Collection)
                    {
                        jsonPayload.Add("resultsEndpoint", "/CollectionSections/Collection");
                    }
                    else if (section.SectionType == SectionType.Playlist)
                    {
                        jsonPayload.Add("resultsEndpoint", "/CollectionSections/Playlist");
                    }

                    try
                    {
                        client.PostAsync("/HomeScreen/RegisterSection",
                            new StringContent(jsonPayload.ToString(Formatting.None),
                                MediaTypeHeaderValue.Parse("application/json"))).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogError(ex, $"Caught exception when attempting to register section with HomeScreenSections plugin. Ensure you have `Home Screen Sections` installed on your server.");
                        return;
                    }
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