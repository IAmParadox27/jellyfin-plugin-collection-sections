using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Playlists;

namespace Jellyfin.Plugin.CollectionSections.Model
{
    public class LibraryCache
    {
        public static Dictionary<Guid, List<Playlist>> CachedPlaylists { get; set; } = new Dictionary<Guid, List<Playlist>>();
        
        public static Dictionary<Guid, List<BoxSet>> CachedCollections { get; set; } = new Dictionary<Guid, List<BoxSet>>();
    }
}