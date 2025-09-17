using Jellyfin.Data.Entities.Libraries;
using Jellyfin.Plugin.CollectionSections.Extensions;
using Jellyfin.Plugin.CollectionSections.JellyfinVersionSpecific;
using Jellyfin.Plugin.CollectionSections.Model;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionSections.Services
{
    public class StartupService : IScheduledTask
    {
        public string Name => "CollectionSections Startup";

        public string Key => "Jellyfin.Plugin.CollectionSections.Startup";
        
        public string Description => "Startup Service for CollectionSections";
        
        public string Category => "Startup Services";
        
        private readonly IServerApplicationHost m_serverApplicationHost;
        private readonly IApplicationPaths m_applicationPaths;
        private readonly IServiceProvider m_serviceProvider;

        public StartupService(IServerApplicationHost serverApplicationHost, IApplicationPaths applicationPaths, IServiceProvider serviceProvider)
        {
            m_serverApplicationHost = serverApplicationHost;
            m_applicationPaths = applicationPaths;
            m_serviceProvider = serviceProvider;
        }

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            ILogger logger = m_serviceProvider.GetRequiredService<ILogger<StartupService>>();
            
            logger.LogInformation("CollectionSections Startup. Starting cache process for playlists and collections for quicker data retrievable on request.");
            IUserManager userManager = m_serviceProvider.GetRequiredService<IUserManager>();
            ICollectionManager collectionManager = m_serviceProvider.GetRequiredService<ICollectionManager>();
            IPlaylistManager playlistManager = m_serviceProvider.GetRequiredService<IPlaylistManager>();

            foreach (User user in userManager.Users)
            {
                logger.LogInformation($"Caching data for user {user.Username}");
                if (!LibraryCache.CachedCollections.ContainsKey(user.Id))
                {
                    LibraryCache.CachedCollections.Add(user.Id, new List<BoxSet>(collectionManager.GetCollections(user)));
                }

                if (!LibraryCache.CachedPlaylists.ContainsKey(user.Id))
                {
                    LibraryCache.CachedPlaylists.Add(user.Id, new List<Playlist>(playlistManager.GetPlaylists(user.Id)));
                }
                
                logger.LogInformation($"Caching data for user {user.Username} finished");
            }
            
            logger.LogInformation($"Caching finished");
            CollectionSectionPlugin.Instance.OnConfigurationChanged(this, CollectionSectionPlugin.Instance.Configuration);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => StartupServiceHelper.GetDefaultTriggers();
    }
}