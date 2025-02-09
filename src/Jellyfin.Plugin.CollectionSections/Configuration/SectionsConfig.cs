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
    }
}