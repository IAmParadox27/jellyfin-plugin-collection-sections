using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.CollectionSections.Configuration;
using Jellyfin.Plugin.CollectionSections.Sections;
using Jellyfin.Plugin.HomeScreenSections.HomeScreen.Sections;
using Jellyfin.Plugin.HomeScreenSections.Library;
using Jellyfin.Plugin.HomeScreenSections.Model.Dto;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.CollectionSections;

public class CollectionSectionPlugin : BasePlugin<PluginConfiguration>, IHasPluginConfiguration, IHasWebPages
{
    [ModuleInitializer]
	public static void Init()
    {
        // This is annoyingly necessary at the moment. Looking to find a solution to this.
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            return AssemblyLoadContext.All.FirstOrDefault(x => x.Name?.Contains("Referenceable") ?? false)?.Assemblies?.FirstOrDefault(x => x.FullName == args.Name);
        };
    }

    public override Guid Id => Guid.Parse("043b2c48-b3e0-4610-b398-8217b146d1a4");

    public override string Name => "Collection Sections";

    private readonly ICollectionManager m_collectionManager;
    private readonly IPlaylistManager m_playlistManager;
    private readonly ILibraryManager m_libraryManager;
    private readonly IDtoService m_dtoService;
    private readonly IUserManager m_userManager;

    public CollectionSectionPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, 
        ICollectionManager collectionManager, IPlaylistManager playlistManager, ILibraryManager libraryManager, 
        IUserManager userManager, IDtoService dtoService, IServiceProvider serviceProvider) : base(applicationPaths, xmlSerializer)
    {
        m_collectionManager = collectionManager;
        m_libraryManager = libraryManager;
        m_playlistManager = playlistManager;
        m_dtoService = dtoService;
        m_userManager = userManager;
        
        IHomeScreenManager? homeScreenManager = serviceProvider.GetService<IHomeScreenManager>();
        homeScreenManager?.RegisterResultsDelegate(new PluginDefinedSection("Jellyfin_Plugin_CollectionSections_Trending", "Trending", additionalData: "Trending")
        {
            OnGetResults = GetResults
        });
        homeScreenManager?.RegisterResultsDelegate(new PluginDefinedSection("Jellyfin_Plugin_CollectionSections_MostWatchedWeek", "Most Watched this Week", additionalData: "Most Watched This Week")
        {
            OnGetResults = GetResults
        });
    }

    private QueryResult<BaseItemDto> GetResults(HomeScreenSectionPayload payload)
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

        var playlist = m_playlistManager.GetPlaylists(payload.UserId)
            .FirstOrDefault(x => x.Name == payload.AdditionalData);

        var itemsRaw = playlist.GetManageableItems()
            .Where(i => i.Item2.IsVisible(m_userManager.GetUserById(payload.UserId)));
        
        var items = itemsRaw.Take(Math.Min(itemsRaw.Count(), 32)).ToArray();
        
        return new QueryResult<BaseItemDto>(m_dtoService.GetBaseItemDtos(items.Select(x => x.Item2).ToList(), dtoOptions));
    }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield break;
    }
}