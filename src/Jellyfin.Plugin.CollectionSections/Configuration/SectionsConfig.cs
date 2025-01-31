namespace Jellyfin.Plugin.CollectionSections.Configuration
{
    public enum SectionType
    {
        Collection,
        Playlist
    }
    
    public class SectionsConfig
    {
        public string UniqueId { get; set; }
        public string DisplayText { get; set; }
        public string CollectionName { get; set; }
        public SectionType SectionType { get; set; }
    }
}