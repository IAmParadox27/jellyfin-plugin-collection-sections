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

        /// <summary>
        /// Waits for Home Screen plugin readiness with exponential backoff.
        /// </summary>
        private async Task<bool> WaitForHomeScreenReady(HttpClient client, int maxAttempts = 12, int initialDelayMs = 250)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var response = await client.GetAsync("/HomeScreen/Ready");
                    if (response.IsSuccessStatusCode)
                    {
                        m_logger.LogInformation("Home Screen plugin ready (attempt {Attempt})", attempt);
                        return true;
                    }
                    
                    m_logger.LogDebug("Home Screen not ready (attempt {Attempt}/{Max}): {Status}", 
                        attempt, maxAttempts, response.StatusCode);
                }
                catch (Exception ex)
                {
                    m_logger.LogDebug("Readiness check failed (attempt {Attempt}/{Max}): {Error}", 
                        attempt, maxAttempts, ex.Message);
                }
                               
                // Exponential backoff: 250ms → 8s max
                int delayMs = Math.Min(initialDelayMs * (1 << (attempt - 1)), 8000);
                await Task.Delay(delayMs);
            }
            
            return false;
        }

        internal async void OnConfigurationChanged(object? sender, BasePluginConfiguration e)
        {
            if (e is PluginConfiguration pluginConfiguration)
            {
                string? publishedServerUrl = m_serverApplicationHost.GetType()
                    .GetProperty("PublishedServerUrl", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(m_serverApplicationHost) as string;

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(publishedServerUrl ?? $"http://localhost:{m_serverApplicationHost.HttpPort}");

                if (!await WaitForHomeScreenReady(client))
                {
                    m_logger.LogError("Home Screen plugin not ready after {MaxWait}s. Cannot register sections.", 96);
                    return;
                }

                foreach (SectionsConfig section in pluginConfiguration.Sections)
                {
                    JObject jsonPayload = new JObject();
                    jsonPayload.Add("id", section.UniqueId);
                    jsonPayload.Add("displayText", section.DisplayText);
                    jsonPayload.Add("limit", 1);
                    jsonPayload.Add("additionalData", section.CollectionName);

                    JObject info = new JObject
                    {
                        ["description"] = string.IsNullOrEmpty(section.Description) 
                            ? $"Display items from the {section.CollectionName} {section.SectionType.ToString().ToLower()}"
                            : section.Description,
                        ["adminNotes"] = "This section is managed by the Collection Sections plugin. Configure the source collection/playlist and display options in the Collection Sections plugin settings.",
                        ["versionControl"] = new JObject
                        {
                            ["platform"] = "GitHub",
                            ["username"] = "IAmParadox27",
                            ["repository"] = "jellyfin-plugin-collection-sections",
                            ["includeIssuesLink"] = true,
                            ["featureRequestTag"] = "collection-sections"
                        }
                    };
                    jsonPayload.Add("info", info);

                    JArray configurationOptions = new JArray();
                    
                    JObject sortOrderOption = new JObject
                    {
                        ["key"] = "sortOrder",
                        ["name"] = "Sort Order",
                        ["description"] = "How to sort the items",
                        ["type"] = "dropdown",
                        ["defaultValue"] = "Default",
                        ["allowUserOverride"] = true,
                        ["options"] = new JArray(
                            new JObject { ["key"] = "Default", ["value"] = "Default" },
                            new JObject { ["key"] = "DateAdded", ["value"] = "Date Added To Server" },
                            new JObject { ["key"] = "Alphabetical", ["value"] = "Alphabetical" },
                            new JObject { ["key"] = "Random", ["value"] = "Random" },
                            new JObject { ["key"] = "CommunityRating", ["value"] = "Rating" },
                            new JObject { ["key"] = "PremiereDate", ["value"] = "Release Date" },
                            new JObject { ["key"] = "RecentlyWatched", ["value"] = "Recently Watched" }
                        )
                    };
                    configurationOptions.Add(sortOrderOption);

                    JObject sortDirectionOption = new JObject
                    {
                        ["key"] = "sortDirection",
                        ["name"] = "Sort Direction",
                        ["description"] = "Direction for sorting",
                        ["type"] = "dropdown",
                        ["defaultValue"] = "Descending",
                        ["allowUserOverride"] = true,
                        ["options"] = new JArray(
                            new JObject { ["key"] = "Ascending", ["value"] = "Ascending (A-Z, Oldest First)" },
                            new JObject { ["key"] = "Descending", ["value"] = "Descending (Z-A, Newest First)" }
                        )
                    };
                    configurationOptions.Add(sortDirectionOption);

                    JObject watchedItemsOption = new JObject
                    {
                        ["key"] = "watchedItemsHandling",
                        ["name"] = "Watched Items Handling",
                        ["description"] = "How to handle items that have been watched",
                        ["type"] = "dropdown",
                        ["defaultValue"] = "Show",
                        ["allowUserOverride"] = true,
                        ["advanced"] = true,
                        ["options"] = new JArray(
                            new JObject { ["key"] = "Show", ["value"] = "Show" },
                            new JObject { ["key"] = "Hide", ["value"] = "Hide" }
                        )
                    };
                    configurationOptions.Add(watchedItemsOption);

                    JObject itemLimitOption = new JObject
                    {
                        ["key"] = "itemLimit",
                        ["name"] = "Item Limit",
                        ["description"] = "Maximum number of items to display",
                        ["type"] = "numberbox",
                        ["defaultValue"] = 32.0,
                        ["allowUserOverride"] = true,
                        ["advanced"] = true,
                        ["validation"] = new JObject
                        {
                            ["min"] = 1,
                            ["max"] = 100
                        }
                    };
                    configurationOptions.Add(itemLimitOption);

                    jsonPayload.Add("configurationOptions", configurationOptions.ToString());

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
                        var response = await client.PostAsync("/HomeScreen/RegisterSection",
                            new StringContent(jsonPayload.ToString(Formatting.None),
                                MediaTypeHeaderValue.Parse("application/json")));
                        
                        if (response.IsSuccessStatusCode)
                        {
                            m_logger.LogInformation("Registered section '{DisplayText}'", section.DisplayText);
                        }
                        else
                        {
                            m_logger.LogWarning("Failed to register section '{DisplayText}': {StatusCode}", 
                                section.DisplayText, response.StatusCode);
                        }
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