using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.CollectionSections.Configuration
{
    public enum SectionType
    {
        Collection,
        Playlist
    }
    
    public class SectionsConfig
    {
        public required string UniqueId { get; set; }
        public required string DisplayText { get; set; }
        public required string CollectionName { get; set; }
        public SectionType SectionType { get; set; }
        public string? Description { get; set; }
        public double MaxItems { get; set; } = 32.0;
        public bool ShowOnlyUnwatched { get; set; } = false;
        public string? SortBy { get; set; } = "Default";
        public bool SortDescending { get; set; } = true;
    }
}