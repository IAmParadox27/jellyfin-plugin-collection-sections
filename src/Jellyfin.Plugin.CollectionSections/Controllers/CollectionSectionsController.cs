using Jellyfin.Plugin.CollectionSections.Extensions;
using Jellyfin.Plugin.CollectionSections.Model;
using Jellyfin.Plugin.CollectionSections.Configuration;
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.CollectionSections.Controllers
{
    /// <summary>
    /// Configuration for collection sections.
    /// </summary>
    public class CollectionSectionConfig
    {
        [JsonPropertyName("itemLimit")]
        public double ItemLimit { get; set; } = 32.0;
        
        [JsonPropertyName("watchedItemsHandling")]
        public string WatchedItemsHandling { get; set; } = "Show";
        
        [JsonPropertyName("sortOrder")]
        public string SortOrder { get; set; } = "Default";
        
        [JsonPropertyName("sortDirection")]
        public string SortDirection { get; set; } = "Descending";
    }

    [Route("[controller]")]
    public class CollectionSectionsController : ControllerBase
    {
        private readonly ICollectionManager m_collectionManager;
        private readonly IPlaylistManager m_playlistManager;
        private readonly IDtoService m_dtoService;
        private readonly IUserManager m_userManager;
        private readonly IUserDataManager m_userDataManager;

        public CollectionSectionsController(ICollectionManager collectionManager, IPlaylistManager playlistManager, 
            IUserManager userManager, IDtoService dtoService, IUserDataManager userDataManager)
        {
            m_collectionManager = collectionManager;
            m_playlistManager = playlistManager;
            m_dtoService = dtoService;
            m_userManager = userManager;
            m_userDataManager = userDataManager;
        }
        
        [HttpPost("Collection")]
        public QueryResult<BaseItemDto> GetCollectionResults([FromBody] HomeScreenSectionPayload payload)
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
        
            if (collection == null)
            {
                return new QueryResult<BaseItemDto>();
            }

            List<BaseItem> items = collection.GetChildren(user, true).ToList();
            items = ApplyUserConfiguration(items, payload, user);
            
            return new QueryResult<BaseItemDto>(m_dtoService.GetBaseItemDtos(items, dtoOptions, user));
        }
        
        [HttpPost("Playlist")]
        public QueryResult<BaseItemDto> GetPlaylistResults([FromBody] HomeScreenSectionPayload payload)
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
            Playlist? playlist = m_playlistManager.GetPlaylists(payload.UserId)
                .FirstOrDefault(x => x.Name == payload.AdditionalData);

            if (playlist == null)
            {
                return new QueryResult<BaseItemDto>();
            }

            IEnumerable<Tuple<LinkedChild, BaseItem>> itemsRaw = playlist.GetManageableItems()
                .Where(i => i.Item2.IsVisible(user));

            IEnumerable<IGrouping<BaseItem, Tuple<LinkedChild, BaseItem>>> groupedItems = itemsRaw.GroupBy(x =>
            {
                if (x.Item2 is Episode episode)
                {
                    return episode.Series;
                }

                return x.Item2;
            });
        
            List<BaseItem> items = groupedItems.Select(x => x.Key).ToList();
            items = ApplyUserConfiguration(items, payload, user);
        
            return new QueryResult<BaseItemDto>(m_dtoService.GetBaseItemDtos(items, dtoOptions, user));
        }

        private List<BaseItem> ApplyUserConfiguration(List<BaseItem> items, HomeScreenSectionPayload payload, User user)
        {
            var config = ParseConfiguration(payload);

            if (config.WatchedItemsHandling == "Hide")
            {
                items = items.Where(item => !item.IsPlayed(user)).ToList();
            }

            items = ApplySortingToItems(items, config.SortOrder, config.SortDirection, user).ToList();
            items = items.Take(Math.Min(items.Count, (int)config.ItemLimit)).ToList();

            return items;
        }

        /// <summary>
        /// Parses configuration from payload.
        /// </summary>
        private CollectionSectionConfig ParseConfiguration(HomeScreenSectionPayload payload)
        {
            var config = new CollectionSectionConfig();
            
            if (payload.UserConfiguration != null)
            {
                try
                {
                    var json = JsonSerializer.Serialize(payload.UserConfiguration);
                    var parsed = JsonSerializer.Deserialize<CollectionSectionConfig>(json);
                    if (parsed != null) 
                    {
                        return parsed;
                    }
                }
                catch (JsonException)
                {
                }
            }
            
            return config;
        }

        /// <summary>
        /// Applies sorting to items.
        /// </summary>
        private IEnumerable<BaseItem> ApplySortingToItems(IEnumerable<BaseItem> items, string sortOrder, string sortDirection, User user)
        {
            var sortedItems = sortOrder switch
            {
                "Default" => items,
                "PremiereDate" => items.OrderBy(item => item.PremiereDate ?? DateTime.MinValue),
                "DateAdded" => items.OrderBy(item => item.DateCreated),
                "Alphabetical" => items.OrderBy(item => item.SortName ?? item.Name),
                "RecentlyWatched" => GetRecentlyWatchedSortedItems(items, user),
                "CommunityRating" => items.OrderBy(item => item.CommunityRating ?? 0),
                "Random" => items.OrderBy(x => Random.Shared.Next()),
                _ => items
            };

            if (string.Equals(sortDirection, "Descending", StringComparison.OrdinalIgnoreCase))
            {
                sortedItems = sortedItems.Reverse();
            }

            return sortedItems;
        }

        private IEnumerable<BaseItem> GetRecentlyWatchedSortedItems(IEnumerable<BaseItem> items, User user)
        {
            var itemsList = items.ToList();
            var userDataLookup = new Dictionary<Guid, DateTime>();
            
            foreach (var item in itemsList)
            {
                try
                {
                    var userData = m_userDataManager.GetUserData(user, item);
                    userDataLookup[item.Id] = userData?.LastPlayedDate ?? DateTime.MinValue;
                }
                catch
                {
                    userDataLookup[item.Id] = DateTime.MinValue;
                }
            }
            
            return itemsList.OrderBy(item => userDataLookup[item.Id]);
        }
    }
}