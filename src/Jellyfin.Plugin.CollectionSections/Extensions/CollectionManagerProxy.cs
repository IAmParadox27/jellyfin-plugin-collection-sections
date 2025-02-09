using System.Reflection;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.CollectionSections.Extensions
{
    public static class CollectionManagerExtensions
    {
        public static IEnumerable<BoxSet> GetCollections(this ICollectionManager collectionManager, User user)
        {
            return collectionManager.GetType()
                .GetMethod("GetCollections", BindingFlags.Instance | BindingFlags.NonPublic)?
                .Invoke(collectionManager, new object?[]
                {
                    user
                }) as IEnumerable<BoxSet> ?? Enumerable.Empty<BoxSet>();
        }
    }
}