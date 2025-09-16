using Jellyfin.Plugin.CollectionSections.Extensions;
using Jellyfin.Plugin.CollectionSections.Model;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Plugin.CollectionSections
{
    public class ResultsHandler
    {
        private readonly ICollectionManager m_collectionManager;
        private readonly IPlaylistManager m_playlistManager;
        private readonly IDtoService m_dtoService;
        private readonly IUserManager m_userManager;

        public ResultsHandler(ICollectionManager collectionManager, IPlaylistManager playlistManager, 
            IUserManager userManager, IDtoService dtoService)
        {
            m_collectionManager = collectionManager;
            m_playlistManager = playlistManager;
            m_dtoService = dtoService;
            m_userManager = userManager;
        }
        
        public QueryResult<BaseItemDto> GetCollectionResults(HomeScreenSectionPayload payload)
        {
            DtoOptions dtoOptions = new DtoOptions
            {
                Fields = new[]
                {
                    ItemFields.PrimaryImageAspectRatio,
                    ItemFields.MediaSourceCount
                },
                ImageTypes = new[]
                {
                    ImageType.Primary,
                    ImageType.Backdrop,
                    ImageType.Banner,
                    ImageType.Thumb
                },
                ImageTypeLimit = 1
            };

            User user = m_userManager.GetUserById(payload.UserId)!;
            BoxSet? collection = m_collectionManager.GetCollections(user)
                .FirstOrDefault(x => x.Name == payload.AdditionalData);
        
            List<BaseItem> items =  collection?.GetChildren(user, true).ToList() ?? new List<BaseItem>();
            
            items = items.Take(Math.Min(items.Count, 32)).ToList();
        
            return new QueryResult<BaseItemDto>(m_dtoService.GetBaseItemDtos(items, dtoOptions, user));
        }

        public QueryResult<BaseItemDto> GetPlaylistResults(HomeScreenSectionPayload payload)
        {
            DtoOptions dtoOptions = new DtoOptions
            {
                Fields = new[]
                {
                    ItemFields.PrimaryImageAspectRatio,
                    ItemFields.MediaSourceCount
                },
                ImageTypes = new[]
                {
                    ImageType.Primary,
                    ImageType.Backdrop,
                    ImageType.Banner,
                    ImageType.Thumb
                },
                ImageTypeLimit = 1
            };

            Playlist? playlist = m_playlistManager.GetPlaylists(payload.UserId)
                .FirstOrDefault(x => x.Name == payload.AdditionalData);

            IEnumerable<Tuple<LinkedChild, BaseItem>> itemsRaw = playlist?.GetManageableItems()
                .Where(i => i.Item2.IsVisible(m_userManager.GetUserById(payload.UserId))) ?? Enumerable.Empty<Tuple<LinkedChild, BaseItem>>();

            IEnumerable<IGrouping<BaseItem, Tuple<LinkedChild, BaseItem>>> groupedItems = itemsRaw.GroupBy(x =>
            {
                if (x.Item2 is Episode episode)
                {
                    return episode.Series;
                }

                return x.Item2;
            });
        
            IGrouping<BaseItem, Tuple<LinkedChild, BaseItem>>[] items = groupedItems.Take(Math.Min(groupedItems.Count(), 32)).ToArray();
        
            User user = m_userManager.GetUserById(payload.UserId)!;
            return new QueryResult<BaseItemDto>(m_dtoService.GetBaseItemDtos(items.Select(x => x.Key).ToList(), dtoOptions, user));
        }
    }
}