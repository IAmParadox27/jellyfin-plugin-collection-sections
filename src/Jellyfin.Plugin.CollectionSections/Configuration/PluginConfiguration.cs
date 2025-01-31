using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CollectionSections.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public SectionsConfig[] Sections { get; set; } = Array.Empty<SectionsConfig>();
}